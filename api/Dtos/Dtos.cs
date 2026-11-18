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
    DateOnly StartDate,
    DateOnly EndDate,
    bool TrackQuestions,
    bool TrackCorrect,
    bool TrackTime);

public record JoinGroupRequest([Required] string Code);

public record GroupSummaryDto(
    Guid Id, string Name, string Code, DateOnly StartDate, DateOnly EndDate,
    bool TrackQuestions, bool TrackCorrect, bool TrackTime, int MemberCount, bool IsOwner);

public record MemberDto(Guid UserId, string Name, bool IsOwner);

public record GroupDetailDto(GroupSummaryDto Group, List<MemberDto> Members);

public record LogDto(
    Guid Id, Guid UserId, string UserName, DateOnly Date, string PhotoUrl,
    int? QuestionsAnswered, int? QuestionsCorrect, int? StudyMinutes,
    string? Subject, string? Note, DateTime CreatedAt);

public record RankingEntryDto(
    Guid UserId, string Name,
    int? TotalQuestions, int? TotalCorrect, double? AccuracyPercent, int? TotalMinutes,
    int DaysStudied, int CurrentStreak);
