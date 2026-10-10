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

namespace ToolBook.Bookings.Features.CheckIn;

/// <summary>Records the actual start of use. Actual times feed the availability estimate.</summary>
internal sealed class CheckInHandler(BookingsDbContext db, IClock clock)
{
    public async Task<Result<BookingResponse>> HandleAsync(Guid bookingId, Guid userId, CancellationToken ct)
    {
        var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, ct);
        if (booking is null)
        {
            return BookingErrors.NotFound(bookingId);
        }

        if (booking.UserId != userId)
        {
            return BookingErrors.NotOwner;
        }

        var result = booking.CheckIn(clock.UtcNow);
        if (!result.IsSuccess)
        {
            return result.Error;
        }

        await db.SaveChangesAsync(ct);
        return BookingResponse.From(booking);
    }
}

internal static class CheckInEndpoint
{
    public static void MapCheckIn(this IEndpointRouteBuilder app) =>
        app.MapPost("/{id:guid}/check-in", async (Guid id, ClaimsPrincipal user, CheckInHandler handler, CancellationToken ct) =>
                (await handler.HandleAsync(id, user.GetUserId(), ct)).ToHttp(TypedResults.Ok))
            .WithName("CheckIn");
}
