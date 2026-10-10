using ToolBook.Bookings.Domain;

namespace ToolBook.UnitTests.Bookings;

public class BookingLifecycleTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 12, 10, 0, 0, TimeSpan.Zero);

    private static Booking NewBooking() =>
        ToolSchedule.For(Guid.NewGuid()).Book(Guid.NewGuid(), Start, Start.AddHours(2), "x", [], Start.AddDays(-1)).Value;

    [Fact]
    public void Check_in_within_window_marks_in_use_and_records_actual_time()
    {
        var booking = NewBooking();
        var at = Start - Booking.CheckInWindow;

        var result = booking.CheckIn(at);

        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatus.InUse, booking.Status);
        Assert.Equal(at, booking.CheckedInAt);
    }

    [Fact]
    public void Check_in_too_early_is_rejected()
    {
        var booking = NewBooking();

        var result = booking.CheckIn(Start - Booking.CheckInWindow - TimeSpan.FromMinutes(1));

        Assert.Equal("Bookings.OutsideCheckInWindow", result.Error?.Code);
        Assert.Equal(BookingStatus.Scheduled, booking.Status);
    }

    [Fact]
    public void Check_out_completes_and_records_actual_time_even_after_overrun()
    {
        var booking = NewBooking();
        booking.CheckIn(Start);
        var overrun = Start.AddHours(2).AddMinutes(12);

        var result = booking.CheckOut(overrun);

        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatus.Completed, booking.Status);
        Assert.Equal(overrun, booking.CheckedOutAt);
    }

    [Fact]
    public void Check_out_without_check_in_is_rejected()
    {
        var result = NewBooking().CheckOut(Start);

        Assert.Equal("Bookings.InvalidStatus", result.Error?.Code);
    }

    [Fact]
    public void Cannot_cancel_a_booking_in_use()
    {
        var booking = NewBooking();
        booking.CheckIn(Start);

        var result = booking.Cancel(Start.AddMinutes(5));

        Assert.False(result.IsSuccess);
        Assert.Equal(BookingStatus.InUse, booking.Status);
    }

    [Fact]
    public void Cancel_records_reason()
    {
        var booking = NewBooking();

        booking.Cancel(Start.AddHours(-3), "tool out of service");

        Assert.Equal(BookingStatus.Cancelled, booking.Status);
        Assert.Equal("tool out of service", booking.CancellationReason);
        Assert.False(booking.IsActive);
    }
}
