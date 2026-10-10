using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using ToolBook.Bookings.Domain;
using ToolBook.Bookings.Persistence;
using ToolBook.SharedKernel.Http;
using ToolBook.SharedKernel.Results;
using ToolBook.SharedKernel.Time;
using ToolBook.Users.Contracts;

namespace ToolBook.Bookings.Features.CancelBooking;

internal sealed class CancelBookingHandler(BookingsDbContext db, IClock clock)
{
    public async Task<Result> HandleAsync(Guid bookingId, Guid userId, bool isAdmin, CancellationToken ct)
    {
        var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, ct);
        if (booking is null)
        {
            return BookingErrors.NotFound(bookingId);
        }

        if (booking.UserId != userId && !isAdmin)
        {
            return BookingErrors.NotOwner;
        }

        var reason = booking.UserId == userId ? null : "Cancelled by an admin";
        var result = booking.Cancel(clock.UtcNow, reason);
        if (result.IsSuccess)
        {
            await db.SaveChangesAsync(ct);
        }

        return result;
    }
}

internal static class CancelBookingEndpoint
{
    public static void MapCancelBooking(this IEndpointRouteBuilder app) =>
        app.MapPost("/{id:guid}/cancel", async (Guid id, ClaimsPrincipal user, CancelBookingHandler handler, CancellationToken ct) =>
                (await handler.HandleAsync(id, user.GetUserId(), user.IsInRole(Roles.Admin), ct)).ToHttp(TypedResults.NoContent))
            .WithName("CancelBooking");
}
