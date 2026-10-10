using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ToolBook.Bookings.Domain;
using ToolBook.Bookings.Persistence;
using ToolBook.SharedKernel.Events;
using ToolBook.SharedKernel.Time;
using ToolBook.Tools.Contracts;

namespace ToolBook.Bookings.Features.CancelBookingsOfDeactivatedTool;

/// <summary>Cancels bookings of a deactivated tool that have not started yet. Sessions in progress are left alone.</summary>
internal sealed partial class ToolDeactivatedHandler(
    BookingsDbContext db,
    IClock clock,
    ILogger<ToolDeactivatedHandler> logger) : IIntegrationEventHandler<ToolDeactivated>
{
    public async Task HandleAsync(ToolDeactivated integrationEvent, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var future = await db.Bookings
            .Where(b => b.ToolId == integrationEvent.ToolId && b.Status == BookingStatus.Scheduled && b.Start > now)
            .ToListAsync(cancellationToken);

        foreach (var booking in future)
        {
            booking.Cancel(now, $"{integrationEvent.ToolName} was taken out of service");
        }

        await db.SaveChangesAsync(cancellationToken);
        LogCancelled(future.Count, integrationEvent.ToolId);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Cancelled {Count} future booking(s) of deactivated tool {ToolId}")]
    private partial void LogCancelled(int count, Guid toolId);
}
