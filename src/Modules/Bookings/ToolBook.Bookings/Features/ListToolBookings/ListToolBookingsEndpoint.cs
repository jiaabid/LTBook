using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using ToolBook.Bookings.Domain;
using ToolBook.Bookings.Persistence;
using ToolBook.Bookings.Scheduling;
using ToolBook.SharedKernel.Time;
using ToolBook.Users.Contracts;

namespace ToolBook.Bookings.Features.ListToolBookings;

internal sealed record ToolDayResponse(Guid ToolId, DateOnly Date, string TimeZone, IReadOnlyList<BookingResponse> Bookings);

internal static class ListToolBookingsEndpoint
{
    public static void MapListToolBookings(this IEndpointRouteBuilder app) =>
        app.MapGet("/{toolId:guid}/bookings", HandleAsync).WithName("ListToolBookings");

    /// <summary>Non-cancelled bookings of a tool that overlap one local lab day (default: today).</summary>
    private static async Task<Ok<ToolDayResponse>> HandleAsync(
        Guid toolId,
        BookingsDbContext db,
        IUserDirectory users,
        LabTimeZone lab,
        IClock clock,
        CancellationToken ct,
        DateOnly? date = null)
    {
        var day = date ?? lab.LocalDate(clock.UtcNow);
        var (start, end) = lab.DayBounds(day);

        var bookings = await db.Bookings.AsNoTracking()
            .Where(b => b.ToolId == toolId && b.Status != BookingStatus.Cancelled && b.Start < end && start < b.End)
            .OrderBy(b => b.Start)
            .ToListAsync(ct);

        var names = await users.GetDisplayNamesAsync(bookings.Select(b => b.UserId), ct);
        var items = bookings.ConvertAll(b => BookingResponse.From(b, userDisplayName: names.GetValueOrDefault(b.UserId)));
        return TypedResults.Ok(new ToolDayResponse(toolId, day, lab.Zone.Id, items));
    }
}
