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
public class ChallengesController(AppDbContext db, AppClock clock, RankingService ranking) : AppControllerBase
{
    private const int MaxPeriodDays = 731;

    [HttpGet]
    public async Task<ActionResult<List<ChallengeDto>>> List(Guid groupId, CancellationToken ct)
    {
        var (group, _) = await FindMyGroup(groupId, ct);
        if (group is null) return NotFound();

        return await db.Challenges.AsNoTracking()
            .Where(c => c.GroupId == groupId)
            .OrderByDescending(c => c.ExamDate)
            .Select(c => new ChallengeDto(c.Id, c.GroupId, c.Name, c.StartDate, c.ExamDate))
            .ToListAsync(ct);
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

        var challenge = new Challenge
        {
            GroupId = groupId,
            Name = req.Name.Trim(),
            StartDate = req.StartDate,
            ExamDate = req.ExamDate,
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

    private static ChallengeDto ToDto(Challenge c) => new(c.Id, c.GroupId, c.Name, c.StartDate, c.ExamDate);
}
