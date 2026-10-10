using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using ToolBook.SharedKernel.Events;
using ToolBook.SharedKernel.Http;
using ToolBook.SharedKernel.Results;
using ToolBook.SharedKernel.Time;
using ToolBook.Tools.Contracts;
using ToolBook.Tools.Domain;
using ToolBook.Tools.Persistence;

namespace ToolBook.Tools.Features.DeactivateTool;

internal sealed class DeactivateToolHandler(ToolsDbContext db, IEventBus eventBus, IClock clock)
{
    public async Task<Result> HandleAsync(Guid id, CancellationToken ct)
    {
        var tool = await db.Tools.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (tool is null)
        {
            return ToolErrors.NotFound(id);
        }

        if (!tool.Deactivate())
        {
            return Result.Success(); // Idempotent: already inactive.
        }

        await db.SaveChangesAsync(ct);

        // Published after commit. Not atomic with the save; see ADR-0003 (Outbox on the roadmap).
        await eventBus.PublishAsync(new ToolDeactivated(Guid.CreateVersion7(), clock.UtcNow, tool.Id, tool.Name), ct);
        return Result.Success();
    }
}

internal static class DeactivateToolEndpoint
{
    public static void MapDeactivateTool(this IEndpointRouteBuilder app) =>
        app.MapPost("/{id:guid}/deactivate", async (Guid id, DeactivateToolHandler handler, CancellationToken ct) =>
                (await handler.HandleAsync(id, ct)).ToHttp(TypedResults.NoContent))
            .RequireAuthorization(Policies.Admin)
            .WithName("DeactivateTool");
}
