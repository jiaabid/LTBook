using Microsoft.Extensions.DependencyInjection;

namespace ToolBook.SharedKernel.Events;

public static class EventBusServiceCollectionExtensions
{
    public static IServiceCollection AddIntegrationEventHandler<TEvent, THandler>(this IServiceCollection services)
        where TEvent : IIntegrationEvent
        where THandler : class, IIntegrationEventHandler<TEvent> =>
        services.AddScoped<IIntegrationEventHandler<TEvent>, THandler>();
}
