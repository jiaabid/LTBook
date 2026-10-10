using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using ToolBook.Bookings.Persistence;
using ToolBook.SharedKernel.Http;
using ToolBook.SharedKernel.Time;
using ToolBook.Tools.Contracts;

namespace ToolBook.Bookings.Features.ListMyBookings;

internal static class ListMyBookingsEndpoint
{
    private static readonly TimeSpan PastWindow = TimeSpan.FromDays(30);

    public static void MapListMyBookings(this IEndpointRouteBuilder app) =>
        app.MapGet("/mine", HandleAsync).WithName("ListMyBookings");

    /// <summary>The caller's bookings that end in the future, or from the last 30 days when <paramref name="includePast"/> is set.</summary>
    private static async Task<Ok<List<BookingResponse>>> HandleAsync(
        ClaimsPrincipal user,
        BookingsDbContext db,
        IToolCatalog tools,
        IClock clock,
        CancellationToken ct,
        bool includePast = false)
    {
        var userId = user.GetUserId();
        var from = includePast ? clock.UtcNow - PastWindow : clock.UtcNow;

        var bookings = await db.Bookings.AsNoTracking()
            .Where(b => b.UserId == userId && b.End >= from)
            .OrderBy(b => b.Start)
            .ToListAsync(ct);

        // Cross-module read through the contract, not a SQL join (ADR-0002).
        var toolNames = (await tools.ListAsync(ct)).ToDictionary(t => t.Id, t => t.Name);
        return TypedResults.Ok(bookings.ConvertAll(b => BookingResponse.From(b, toolNames.GetValueOrDefault(b.ToolId))));
    }
}
