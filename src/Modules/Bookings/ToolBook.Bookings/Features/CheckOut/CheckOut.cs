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

namespace ToolBook.Bookings.Features.CheckOut;

/// <summary>Records the actual end of use. An admin can also close a session someone forgot to check out of.</summary>
internal sealed class CheckOutHandler(BookingsDbContext db, IClock clock)
{
    public async Task<Result<BookingResponse>> HandleAsync(Guid bookingId, Guid userId, bool isAdmin, CancellationToken ct)
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

        var result = booking.CheckOut(clock.UtcNow);
        if (!result.IsSuccess)
        {
            return result.Error;
        }

        await db.SaveChangesAsync(ct);
        return BookingResponse.From(booking);
    }
}

internal static class CheckOutEndpoint
{
    public static void MapCheckOut(this IEndpointRouteBuilder app) =>
        app.MapPost("/{id:guid}/check-out", async (Guid id, ClaimsPrincipal user, CheckOutHandler handler, CancellationToken ct) =>
                (await handler.HandleAsync(id, user.GetUserId(), user.IsInRole(Roles.Admin), ct)).ToHttp(TypedResults.Ok))
            .WithName("CheckOut");
}
