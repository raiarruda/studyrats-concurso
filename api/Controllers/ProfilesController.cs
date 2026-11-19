using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudyRats.Api.Data;
using StudyRats.Api.Dtos;
using StudyRats.Api.Services;

namespace StudyRats.Api.Controllers;

[Route("api/profile")]
[Authorize]
public class ProfilesController(AppDbContext db, AppClock clock) : AppControllerBase
{
    /// <summary>
    /// Perfil e calendário do mês (AAAA-MM). Sem userId, é o perfil de quem pediu, com todos os seus grupos.
    /// Ao ver outra pessoa, só entram registros de grupos que as duas pessoas integram.
    /// </summary>
    [HttpGet("{userId:guid?}")]
    public async Task<ActionResult<ProfileDto>> Get(Guid? userId, string month, CancellationToken ct)
    {
        if (!DateOnly.TryParseExact(month + "-01", "yyyy-MM-dd", out var start))
            return Fail(400, "Mês inválido. Use o formato AAAA-MM.");
        var end = start.AddMonths(1);

        var viewer = UserId;
        var target = userId ?? viewer;

        var name = await db.Users.AsNoTracking()
            .Where(u => u.Id == target)
            .Select(u => u.Name)
            .FirstOrDefaultAsync(ct);
        if (name is null) return NotFound();

        var viewerGroups = db.Members.Where(m => m.UserId == viewer).Select(m => m.GroupId);
        var sharedGroups = db.Members
            .Where(m => m.UserId == target && viewerGroups.Contains(m.GroupId))
            .Select(m => m.GroupId);

        // Sem grupo em comum, a pessoa não é visível: 404, como nos grupos (não revela que existe).
        if (target != viewer && !await sharedGroups.AnyAsync(ct)) return NotFound();

        // Uma pessoa tem centenas de registros, não milhões: calcular em memória é simples e suficiente.
        var all = await db.StudyLogs.AsNoTracking()
            .Where(l => l.UserId == target && sharedGroups.Contains(l.GroupId))
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => new { l.Date, l.PhotoFileName, l.QuestionsAnswered, l.StudyMinutes })
            .ToListAsync(ct);

        var dates = all.Select(l => l.Date).ToHashSet();

        var days = all
            .Where(l => l.Date >= start && l.Date < end)
            .GroupBy(l => l.Date)
            .OrderBy(g => g.Key)
            .Select(g => new CalendarDayDto(
                g.Key,
                g.Count(),
                "/uploads/" + g.First().PhotoFileName, // o mais recente do dia (a lista já vem ordenada)
                SumOrNull(g.Select(l => l.QuestionsAnswered)),
                SumOrNull(g.Select(l => l.StudyMinutes))))
            .ToList();

        return Ok(new ProfileDto(
            target, name, target == viewer,
            all.Count,
            dates.Count,
            SumOrNull(all.Select(l => l.StudyMinutes)),
            SumOrNull(all.Select(l => l.QuestionsAnswered)),
            RankingService.CurrentDayStreak(dates, clock.Today()),
            dates.Count > 0 ? dates.Min() : null,
            days));
    }

    private static int? SumOrNull(IEnumerable<int?> values)
    {
        var list = values.Where(v => v.HasValue).ToList();
        return list.Count == 0 ? null : list.Sum(v => v!.Value);
    }
}
