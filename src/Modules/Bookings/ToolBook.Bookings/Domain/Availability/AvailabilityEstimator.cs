using System.Globalization;

namespace ToolBook.Bookings.Domain.Availability;

internal enum AvailabilityStatus
{
    /// <summary>No booking covers the current time.</summary>
    Free,

    /// <summary>Someone has checked in and is using the tool.</summary>
    InUse,

    /// <summary>A booking covers the current time, but nobody has checked in yet.</summary>
    Booked,
}

internal enum EstimateConfidence
{
    Low,
    Medium,
    High,
}

/// <summary>An active booking as the estimator sees it.</summary>
internal sealed record ScheduledSlot(DateTimeOffset Start, DateTimeOffset End, bool CheckedIn);

internal sealed record FreeSlot(DateTimeOffset Start, DateTimeOffset End);

internal sealed record AvailabilityInput(
    DateTimeOffset Now,
    TimeSpan MinimumSlot,
    IReadOnlyList<ScheduledSlot> ActiveBookings,
    IReadOnlyList<TimeSpan> RecentOverruns,
    DateTimeOffset SearchUntil,
    TimeZoneInfo TimeZone);

internal sealed record AvailabilityEstimate(
    AvailabilityStatus Status,
    bool FreeNow,
    DateTimeOffset EstimatedFreeAt,
    FreeSlot? NextFreeSlot,
    EstimateConfidence Confidence,
    TimeSpan? TypicalOverrun,
    string Explanation);

/// <summary>
/// Deterministic "when will this tool be free?" estimate (ADR-0007). Pure: no I/O, no clock, no randomness.
/// <list type="number">
/// <item>The current booking's scheduled end is the base estimate.</item>
/// <item>It is shifted by the tool's typical overrun: the median of (actual check-out − scheduled end) over recent
/// completed bookings. With fewer than <see cref="MinHistory"/> samples it is not adjusted, and confidence is Low.</item>
/// <item>From there it walks forward through later bookings to the first gap of at least the tool's minimum slot.</item>
/// </list>
/// </summary>
internal static class AvailabilityEstimator
{
    public const int MinHistory = 3;
    public const int HighConfidenceHistory = 8;
    private static readonly TimeSpan Rounding = TimeSpan.FromMinutes(5);

    public static AvailabilityEstimate Estimate(AvailabilityInput input)
    {
        var now = input.Now;
        var bookings = input.ActiveBookings.OrderBy(b => b.Start).ToList();

        // A checked-in session is current even if it has run past its scheduled end.
        var current = bookings.FirstOrDefault(b => b.CheckedIn)
                      ?? bookings.FirstOrDefault(b => b.Start <= now && now < b.End);

        var (overrun, confidence) = TypicalOverrun(input.RecentOverruns);

        DateTimeOffset estimatedFreeAt;
        AvailabilityStatus status;
        if (current is null)
        {
            status = AvailabilityStatus.Free;
            estimatedFreeAt = now;
        }
        else
        {
            status = current.CheckedIn ? AvailabilityStatus.InUse : AvailabilityStatus.Booked;
            var adjustedEnd = current.End + (overrun ?? TimeSpan.Zero);
            if (adjustedEnd <= now)
            {
                // Already running longer than usual: it should end any minute, but we cannot say when.
                estimatedFreeAt = now;
                confidence = EstimateConfidence.Low;
            }
            else
            {
                estimatedFreeAt = RoundUp(adjustedEnd, Rounding);
            }
        }

        var later = bookings.Where(b => b != current);
        var nextFreeSlot = FindGap(estimatedFreeAt, later, input.MinimumSlot, input.SearchUntil);

        var estimate = new AvailabilityEstimate(
            status,
            FreeNow: status == AvailabilityStatus.Free,
            estimatedFreeAt,
            nextFreeSlot,
            status == AvailabilityStatus.Free ? EstimateConfidence.High : confidence,
            overrun,
            Explanation: string.Empty);

        return estimate with { Explanation = Explain(estimate, current, input) };
    }

    /// <summary>Median of the recent overruns (negative = finished early), or null with Low confidence if there is too little history.</summary>
    internal static (TimeSpan? Overrun, EstimateConfidence Confidence) TypicalOverrun(IReadOnlyList<TimeSpan> samples)
    {
        if (samples.Count < MinHistory)
        {
            return (null, EstimateConfidence.Low);
        }

        var sorted = samples.Order().ToList();
        var mid = sorted.Count / 2;
        var median = sorted.Count % 2 == 1 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2;
        var confidence = samples.Count >= HighConfidenceHistory ? EstimateConfidence.High : EstimateConfidence.Medium;
        return (median, confidence);
    }

    private static FreeSlot? FindGap(DateTimeOffset from, IEnumerable<ScheduledSlot> later, TimeSpan minimum, DateTimeOffset until)
    {
        var cursor = from;
        foreach (var booking in later)
        {
            if (booking.End <= cursor)
            {
                continue;
            }

            if (booking.Start - cursor >= minimum)
            {
                return new FreeSlot(cursor, booking.Start);
            }

            cursor = booking.End > cursor ? booking.End : cursor;
        }

        return until - cursor >= minimum ? new FreeSlot(cursor, until) : null;
    }

    private static string Explain(AvailabilityEstimate e, ScheduledSlot? current, AvailabilityInput input)
    {
        string Time(DateTimeOffset t) => TimeZoneInfo.ConvertTime(t, input.TimeZone).ToString("HH:mm", CultureInfo.InvariantCulture);

        var parts = new List<string>();
        if (current is null)
        {
            var nextBooking = input.ActiveBookings.Where(b => b.Start >= input.Now).MinBy(b => b.Start);
            parts.Add(e.NextFreeSlot switch
            {
                { } s when s.Start == input.Now && s.End == input.SearchUntil => "Free now for the rest of the day.",
                { } s when s.Start == input.Now => $"Free now until {Time(s.End)}.",
                _ when nextBooking is not null => $"Free now, but booked from {Time(nextBooking.Start)}.",
                _ => "Free now.",
            });
        }
        else
        {
            var what = e.Status == AvailabilityStatus.InUse ? "In use" : "Booked (not checked in yet)";
            if (e.EstimatedFreeAt == input.Now)
            {
                parts.Add($"{what}, already past its scheduled end of {Time(current.End)}; should be free any minute.");
            }
            else if (e.TypicalOverrun is { } overrun)
            {
                parts.Add($"{what} until ~{Time(e.EstimatedFreeAt)}; {DescribeOverrun(overrun)}.");
            }
            else
            {
                parts.Add($"{what} until {Time(current.End)}; not enough history to adjust the estimate.");
            }
        }

        if (e.NextFreeSlot is { } slot)
        {
            if (current is not null || slot.Start != input.Now)
            {
                var end = slot.End == input.SearchUntil ? "end of day" : Time(slot.End);
                parts.Add($"Next free {Time(slot.Start)}–{end}.");
            }
        }
        else
        {
            parts.Add($"No free slot of {input.MinimumSlot.TotalMinutes:0} min left today.");
        }

        return string.Join(" ", parts);
    }

    private static string DescribeOverrun(TimeSpan overrun)
    {
        var minutes = (int)Math.Round(overrun.TotalMinutes);
        return minutes switch
        {
            > 0 => $"sessions usually run {minutes} min over",
            < 0 => $"sessions usually end {-minutes} min early",
            _ => "sessions usually end on time",
        };
    }

    private static DateTimeOffset RoundUp(DateTimeOffset t, TimeSpan step)
    {
        var remainder = t.UtcTicks % step.Ticks;
        return remainder == 0 ? t : t.AddTicks(step.Ticks - remainder);
    }
}
