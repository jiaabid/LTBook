using ToolBook.Bookings.Domain;

namespace ToolBook.Bookings.Features;

/// <summary>API representation of a booking, shared by the booking slices. Times are UTC (ISO 8601).</summary>
internal sealed record BookingResponse(
    Guid Id,
    Guid ToolId,
    string? ToolName,
    Guid UserId,
    string? UserDisplayName,
    DateTimeOffset Start,
    DateTimeOffset End,
    string Purpose,
    BookingStatus Status,
    DateTimeOffset? CheckedInAt,
    DateTimeOffset? CheckedOutAt,
    string? CancellationReason)
{
    public static BookingResponse From(Booking b, string? toolName = null, string? userDisplayName = null) =>
        new(b.Id, b.ToolId, toolName, b.UserId, userDisplayName, b.Start, b.End, b.Purpose, b.Status,
            b.CheckedInAt, b.CheckedOutAt, b.CancellationReason);
}
