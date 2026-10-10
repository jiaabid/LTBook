using ToolBook.Bookings.Domain.Availability;

namespace ToolBook.UnitTests.Bookings;

public class AvailabilityEstimatorTests
{
    // 2026-10-12 is in CEST (UTC+2): 12:00 UTC is 14:00 in Berlin.
    private static readonly TimeZoneInfo Berlin = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");
    private static readonly DateTimeOffset Now = Utc(12, 0);
    private static readonly DateTimeOffset EndOfDay = Utc(22, 0); // midnight in Berlin
    private static readonly TimeSpan OneHour = TimeSpan.FromHours(1);

    private static DateTimeOffset Utc(int hour, int minute = 0) => new(2026, 10, 12, hour, minute, 0, TimeSpan.Zero);

    private static TimeSpan[] Minutes(params int[] values) => [.. values.Select(v => TimeSpan.FromMinutes(v))];

    private static AvailabilityEstimate Estimate(ScheduledSlot[] bookings, TimeSpan[]? history = null, TimeSpan? minimumSlot = null) =>
        AvailabilityEstimator.Estimate(new AvailabilityInput(Now, minimumSlot ?? OneHour, bookings, history ?? [], EndOfDay, Berlin));

    [Fact]
    public void Free_now_when_nothing_is_booked()
    {
        var result = Estimate([]);

        Assert.Equal(AvailabilityStatus.Free, result.Status);
        Assert.True(result.FreeNow);
        Assert.Equal(Now, result.EstimatedFreeAt);
        Assert.Equal(new FreeSlot(Now, EndOfDay), result.NextFreeSlot);
        Assert.Equal("Free now for the rest of the day.", result.Explanation);
    }

    [Fact]
    public void Free_now_until_the_next_booking()
    {
        var result = Estimate([new ScheduledSlot(Utc(14), Utc(15), CheckedIn: false)]);

        Assert.True(result.FreeNow);
        Assert.Equal(new FreeSlot(Now, Utc(14)), result.NextFreeSlot);
        Assert.Equal("Free now until 16:00.", result.Explanation);
    }

    [Fact]
    public void Free_now_but_next_booking_too_close_skips_to_the_following_gap()
    {
        var result = Estimate([new ScheduledSlot(Utc(12, 30), Utc(13, 30), CheckedIn: false)]);

        Assert.True(result.FreeNow);
        Assert.Equal(new FreeSlot(Utc(13, 30), EndOfDay), result.NextFreeSlot);
        Assert.StartsWith("Free now, but booked from 14:30.", result.Explanation);
    }

    [Fact]
    public void In_use_without_history_uses_scheduled_end_with_low_confidence()
    {
        var result = Estimate([new ScheduledSlot(Utc(11), Utc(12, 30), CheckedIn: true)], history: Minutes(10, 20));

        Assert.Equal(AvailabilityStatus.InUse, result.Status);
        Assert.False(result.FreeNow);
        Assert.Equal(Utc(12, 30), result.EstimatedFreeAt);
        Assert.Equal(EstimateConfidence.Low, result.Confidence);
        Assert.Null(result.TypicalOverrun);
        Assert.Contains("not enough history", result.Explanation);
    }

    [Fact]
    public void In_use_with_history_adds_the_median_overrun()
    {
        // Median of {-5, 8, 10, 12, 40} is 10 min. 12:30 + 10 min = 12:40 UTC = 14:40 Berlin.
        var result = Estimate([new ScheduledSlot(Utc(11), Utc(12, 30), CheckedIn: true)], history: Minutes(40, 10, -5, 12, 8));

        Assert.Equal(Utc(12, 40), result.EstimatedFreeAt);
        Assert.Equal(TimeSpan.FromMinutes(10), result.TypicalOverrun);
        Assert.Equal(EstimateConfidence.Medium, result.Confidence);
        Assert.Equal(new FreeSlot(Utc(12, 40), EndOfDay), result.NextFreeSlot);
        Assert.Equal("In use until ~14:40; sessions usually run 10 min over. Next free 14:40–end of day.", result.Explanation);
    }

    [Fact]
    public void Lots_of_history_gives_high_confidence()
    {
        var result = Estimate([new ScheduledSlot(Utc(11), Utc(12, 30), CheckedIn: true)], history: Minutes(5, 5, 5, 5, 5, 5, 5, 5));

        Assert.Equal(EstimateConfidence.High, result.Confidence);
    }

    [Fact]
    public void Estimate_is_rounded_up_to_five_minutes()
    {
        var result = Estimate([new ScheduledSlot(Utc(11), Utc(12, 30), CheckedIn: true)], history: Minutes(7, 7, 7));

        Assert.Equal(Utc(12, 40), result.EstimatedFreeAt);
    }

    [Fact]
    public void Back_to_back_bookings_push_the_next_free_slot_past_the_chain()
    {
        ScheduledSlot[] bookings =
        [
            new(Utc(11), Utc(12, 30), CheckedIn: true),
            new(Utc(12, 30), Utc(13, 30), CheckedIn: false),
            new(Utc(13, 30), Utc(15), CheckedIn: false),
            new(Utc(17), Utc(18), CheckedIn: false),
        ];

        var result = Estimate(bookings);

        Assert.Equal(new FreeSlot(Utc(15), Utc(17)), result.NextFreeSlot);
        Assert.EndsWith("Next free 17:00–19:00.", result.Explanation);
    }

    [Fact]
    public void Gap_shorter_than_the_minimum_slot_is_skipped()
    {
        ScheduledSlot[] bookings =
        [
            new(Utc(11), Utc(12, 30), CheckedIn: true),
            new(Utc(13), Utc(14), CheckedIn: false), // 30 min gap before it: too short for a 60 min slot
        ];

        var result = Estimate(bookings);

        Assert.Equal(new FreeSlot(Utc(14), EndOfDay), result.NextFreeSlot);
    }

    [Fact]
    public void No_gap_left_today()
    {
        ScheduledSlot[] bookings =
        [
            new(Utc(11), Utc(13), CheckedIn: true),
            new(Utc(13), Utc(17), CheckedIn: false),
            new(Utc(17), Utc(21, 30), CheckedIn: false), // only 30 min left before midnight in Berlin
        ];

        var result = Estimate(bookings);

        Assert.Null(result.NextFreeSlot);
        Assert.EndsWith("No free slot of 60 min left today.", result.Explanation);
    }

    [Fact]
    public void Session_running_past_its_adjusted_end_is_expected_any_minute()
    {
        var result = Estimate([new ScheduledSlot(Utc(10), Utc(11, 30), CheckedIn: true)], history: Minutes(5, 5, 5));

        Assert.Equal(AvailabilityStatus.InUse, result.Status);
        Assert.Equal(Now, result.EstimatedFreeAt);
        Assert.Equal(EstimateConfidence.Low, result.Confidence);
        Assert.Contains("should be free any minute", result.Explanation);
    }

    [Fact]
    public void Booked_but_not_checked_in_is_reported_as_booked()
    {
        var result = Estimate([new ScheduledSlot(Utc(11, 30), Utc(13), CheckedIn: false)]);

        Assert.Equal(AvailabilityStatus.Booked, result.Status);
        Assert.Equal(Utc(13), result.EstimatedFreeAt);
    }

    [Theory]
    [InlineData(new[] { 10, 20 }, null)]               // too little history
    [InlineData(new[] { 1, 2, 3 }, 2.0)]               // odd count
    [InlineData(new[] { 1, 2, 3, 100 }, 2.5)]          // even count: mean of the middle two; robust to the outlier
    [InlineData(new[] { -10, -5, -1 }, -5.0)]          // tools that usually finish early
    public void Typical_overrun_is_the_median(int[] samples, double? expectedMinutes)
    {
        var (overrun, _) = AvailabilityEstimator.TypicalOverrun(Minutes(samples));

        Assert.Equal(expectedMinutes, overrun?.TotalMinutes);
    }
}
