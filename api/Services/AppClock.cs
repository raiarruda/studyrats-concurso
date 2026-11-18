using Microsoft.Extensions.Options;

namespace StudyRats.Api.Services;

public class ClockOptions
{
    public string TimeZone { get; set; } = "America/Sao_Paulo";
}

/// <summary>"Hoje" no fuso do grupo de estudo, não em UTC (22h em Brasília já é o dia seguinte em UTC).</summary>
public class AppClock(IOptions<ClockOptions> options)
{
    private readonly TimeZoneInfo _tz = TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZone);

    public DateOnly Today() =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _tz));
}
