using Microsoft.EntityFrameworkCore;
using StudyRats.Api.Data;
using StudyRats.Api.Domain;
using StudyRats.Api.Dtos;

namespace StudyRats.Api.Services;

/// <summary>
/// Bolão simulado de um desafio. Tudo é derivado dos registros de estudo na hora da consulta:
/// não há lançamentos gravados, então apagar um registro refaz a conta sozinho.
/// O app só calcula e mostra valores, nunca movimenta dinheiro.
/// </summary>
public class BolaoService(AppDbContext db, RankingService ranking)
{
    /// <summary>Segundas-feiras das semanas inteiras (segunda a domingo) que cabem dentro do desafio.</summary>
    public static List<DateOnly> FullWeeks(DateOnly start, DateOnly exam)
    {
        var weeks = new List<DateOnly>();
        var monday = RankingService.WeekStart(start);
        if (monday < start) monday = monday.AddDays(7);
        for (; monday.AddDays(6) <= exam; monday = monday.AddDays(7)) weeks.Add(monday);
        return weeks;
    }

    /// <summary>Semanas que já terminaram e valem para a pessoa (começam no dia em que entrou ou depois).</summary>
    public static int ClosedWeeksFor(IEnumerable<DateOnly> weeks, DateOnly joinedOn, DateOnly today) =>
        weeks.Count(w => w >= joinedOn && w.AddDays(6) < today);

    public static string WeekStatus(DateOnly week, DateOnly joinedOn, HashSet<DateOnly> days, int minDays, DateOnly today)
    {
        if (week < joinedOn) return "na";
        var end = week.AddDays(6);
        if (today < week) return "future";
        if (today <= end) return "open"; // ainda dá tempo de bater o mínimo

        var studied = 0;
        for (var d = week; d <= end; d = d.AddDays(1))
            if (days.Contains(d)) studied++;
        return studied >= minDays ? "ok" : "failed";
    }

    public async Task<BolaoDto> BuildAsync(
        Group group, Challenge challenge, Guid viewerId, DateOnly today, CancellationToken ct)
    {
        var penalty = challenge.PenaltyCents ?? throw new InvalidOperationException("Desafio sem bolão.");
        var percents = challenge.PrizePercents.ToList();
        var weeks = FullWeeks(challenge.StartDate, challenge.ExamDate);

        var bettors = await db.ChallengeBettors.AsNoTracking()
            .Where(b => b.ChallengeId == challenge.Id)
            .Select(b => new { b.UserId, b.User.Name, b.JoinedOn })
            .ToListAsync(ct);
        var bettorIds = bettors.Select(b => b.UserId).ToList();

        // Dias estudados dentro do desafio, só de quem está no bolão.
        var daysByUser = (await db.StudyLogs.AsNoTracking()
                .Where(l => l.GroupId == group.Id && bettorIds.Contains(l.UserId)
                            && l.Date >= challenge.StartDate && l.Date <= challenge.ExamDate)
                .Select(l => new { l.UserId, l.Date })
                .Distinct()
                .ToListAsync(ct))
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Date).ToHashSet());

        var statuses = bettors.ToDictionary(
            b => b.UserId,
            b =>
            {
                daysByUser.TryGetValue(b.UserId, out var days);
                return weeks.Select(w => WeekStatus(w, b.JoinedOn, days ?? [], group.MinDaysPerWeek, today)).ToList();
            });

        var penaltyByUser = bettors.ToDictionary(
            b => b.UserId,
            b => statuses[b.UserId].Count(s => s == "failed") * penalty);
        var pot = penaltyByUser.Values.Sum();

        // Classificação do prêmio: o mesmo ranking do desafio (dias ativos, depois tempo, depois questões),
        // só entre quem está no bolão.
        var entries = await ranking.BuildAsync(group, challenge.StartDate, challenge.ExamDate, today, ct);
        var ordered = entries.Where(e => bettorIds.Contains(e.UserId)).ToList();

        var positions = new Dictionary<Guid, int>();
        var prizes = new Dictionary<Guid, int>();
        var index = 0;
        while (index < ordered.Count)
        {
            var key = TieKey(ordered[index]);
            var last = index;
            while (last + 1 < ordered.Count && TieKey(ordered[last + 1]) == key) last++;
            var size = last - index + 1;

            // Empate total: quem empata divide o prêmio das posições que ocupa (centavos que sobram vão ao caixa).
            long prizeSum = 0;
            for (var place = index; place <= last && place < percents.Count; place++)
                prizeSum += (long)pot * percents[place] / 100;
            var share = (int)(prizeSum / size);

            for (var i = index; i <= last; i++)
            {
                positions[ordered[i].UserId] = index + 1;
                prizes[ordered[i].UserId] = share;
            }
            index = last + 1;
        }

        var members = ordered.Select(e =>
        {
            var b = bettors.First(x => x.UserId == e.UserId);
            var failed = statuses[e.UserId].Count(s => s == "failed");
            var prize = prizes[e.UserId];
            return new BolaoMemberDto(e.UserId, e.Name, b.JoinedOn, positions[e.UserId],
                failed, penaltyByUser[e.UserId], prize, prize - penaltyByUser[e.UserId], statuses[e.UserId]);
        }).ToList();

        var me = bettors.FirstOrDefault(b => b.UserId == viewerId);
        var isFinal = today > challenge.ExamDate;

        return new BolaoDto(
            penalty, percents, isFinal,
            IAmIn: me is not null,
            CanJoin: me is null && !isFinal,
            CanLeave: me is not null && ClosedWeeksFor(weeks, me.JoinedOn, today) == 0,
            PotCents: pot,
            CaixaCents: pot - prizes.Values.Sum(),
            Weeks: weeks,
            Members: members);
    }

    private static (int, int, int) TieKey(RankingEntryDto e) =>
        (e.DaysStudied, e.TotalMinutes ?? 0, e.TotalQuestions ?? 0);
}
