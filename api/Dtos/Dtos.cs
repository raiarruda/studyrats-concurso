using System.ComponentModel.DataAnnotations;

namespace StudyRats.Api.Dtos;

public record RegisterRequest(
    [Required, StringLength(80, MinimumLength = 2)] string Name,
    [Required, EmailAddress, StringLength(200)] string Email,
    [Required, StringLength(100, MinimumLength = 8)] string Password);

public record LoginRequest(
    [Required] string Email,
    [Required] string Password);

public record UserDto(Guid Id, string Name, string Email);
public record AuthResponse(string Token, UserDto User);

public record CreateGroupRequest(
    [Required, StringLength(80, MinimumLength = 2)] string Name,
    bool TrackQuestions,
    bool TrackCorrect,
    bool TrackTime,
    [Range(1, 7)] int MinDaysPerWeek = 3);

public record JoinGroupRequest([Required] string Code);

public record GroupSummaryDto(
    Guid Id, string Name, string Code,
    bool TrackQuestions, bool TrackCorrect, bool TrackTime,
    int MinDaysPerWeek, DateTime CreatedAt, int MemberCount, bool IsOwner);

public record MemberDto(Guid UserId, string Name, bool IsOwner);

public record GroupDetailDto(GroupSummaryDto Group, List<MemberDto> Members);

/// <summary>Bolão (simulado) opcional do desafio: valores em centavos e porcentagens do prêmio.</summary>
public record CreateChallengeRequest(
    [Required, StringLength(80, MinimumLength = 2)] string Name,
    DateOnly StartDate,
    DateOnly ExamDate,
    [Range(1, 100_000)] int? PenaltyCents = null,
    List<int>? PrizePercents = null);

public record BetConfigDto(int PenaltyCents, List<int> PrizePercents);

public record ChallengeDto(Guid Id, Guid GroupId, string Name, DateOnly StartDate, DateOnly ExamDate, BetConfigDto? Bet);

/// <summary>Semanas: "ok", "failed", "open" (em andamento), "future" ou "na" (antes de entrar no bolão).</summary>
public record BolaoMemberDto(
    Guid UserId, string Name, DateOnly JoinedOn, int Position,
    int FailedWeeks, int PenaltyCents, int PrizeCents, int NetCents, List<string> Weeks);

public record BolaoDto(
    int PenaltyCents, List<int> PrizePercents, bool IsFinal,
    bool IAmIn, bool CanJoin, bool CanLeave,
    int PotCents, int CaixaCents,
    List<DateOnly> Weeks, List<BolaoMemberDto> Members);

public record LogDto(
    Guid Id, Guid UserId, string UserName, DateOnly Date, string PhotoUrl,
    int? QuestionsAnswered, int? QuestionsCorrect, int? StudyMinutes,
    string? Subject, string? Note, DateTime CreatedAt);

public record RankingEntryDto(
    Guid UserId, string Name,
    int? TotalQuestions, int? TotalCorrect, double? AccuracyPercent, int? TotalMinutes,
    int DaysStudied, int CurrentStreak, int CurrentWeekStreak, int BestWeekStreak);

/// <summary>Um dia com estudo no calendário: foto do registro mais recente e totais das métricas do grupo.</summary>
public record CalendarDayDto(DateOnly Date, int LogsCount, string PhotoUrl, int? Questions, int? Minutes);

/// <summary>
/// Perfil de uma pessoa: estatísticas de todos os registros visíveis ao visitante (grupos em comum)
/// e os dias do mês pedido para o calendário. Métrica sem nenhum registro vem como null.
/// </summary>
public record ProfileDto(
    Guid UserId, string Name, bool IsMe,
    int TotalLogs, int ActiveDays, int? TotalMinutes, int? TotalQuestions, int CurrentStreak,
    DateOnly? FirstActiveDate, List<CalendarDayDto> Days);
