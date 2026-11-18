using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using StudyRats.Api.Data;
using StudyRats.Api.Domain;
using StudyRats.Api.Dtos;
using StudyRats.Api.Services;

namespace StudyRats.Api.Controllers;

[Route("api/groups")]
[Authorize]
public class GroupsController(AppDbContext db, AppClock clock) : AppControllerBase
{
    // Sem 0/O/1/I: o código é digitado à mão a partir de uma mensagem de WhatsApp.
    private const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int CodeLength = 6;
    private const int MaxPeriodDays = 366;

    [HttpGet]
    public async Task<ActionResult<List<GroupSummaryDto>>> Mine(CancellationToken ct)
    {
        var uid = UserId;
        return await db.Groups.AsNoTracking()
            .Where(g => g.Members.Any(m => m.UserId == uid))
            .OrderByDescending(g => g.CreatedAt)
            .Select(g => new GroupSummaryDto(g.Id, g.Name, g.Code, g.StartDate, g.EndDate,
                g.TrackQuestions, g.TrackCorrect, g.TrackTime, g.Members.Count, g.OwnerId == uid))
            .ToListAsync(ct);
    }

    [HttpPost]
    public async Task<ActionResult<GroupSummaryDto>> Create(CreateGroupRequest req, CancellationToken ct)
    {
        if (!req.TrackQuestions && !req.TrackCorrect && !req.TrackTime)
            return Fail(400, "Escolha pelo menos uma métrica: questões, acertos ou tempo de estudo.");
        if (req.TrackCorrect && !req.TrackQuestions)
            return Fail(400, "Para contar acertos, o grupo também precisa contar questões respondidas.");
        if (req.EndDate < req.StartDate)
            return Fail(400, "A data final não pode ser anterior à inicial.");
        if (req.EndDate.DayNumber - req.StartDate.DayNumber > MaxPeriodDays)
            return Fail(400, $"O período máximo é de {MaxPeriodDays} dias.");

        var uid = UserId;
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var group = new Group
            {
                Name = req.Name.Trim(),
                Code = RandomNumberGenerator.GetString(CodeAlphabet, CodeLength),
                StartDate = req.StartDate,
                EndDate = req.EndDate,
                TrackQuestions = req.TrackQuestions,
                TrackCorrect = req.TrackCorrect,
                TrackTime = req.TrackTime,
                OwnerId = uid,
            };
            group.Members.Add(new Member { UserId = uid, Role = MemberRole.Owner });
            db.Groups.Add(group);

            try
            {
                await db.SaveChangesAsync(ct);
                return Ok(ToSummary(group, 1, uid));
            }
            catch (DbUpdateException)
            {
                db.ChangeTracker.Clear(); // código repetido: tenta outro
            }
        }
        return Fail(500, "Não foi possível gerar um código de convite. Tente novamente.");
    }

    [HttpPost("join")]
    [EnableRateLimiting("join")]
    public async Task<ActionResult<GroupSummaryDto>> Join(JoinGroupRequest req, CancellationToken ct)
    {
        var uid = UserId;
        var code = req.Code.Trim().ToUpperInvariant();
        var group = await db.Groups.FirstOrDefaultAsync(g => g.Code == code, ct);
        if (group is null) return Fail(404, "Código de convite inválido.");

        var already = await db.Members.AnyAsync(m => m.GroupId == group.Id && m.UserId == uid, ct);
        if (!already)
        {
            db.Members.Add(new Member { GroupId = group.Id, UserId = uid, Role = MemberRole.Member });
            try { await db.SaveChangesAsync(ct); }
            catch (DbUpdateException) { /* entrou em paralelo: já é membro */ }
        }

        var count = await db.Members.CountAsync(m => m.GroupId == group.Id, ct);
        return Ok(ToSummary(group, count, uid));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GroupDetailDto>> Detail(Guid id, CancellationToken ct)
    {
        var group = await FindMyGroup(id, ct);
        if (group is null) return NotFound();

        var members = await db.Members.AsNoTracking()
            .Where(m => m.GroupId == id)
            .OrderBy(m => m.JoinedAt)
            .Select(m => new MemberDto(m.UserId, m.User.Name, m.Role == MemberRole.Owner))
            .ToListAsync(ct);

        return Ok(new GroupDetailDto(ToSummary(group, members.Count, UserId), members));
    }

    [HttpGet("{id:guid}/ranking")]
    public async Task<ActionResult<List<RankingEntryDto>>> Ranking(Guid id, CancellationToken ct)
    {
        var group = await FindMyGroup(id, ct);
        if (group is null) return NotFound();

        var members = await db.Members.AsNoTracking()
            .Where(m => m.GroupId == id)
            .Select(m => new { m.UserId, m.User.Name })
            .ToListAsync(ct);

        var totals = (await db.StudyLogs.AsNoTracking()
                .Where(l => l.GroupId == id)
                .GroupBy(l => l.UserId)
                .Select(x => new
                {
                    UserId = x.Key,
                    Questions = x.Sum(l => l.QuestionsAnswered ?? 0),
                    Correct = x.Sum(l => l.QuestionsCorrect ?? 0),
                    Minutes = x.Sum(l => l.StudyMinutes ?? 0),
                })
                .ToListAsync(ct))
            .ToDictionary(x => x.UserId);

        var datesByUser = (await db.StudyLogs.AsNoTracking()
                .Where(l => l.GroupId == id)
                .Select(l => new { l.UserId, l.Date })
                .Distinct()
                .ToListAsync(ct))
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Date).ToHashSet());

        var today = clock.Today();

        var entries = members.Select(m =>
        {
            totals.TryGetValue(m.UserId, out var t);
            datesByUser.TryGetValue(m.UserId, out var dates);
            dates ??= [];

            int? questions = group.TrackQuestions ? t?.Questions ?? 0 : null;
            int? correct = group.TrackCorrect ? t?.Correct ?? 0 : null;
            int? minutes = group.TrackTime ? t?.Minutes ?? 0 : null;
            double? accuracy = group.TrackCorrect && questions > 0
                ? Math.Round((double)correct!.Value / questions.Value * 100, 1)
                : null;

            return new RankingEntryDto(m.UserId, m.Name, questions, correct, accuracy, minutes,
                dates.Count, CurrentStreak(dates, today));
        });

        // Ordem padrão: volume de questões; se o grupo não conta questões, tempo de estudo.
        var ordered = group.TrackQuestions
            ? entries.OrderByDescending(e => e.TotalQuestions)
            : entries.OrderByDescending(e => e.TotalMinutes);

        return Ok(ordered.ThenBy(e => e.Name).ToList());
    }

    [HttpGet("{id:guid}/logs")]
    public async Task<ActionResult<List<LogDto>>> Logs(Guid id, int skip = 0, int take = 20, CancellationToken ct = default)
    {
        if (await FindMyGroup(id, ct) is null) return NotFound();
        skip = Math.Max(skip, 0);
        take = Math.Clamp(take, 1, 50);

        return await db.StudyLogs.AsNoTracking()
            .Where(l => l.GroupId == id)
            .OrderByDescending(l => l.CreatedAt)
            .Skip(skip).Take(take)
            .Select(l => new LogDto(l.Id, l.UserId, l.User.Name, l.Date, "/uploads/" + l.PhotoFileName,
                l.QuestionsAnswered, l.QuestionsCorrect, l.StudyMinutes, l.Subject, l.Note, l.CreatedAt))
            .ToListAsync(ct);
    }

    [HttpPost("{id:guid}/logs")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<LogDto>> CreateLog(
        Guid id, [FromForm] CreateLogForm form, [FromServices] IPhotoStorage photos, CancellationToken ct)
    {
        var group = await FindMyGroup(id, ct);
        if (group is null) return NotFound();

        var error = ValidateLog(group, form, clock.Today(), out var date);
        if (error is not null) return Fail(400, error);

        var (fileName, photoError) = await photos.SaveAsync(form.Photo!, ct);
        if (photoError is not null) return Fail(400, photoError);

        var log = new StudyLog
        {
            GroupId = id,
            UserId = UserId,
            Date = date,
            PhotoFileName = fileName!,
            QuestionsAnswered = group.TrackQuestions ? form.QuestionsAnswered : null,
            QuestionsCorrect = group.TrackCorrect ? form.QuestionsCorrect : null,
            StudyMinutes = group.TrackTime ? form.StudyMinutes : null,
            Subject = NullIfBlank(form.Subject),
            Note = NullIfBlank(form.Note),
        };

        db.StudyLogs.Add(log);
        try { await db.SaveChangesAsync(ct); }
        catch
        {
            photos.Delete(fileName!); // não deixa foto órfã se o banco falhar
            throw;
        }

        var userName = await db.Users.Where(u => u.Id == log.UserId).Select(u => u.Name).FirstAsync(ct);
        return Ok(new LogDto(log.Id, log.UserId, userName, log.Date, "/uploads/" + log.PhotoFileName,
            log.QuestionsAnswered, log.QuestionsCorrect, log.StudyMinutes, log.Subject, log.Note, log.CreatedAt));
    }

    [HttpDelete("{id:guid}/logs/{logId:guid}")]
    public async Task<IActionResult> DeleteLog(Guid id, Guid logId, [FromServices] IPhotoStorage photos, CancellationToken ct)
    {
        var uid = UserId;
        var log = await db.StudyLogs.FirstOrDefaultAsync(l => l.Id == logId && l.GroupId == id && l.UserId == uid, ct);
        if (log is null) return NotFound(); // só o autor apaga o próprio registro

        db.StudyLogs.Remove(log);
        await db.SaveChangesAsync(ct);
        photos.Delete(log.PhotoFileName);
        return NoContent();
    }

    // ---------- helpers ----------

    // Registros retroativos só até 3 dias: sem isso, o streak seria trivial de fraudar.
    private const int MaxBackfillDays = 3;

    private static string? ValidateLog(Group g, CreateLogForm f, DateOnly today, out DateOnly date)
    {
        date = f.Date ?? today;

        if (today < g.StartDate) return "O desafio ainda não começou.";
        if (date > today) return "Não é possível registrar estudo em data futura.";
        if (date < g.StartDate || date > g.EndDate) return "A data está fora do período do desafio.";
        if (today.DayNumber - date.DayNumber > MaxBackfillDays)
            return $"Só é possível registrar os últimos {MaxBackfillDays} dias.";

        if (g.TrackQuestions)
        {
            if (f.QuestionsAnswered is null or < 1) return "Informe quantas questões você respondeu.";
            if (f.QuestionsAnswered > 5000) return "Número de questões inválido.";
        }
        else if (f.QuestionsAnswered is not null) return "Este grupo não registra questões.";

        if (g.TrackCorrect)
        {
            if (f.QuestionsCorrect is null or < 0) return "Informe quantas questões você acertou.";
            if (f.QuestionsCorrect > f.QuestionsAnswered) return "Os acertos não podem passar do total de questões.";
        }
        else if (f.QuestionsCorrect is not null) return "Este grupo não registra acertos.";

        if (g.TrackTime)
        {
            if (f.StudyMinutes is null or < 1) return "Informe quantos minutos você estudou.";
            if (f.StudyMinutes > 1440) return "O tempo de estudo não pode passar de 24 horas.";
        }
        else if (f.StudyMinutes is not null) return "Este grupo não registra tempo de estudo.";

        return null;
    }

    /// <summary>Dias seguidos até hoje. Se ainda não estudou hoje, a sequência segue viva a partir de ontem.</summary>
    private static int CurrentStreak(HashSet<DateOnly> dates, DateOnly today)
    {
        var day = dates.Contains(today) ? today : today.AddDays(-1);
        var streak = 0;
        while (dates.Contains(day))
        {
            streak++;
            day = day.AddDays(-1);
        }
        return streak;
    }

    // Quem não é membro recebe 404 (e não 403): não revela que o grupo existe.
    private Task<Group?> FindMyGroup(Guid id, CancellationToken ct)
    {
        var uid = UserId;
        return db.Groups.AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == id && g.Members.Any(m => m.UserId == uid), ct);
    }

    private static GroupSummaryDto ToSummary(Group g, int memberCount, Guid uid) =>
        new(g.Id, g.Name, g.Code, g.StartDate, g.EndDate,
            g.TrackQuestions, g.TrackCorrect, g.TrackTime, memberCount, g.OwnerId == uid);

    private static string? NullIfBlank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
