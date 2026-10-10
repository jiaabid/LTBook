using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace ToolBook.Knowledge.Features.AssistantStatus;

internal sealed record AssistantStatusResponse(bool Enabled);

internal static class AssistantStatusEndpoint
{
    /// <summary>Lets the UI hide the "Ask" box when no AI key is configured.</summary>
    public static void MapAssistantStatus(this IEndpointRouteBuilder app) =>
        app.MapGet("/status", (IServiceProvider services) =>
                TypedResults.Ok(new AssistantStatusResponse(services.GetService<IChatClient>() is not null)))
            .WithName("AssistantStatus");
}
