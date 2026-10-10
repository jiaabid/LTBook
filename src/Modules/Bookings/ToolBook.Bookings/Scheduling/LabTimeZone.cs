using Microsoft.Extensions.Configuration;

namespace ToolBook.Bookings.Scheduling;

/// <summary>
/// The lab's local time zone (<c>Lab:TimeZone</c>, default Europe/Berlin). Times are stored and transported in UTC.
/// Local time is only used to define "a day" and to word explanations.
/// </summary>
internal sealed class LabTimeZone(TimeZoneInfo zone)
{
    public const string DefaultZoneId = "Europe/Berlin";

    public static LabTimeZone FromConfiguration(IConfiguration configuration) =>
        new(TimeZoneInfo.FindSystemTimeZoneById(configuration["Lab:TimeZone"] ?? DefaultZoneId));

    public TimeZoneInfo Zone { get; } = zone;

    public DateTimeOffset ToLocal(DateTimeOffset instant) => TimeZoneInfo.ConvertTime(instant, Zone);

    public DateOnly LocalDate(DateTimeOffset instant) => DateOnly.FromDateTime(ToLocal(instant).DateTime);

    /// <summary>The UTC instants at which the given local day starts and ends (DST-aware: a day can be 23 or 25 hours).</summary>
    public (DateTimeOffset Start, DateTimeOffset End) DayBounds(DateOnly day) =>
        (StartOfDay(day), StartOfDay(day.AddDays(1)));

    private DateTimeOffset StartOfDay(DateOnly day)
    {
        var local = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, Zone), TimeSpan.Zero);
    }
}
