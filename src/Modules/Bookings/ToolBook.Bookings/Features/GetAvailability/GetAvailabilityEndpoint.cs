using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using ToolBook.Bookings.Domain;
using ToolBook.Bookings.Domain.Availability;
using ToolBook.Bookings.Persistence;
using ToolBook.Bookings.Scheduling;
using ToolBook.SharedKernel.Http;
using ToolBook.SharedKernel.Time;
using ToolBook.Tools.Contracts;

namespace ToolBook.Bookings.Features.GetAvailability;

internal sealed record AvailabilityResponse(
    Guid ToolId,
    AvailabilityStatus Status,
    bool FreeNow,
    DateTimeOffset EstimatedFreeAt,
    FreeSlot? NextFreeSlot,
    EstimateConfidence Confidence,
    int? TypicalOverrunMinutes,
    int HistorySampleSize,
    string Explanation);

internal static class GetAvailabilityEndpoint
{
    /// <summary>How many recent completed bookings feed the overrun statistic.</summary>
    private const int HistoryWindow = 20;

    public static void MapGetAvailability(this IEndpointRouteBuilder app) =>
        app.MapGet("/{toolId:guid}/availability", HandleAsync).WithName("GetAvailability");

    private static async Task<IResult> HandleAsync(
        Guid toolId,
        BookingsDbContext db,
        IToolCatalog tools,
        LabTimeZone lab,
        IClock clock,
        CancellationToken ct)
    {
        var tool = await tools.GetAsync(toolId, ct);
        if (tool is null)
        {
            return BookingErrors.ToolNotFound(toolId).ToProblem();
        }

        var now = clock.UtcNow;
        var (_, endOfToday) = lab.DayBounds(lab.LocalDate(now));

        var active = await db.Bookings.AsNoTracking()
            .Where(b => b.ToolId == toolId
                        && (b.Status == BookingStatus.InUse || (b.Status == BookingStatus.Scheduled && b.End > now))
                        && b.Start < endOfToday)
            .Select(b => new { b.Start, b.End, b.Status })
            .ToListAsync(ct);

        var history = await db.Bookings.AsNoTracking()
            .Where(b => b.ToolId == toolId && b.Status == BookingStatus.Completed && b.CheckedOutAt != null)
            .OrderByDescending(b => b.End)
            .Take(HistoryWindow)
            .Select(b => new { b.End, CheckedOutAt = b.CheckedOutAt!.Value })
            .ToListAsync(ct);

        var estimate = AvailabilityEstimator.Estimate(new AvailabilityInput(
            now,
            TimeSpan.FromMinutes(tool.DefaultSlotMinutes),
            active.ConvertAll(b => new ScheduledSlot(b.Start, b.End, b.Status == BookingStatus.InUse)),
            history.ConvertAll(h => h.CheckedOutAt - h.End),
            endOfToday,
            lab.Zone));

        return TypedResults.Ok(new AvailabilityResponse(
            toolId,
            estimate.Status,
            estimate.FreeNow,
            estimate.EstimatedFreeAt,
            estimate.NextFreeSlot,
            estimate.Confidence,
            estimate.TypicalOverrun is { } o ? (int)Math.Round(o.TotalMinutes) : null,
            history.Count,
            estimate.Explanation));
    }
}
