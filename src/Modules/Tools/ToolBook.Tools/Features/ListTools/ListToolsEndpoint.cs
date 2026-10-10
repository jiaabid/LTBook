using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using ToolBook.Tools.Persistence;

namespace ToolBook.Tools.Features.ListTools;

internal static class ListToolsEndpoint
{
    public static void MapListTools(this IEndpointRouteBuilder app) =>
        app.MapGet("/", HandleAsync).WithName("ListTools");

    private static async Task<Ok<List<ToolResponse>>> HandleAsync(
        ToolsDbContext db, CancellationToken ct, bool includeInactive = false)
    {
        var tools = await db.Tools.AsNoTracking()
            .Where(t => includeInactive || t.IsActive)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);

        return TypedResults.Ok(tools.ConvertAll(ToolResponse.From));
    }
}
