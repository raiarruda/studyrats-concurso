using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudyRats.Api.Data;
using StudyRats.Api.Domain;
using StudyRats.Api.Dtos;
using StudyRats.Api.Services;

namespace StudyRats.Api.Controllers;

/// <summary>Desafio = o grupo se preparando para um edital, com data de prova.</summary>
[Route("api/groups/{groupId:guid}/challenges")]
[Authorize]
public class ChallengesController(AppDbContext db, AppClock clock, RankingService ranking, BolaoService bolao)
    : AppControllerBase
{
    private const int MaxPeriodDays = 731;

    [HttpGet]
    public async Task<ActionResult<List<ChallengeDto>>> List(Guid groupId, CancellationToken ct)
    {
        var (group, _) = await FindMyGroup(groupId, ct);
        if (group is null) return NotFound();

        var challenges = await db.Challenges.AsNoTracking()
            .Where(c => c.GroupId == groupId)
            .OrderByDescending(c => c.ExamDate)
            .ToListAsync(ct);
        return challenges.Select(ToDto).ToList();
    }

    [HttpPost]
    public async Task<ActionResult<ChallengeDto>> Create(Guid groupId, CreateChallengeRequest req, CancellationToken ct)
    {
        var (group, isOwner) = await FindMyGroup(groupId, ct);
        if (group is null) return NotFound();
        if (!isOwner) return Fail(403, "Só quem criou o grupo pode criar desafios.");

        if (req.ExamDate < req.StartDate)
            return Fail(400, "A data da prova não pode ser anterior ao início.");
        if (req.ExamDate < clock.Today())
            return Fail(400, "A data da prova não pode estar no passado.");
        if (req.ExamDate.DayNumber - req.StartDate.DayNumber > MaxPeriodDays)
            return Fail(400, $"O período máximo de um desafio é de {MaxPeriodDays} dias.");

        var betError = ValidateBet(req);
        if (betError is not null) return Fail(400, betError);

        var challenge = new Challenge
        {
            GroupId = groupId,
            Name = req.Name.Trim(),
            StartDate = req.StartDate,
            ExamDate = req.ExamDate,
            PenaltyCents = req.PenaltyCents,
            PrizePercents = req.PenaltyCents is null ? [] : req.PrizePercents!.ToArray(),
        };
        db.Challenges.Add(challenge);
        await db.SaveChangesAsync(ct);

        return Ok(ToDto(challenge));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ChallengeDto>> Detail(Guid groupId, Guid id, CancellationToken ct)
    {
        var (group, _) = await FindMyGroup(groupId, ct);
        if (group is null) return NotFound();

        var challenge = await db.Challenges.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id && c.GroupId == groupId, ct);
        return challenge is null ? NotFound() : Ok(ToDto(challenge));
    }

    /// <summary>Ranking do desafio: só conta registros entre o início e o dia da prova.</summary>
    [HttpGet("{id:guid}/ranking")]
    public async Task<ActionResult<List<RankingEntryDto>>> Ranking(Guid groupId, Guid id, CancellationToken ct)
    {
        var (group, _) = await FindMyGroup(groupId, ct);
        if (group is null) return NotFound();

        var challenge = await db.Challenges.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id && c.GroupId == groupId, ct);
        if (challenge is null) return NotFound();

        return Ok(await ranking.BuildAsync(group, challenge.StartDate, challenge.ExamDate, clock.Today(), ct));
    }

    // ---------- bolão (simulado) ----------

    [HttpGet("{id:guid}/bolao")]
    public async Task<ActionResult<BolaoDto>> GetBolao(Guid groupId, Guid id, CancellationToken ct)
    {
        var (group, _) = await FindMyGroup(groupId, ct);
        if (group is null) return NotFound();

        var challenge = await db.Challenges.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id && c.GroupId == groupId, ct);
        if (challenge is null) return NotFound();
        if (challenge.PenaltyCents is null) return Fail(404, "Este desafio não tem bolão.");

        return Ok(await bolao.BuildAsync(group, challenge, UserId, clock.Today(), ct));
    }

    /// <summary>Entrar no bolão é uma escolha de cada pessoa. Só valem as semanas inteiras a partir de hoje.</summary>
    [HttpPost("{id:guid}/bolao/join")]
    public async Task<ActionResult<BolaoDto>> JoinBolao(Guid groupId, Guid id, CancellationToken ct)
    {
        var (group, _) = await FindMyGroup(groupId, ct);
        if (group is null) return NotFound();

        var challenge = await db.Challenges.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id && c.GroupId == groupId, ct);
        if (challenge is null) return NotFound();
        if (challenge.PenaltyCents is null) return Fail(404, "Este desafio não tem bolão.");

        var today = clock.Today();
        if (today > challenge.ExamDate) return Fail(409, "O desafio já terminou.");

        var uid = UserId;
        if (!await db.ChallengeBettors.AnyAsync(b => b.ChallengeId == id && b.UserId == uid, ct))
        {
            db.ChallengeBettors.Add(new ChallengeBettor { ChallengeId = id, UserId = uid, JoinedOn = today });
            try { await db.SaveChangesAsync(ct); }
            catch (DbUpdateException) { /* entrou em paralelo: já está no bolão */ }
        }

        return Ok(await bolao.BuildAsync(group, challenge, uid, today, ct));
    }

    /// <summary>Só dá para sair enquanto nenhuma semana sua foi contabilizada; depois disso a entrada é definitiva.</summary>
    [HttpDelete("{id:guid}/bolao/join")]
    public async Task<IActionResult> LeaveBolao(Guid groupId, Guid id, CancellationToken ct)
    {
        var (group, _) = await FindMyGroup(groupId, ct);
        if (group is null) return NotFound();

        var challenge = await db.Challenges.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id && c.GroupId == groupId, ct);
        if (challenge is null) return NotFound();

        var uid = UserId;
        var bettor = await db.ChallengeBettors.FirstOrDefaultAsync(b => b.ChallengeId == id && b.UserId == uid, ct);
        if (bettor is null) return NotFound();

        var weeks = BolaoService.FullWeeks(challenge.StartDate, challenge.ExamDate);
        if (BolaoService.ClosedWeeksFor(weeks, bettor.JoinedOn, clock.Today()) > 0)
            return Fail(409, "Você já tem semanas contabilizadas no bolão e não pode mais sair.");

        db.ChallengeBettors.Remove(bettor);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid groupId, Guid id, CancellationToken ct)
    {
        var (group, isOwner) = await FindMyGroup(groupId, ct);
        if (group is null) return NotFound();
        if (!isOwner) return Fail(403, "Só quem criou o grupo pode apagar desafios.");

        var challenge = await db.Challenges.FirstOrDefaultAsync(c => c.Id == id && c.GroupId == groupId, ct);
        if (challenge is null) return NotFound();

        db.Challenges.Remove(challenge); // os registros de estudo ficam: pertencem ao grupo
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ---------- helpers ----------

    private static string? ValidateBet(CreateChallengeRequest req)
    {
        var percents = req.PrizePercents ?? [];

        if (req.PenaltyCents is null)
            return percents.Count > 0 ? "Para dividir prêmios, defina o valor da multa por semana falhada." : null;

        if (percents.Count is < 2 or > 3)
            return "Defina a divisão do prêmio para 2 ou 3 colocados.";
        if (percents.Any(p => p < 1 || p > 100))
            return "Cada porcentagem do prêmio deve ficar entre 1 e 100.";
        if (percents.Sum() > 100)
            return "A soma das porcentagens do prêmio não pode passar de 100.";
        if (BolaoService.FullWeeks(req.StartDate, req.ExamDate).Count == 0)
            return "Para valer multa, o desafio precisa ter ao menos uma semana inteira (segunda a domingo).";

        return null;
    }

    // Quem não é membro recebe 404 (e não 403): não revela que o grupo existe.
    private async Task<(Group? Group, bool IsOwner)> FindMyGroup(Guid groupId, CancellationToken ct)
    {
        var uid = UserId;
        var row = await db.Members.AsNoTracking()
            .Where(m => m.GroupId == groupId && m.UserId == uid)
            .Select(m => new { m.Group, m.Role })
            .FirstOrDefaultAsync(ct);
        return row is null ? (null, false) : (row.Group, row.Role == MemberRole.Owner);
    }

    private static ChallengeDto ToDto(Challenge c) =>
        new(c.Id, c.GroupId, c.Name, c.StartDate, c.ExamDate,
            c.PenaltyCents is { } p ? new BetConfigDto(p, c.PrizePercents.ToList()) : null);
}
