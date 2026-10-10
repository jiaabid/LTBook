using ToolBook.SharedKernel.Results;

namespace ToolBook.Bookings.Domain;

/// <summary>
/// One row per tool that serialises booking creation for that tool (ADR-0006).
/// Every new booking increments <see cref="BookingsCreated"/>, so the row is always updated in the same
/// transaction as the insert. Its <c>rowversion</c> makes a concurrent writer that read the same version fail.
/// </summary>
internal sealed class ToolSchedule
{
    private ToolSchedule(Guid toolId) => ToolId = toolId;

    public Guid ToolId { get; private set; }

    public long BookingsCreated { get; private set; }

    /// <summary>SQL Server <c>rowversion</c>, the optimistic concurrency token.</summary>
    public byte[] Version { get; private set; } = [];

    public static ToolSchedule For(Guid toolId) => new(toolId);

    /// <param name="existingBookings">The tool's bookings that might overlap the requested range (loaded by the caller).</param>
    public Result<Booking> Book(
        Guid userId,
        DateTimeOffset start,
        DateTimeOffset end,
        string purpose,
        IEnumerable<Booking> existingBookings,
        DateTimeOffset now)
    {
        if (end <= start)
        {
            return BookingErrors.EndBeforeStart;
        }

        if (end - start < Booking.MinDuration || end - start > Booking.MaxDuration)
        {
            return BookingErrors.InvalidDuration;
        }

        if (start < now)
        {
            return BookingErrors.InThePast;
        }

        var clash = existingBookings.FirstOrDefault(b => b.ToolId == ToolId && b.IsActive && b.Overlaps(start, end));
        if (clash is not null)
        {
            return BookingErrors.Overlap(clash);
        }

        BookingsCreated++;
        return Booking.Create(ToolId, userId, start, end, purpose, now);
    }
}
