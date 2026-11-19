using Microsoft.EntityFrameworkCore;
using StudyRats.Api.Data;
using StudyRats.Api.Domain;
using StudyRats.Api.Dtos;

namespace StudyRats.Api.Services;

/// <summary>Monta o ranking de um grupo, inteiro ou limitado a uma janela de datas (desafio).</summary>
public class RankingService(AppDbContext db)
{
    public async Task<List<RankingEntryDto>> BuildAsync(
        Group group, DateOnly? from, DateOnly? to, DateOnly today, CancellationToken ct)
    {
        var id = group.Id;

        var members = await db.Members.AsNoTracking()
            .Where(m => m.GroupId == id)
            .Select(m => new { m.UserId, m.User.Name })
            .ToListAsync(ct);

        var logs = db.StudyLogs.AsNoTracking().Where(l => l.GroupId == id);
        if (from is { } start) logs = logs.Where(l => l.Date >= start);
        if (to is { } end) logs = logs.Where(l => l.Date <= end);

        var totals = (await logs
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

        var datesByUser = (await logs
                .Select(l => new { l.UserId, l.Date })
                .Distinct()
                .ToListAsync(ct))
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Date).ToHashSet());

        // Num desafio já encerrado, as sequências são contadas até o dia da prova, não até hoje.
        var reference = to is { } last && last < today ? last : today;

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

            var (currentWeeks, bestWeeks) = WeekStreaks(dates, group.MinDaysPerWeek, reference);

            return new RankingEntryDto(m.UserId, m.Name, questions, correct, accuracy, minutes,
                dates.Count, CurrentDayStreak(dates, reference), currentWeeks, bestWeeks);
        });

        // Ordem padrão: constância (dias ativos); empate por tempo e depois por questões.
        return entries
            .OrderByDescending(e => e.DaysStudied)
            .ThenByDescending(e => e.TotalMinutes ?? 0)
            .ThenByDescending(e => e.TotalQuestions ?? 0)
            .ThenBy(e => e.Name)
            .ToList();
    }

    /// <summary>Dias seguidos até a referência. Se ainda não estudou nela, a sequência segue viva a partir de ontem.</summary>
    public static int CurrentDayStreak(HashSet<DateOnly> dates, DateOnly reference)
    {
        var day = dates.Contains(reference) ? reference : reference.AddDays(-1);
        var streak = 0;
        while (dates.Contains(day))
        {
            streak++;
            day = day.AddDays(-1);
        }
        return streak;
    }

    /// <summary>
    /// Semanas (segunda a domingo) seguidas com pelo menos <paramref name="minDays"/> dias estudados.
    /// A semana em andamento ainda não quebrou a sequência: se ela não atingiu o mínimo, a contagem parte da anterior.
    /// </summary>
    public static (int Current, int Best) WeekStreaks(IEnumerable<DateOnly> dates, int minDays, DateOnly reference)
    {
        var active = dates
            .GroupBy(WeekStart)
            .Where(g => g.Count() >= minDays)
            .Select(g => g.Key)
            .ToHashSet();
        if (active.Count == 0) return (0, 0);

        var week = WeekStart(reference);
        if (!active.Contains(week)) week = week.AddDays(-7);
        var current = 0;
        while (active.Contains(week))
        {
            current++;
            week = week.AddDays(-7);
        }

        var best = 0;
        var run = 0;
        DateOnly? previous = null;
        foreach (var w in active.OrderBy(x => x))
        {
            run = previous is { } p && w == p.AddDays(7) ? run + 1 : 1;
            best = Math.Max(best, run);
            previous = w;
        }
        return (current, best);
    }

    /// <summary>Segunda-feira da semana da data.</summary>
    public static DateOnly WeekStart(DateOnly d) => d.AddDays(-(((int)d.DayOfWeek + 6) % 7));
}
