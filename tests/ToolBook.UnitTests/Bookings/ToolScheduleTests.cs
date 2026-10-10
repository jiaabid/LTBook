using ToolBook.Bookings.Domain;

namespace ToolBook.UnitTests.Bookings;

public class ToolScheduleTests
{
    private static readonly Guid ToolId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 10, 12, 8, 0, 0, TimeSpan.Zero);

    private static Booking Existing(int startHour, int endHour)
    {
        var schedule = ToolSchedule.For(ToolId);
        return schedule.Book(Guid.NewGuid(), At(startHour), At(endHour), "existing", [], Now).Value;
    }

    private static DateTimeOffset At(int hour) => new(2026, 10, 12, hour, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Books_a_free_slot()
    {
        var result = ToolSchedule.For(ToolId).Book(UserId, At(10), At(12), "imaging", [], Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatus.Scheduled, result.Value.Status);
        Assert.Equal(At(10), result.Value.Start);
        Assert.Equal(UserId, result.Value.UserId);
    }

    [Theory]
    [InlineData(9, 11)]   // overlaps the start
    [InlineData(11, 13)]  // overlaps the end
    [InlineData(10, 12)]  // identical
    [InlineData(9, 13)]   // contains
    public void Rejects_overlapping_slot(int startHour, int endHour)
    {
        var existing = Existing(10, 12);

        var result = ToolSchedule.For(ToolId).Book(UserId, At(startHour), At(endHour), "x", [existing], Now);

        Assert.False(result.IsSuccess);
        Assert.Equal("Bookings.Overlap", result.Error.Code);
    }

    [Theory]
    [InlineData(8, 10)]   // ends exactly when the other starts
    [InlineData(12, 14)]  // starts exactly when the other ends
    public void Allows_back_to_back_slots(int startHour, int endHour)
    {
        var existing = Existing(10, 12);

        var result = ToolSchedule.For(ToolId).Book(UserId, At(startHour), At(endHour), "x", [existing], Now);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Ignores_cancelled_bookings()
    {
        var existing = Existing(10, 12);
        existing.Cancel(Now);

        var result = ToolSchedule.For(ToolId).Book(UserId, At(10), At(12), "x", [existing], Now);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Rejects_start_in_the_past()
    {
        var result = ToolSchedule.For(ToolId).Book(UserId, Now.AddMinutes(-1), Now.AddHours(1), "x", [], Now);

        Assert.Equal("Bookings.InThePast", result.Error?.Code);
    }

    [Fact]
    public void Rejects_end_before_start()
    {
        var result = ToolSchedule.For(ToolId).Book(UserId, At(12), At(10), "x", [], Now);

        Assert.Equal("Bookings.EndBeforeStart", result.Error?.Code);
    }

    [Fact]
    public void Rejects_bookings_longer_than_the_maximum()
    {
        var result = ToolSchedule.For(ToolId).Book(UserId, At(9), At(9) + Booking.MaxDuration + TimeSpan.FromMinutes(1), "x", [], Now);

        Assert.Equal("Bookings.InvalidDuration", result.Error?.Code);
    }

    [Fact]
    public void Every_booking_bumps_the_schedule_so_the_row_is_updated_in_the_same_transaction()
    {
        var schedule = ToolSchedule.For(ToolId);

        schedule.Book(UserId, At(9), At(10), "a", [], Now);
        schedule.Book(UserId, At(10), At(11), "b", [], Now);

        Assert.Equal(2, schedule.BookingsCreated);
    }
}
