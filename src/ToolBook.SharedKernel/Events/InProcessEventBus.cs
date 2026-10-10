using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ToolBook.SharedKernel.Events;

/// <summary>
/// Dispatches integration events to every registered handler, in-process and in the caller's DI scope.
/// Delivery is at-most-once and not atomic with the publisher's DB transaction; see ADR-0003 (Outbox on the roadmap).
/// </summary>
public sealed partial class InProcessEventBus(IServiceProvider services, ILogger<InProcessEventBus> logger) : IEventBus
{
    public async Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken)
        where TEvent : IIntegrationEvent
    {
        var handlers = services.GetServices<IIntegrationEventHandler<TEvent>>().ToList();
        LogPublishing(typeof(TEvent).Name, integrationEvent.EventId, handlers.Count);

        foreach (var handler in handlers)
        {
            await handler.HandleAsync(integrationEvent, cancellationToken);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Publishing {EventType} {EventId} to {HandlerCount} handler(s)")]
    private partial void LogPublishing(string eventType, Guid eventId, int handlerCount);
}
