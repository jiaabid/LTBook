using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using ToolBook.SharedKernel.Http;
using ToolBook.Tools.Domain;
using ToolBook.Tools.Persistence;

namespace ToolBook.Tools.Features.GetTool;

internal static class GetToolEndpoint
{
    public static void MapGetTool(this IEndpointRouteBuilder app) =>
        app.MapGet("/{id:guid}", HandleAsync).WithName("GetTool");

    private static async Task<IResult> HandleAsync(Guid id, ToolsDbContext db, CancellationToken ct)
    {
        var tool = await db.Tools.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct);
        return tool is null
            ? ToolErrors.NotFound(id).ToProblem()
            : TypedResults.Ok(ToolResponse.From(tool));
    }
}
