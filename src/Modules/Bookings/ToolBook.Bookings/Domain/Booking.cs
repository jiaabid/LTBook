using ToolBook.SharedKernel.Domain;
using ToolBook.SharedKernel.Results;

namespace ToolBook.Bookings.Domain;

internal enum BookingStatus
{
    Scheduled,
    InUse,
    Completed,
    Cancelled,
}

/// <summary>A reservation of one tool by one user for a time range. All times are UTC.</summary>
internal sealed class Booking : Entity
{
    public const int PurposeMaxLength = 200;
    public static readonly TimeSpan MinDuration = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan MaxDuration = TimeSpan.FromHours(8);

    /// <summary>How early before the scheduled start a user may check in.</summary>
    public static readonly TimeSpan CheckInWindow = TimeSpan.FromMinutes(15);

    private Booking(Guid id, Guid toolId, Guid userId, DateTimeOffset start, DateTimeOffset end, string purpose, DateTimeOffset createdAt)
        : base(id)
    {
        ToolId = toolId;
        UserId = userId;
        Start = start;
        End = end;
        Purpose = purpose;
        CreatedAt = createdAt;
        Status = BookingStatus.Scheduled;
    }

    public Guid ToolId { get; private set; }

    public Guid UserId { get; private set; }

    public DateTimeOffset Start { get; private set; }

    public DateTimeOffset End { get; private set; }

    public string Purpose { get; private set; }

    public BookingStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? CheckedInAt { get; private set; }

    public DateTimeOffset? CheckedOutAt { get; private set; }

    public DateTimeOffset? CancelledAt { get; private set; }

    public string? CancellationReason { get; private set; }

    /// <summary>Scheduled or in use: the booking still occupies the tool's calendar.</summary>
    public bool IsActive => Status is BookingStatus.Scheduled or BookingStatus.InUse;

    /// <summary>Only <see cref="ToolSchedule"/> creates bookings, so the overlap rule cannot be bypassed.</summary>
    internal static Booking Create(Guid toolId, Guid userId, DateTimeOffset start, DateTimeOffset end, string purpose, DateTimeOffset now) =>
        new(Guid.CreateVersion7(), toolId, userId, start.ToUniversalTime(), end.ToUniversalTime(), purpose.Trim(), now);

    public bool Overlaps(DateTimeOffset start, DateTimeOffset end) => Start < end && start < End;

    public Result Cancel(DateTimeOffset now, string? reason = null)
    {
        if (Status != BookingStatus.Scheduled)
        {
            return BookingErrors.InvalidTransition(Status, "cancel");
        }

        Status = BookingStatus.Cancelled;
        CancelledAt = now;
        CancellationReason = reason;
        return Result.Success();
    }

    public Result CheckIn(DateTimeOffset now)
    {
        if (Status != BookingStatus.Scheduled)
        {
            return BookingErrors.InvalidTransition(Status, "check in");
        }

        if (now < Start - CheckInWindow || now >= End)
        {
            return BookingErrors.OutsideCheckInWindow(CheckInWindow);
        }

        Status = BookingStatus.InUse;
        CheckedInAt = now;
        return Result.Success();
    }

    public Result CheckOut(DateTimeOffset now)
    {
        if (Status != BookingStatus.InUse)
        {
            return BookingErrors.InvalidTransition(Status, "check out");
        }

        Status = BookingStatus.Completed;
        CheckedOutAt = now;
        return Result.Success();
    }
}
