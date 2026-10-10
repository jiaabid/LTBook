using ToolBook.SharedKernel.Results;

namespace ToolBook.Bookings.Domain;

internal static class BookingErrors
{
    public static readonly Error EndBeforeStart =
        Error.Validation("Bookings.EndBeforeStart", "The booking must end after it starts.");

    public static readonly Error InvalidDuration = Error.Validation(
        "Bookings.InvalidDuration",
        $"A booking must last between {Booking.MinDuration.TotalMinutes:0} minutes and {Booking.MaxDuration.TotalHours:0} hours.");

    public static readonly Error InThePast =
        Error.Validation("Bookings.InThePast", "A booking cannot start in the past.");

    public static readonly Error ConcurrentUpdate = Error.Conflict(
        "Bookings.ConcurrentUpdate",
        "The tool's schedule changed while booking. Please try again.");

    public static readonly Error NotOwner =
        Error.Forbidden("Bookings.NotOwner", "Only the person who made the booking (or an admin) can do this.");

    public static Error Overlap(Booking clash) => Error.Conflict(
        "Bookings.Overlap",
        $"The tool is already booked from {clash.Start:u} to {clash.End:u}.");

    public static Error NotFound(Guid id) => Error.NotFound("Bookings.NotFound", $"Booking {id} was not found.");

    public static Error ToolNotFound(Guid id) => Error.NotFound("Bookings.ToolNotFound", $"Tool {id} was not found.");

    public static Error ToolInactive(string name) =>
        Error.Conflict("Bookings.ToolInactive", $"{name} is out of service and cannot be booked.");

    public static Error InvalidTransition(BookingStatus status, string action) =>
        Error.Conflict("Bookings.InvalidStatus", $"Cannot {action} a booking that is {status}.");

    public static Error OutsideCheckInWindow(TimeSpan window) => Error.Conflict(
        "Bookings.OutsideCheckInWindow",
        $"Check-in is possible from {window.TotalMinutes:0} minutes before the start until the end of the booking.");
}
