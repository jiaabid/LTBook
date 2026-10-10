using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using ToolBook.SharedKernel.Http;
using ToolBook.SharedKernel.Results;
using ToolBook.Tools.Domain;
using ToolBook.Tools.Persistence;

namespace ToolBook.Tools.Features.UpdateTool;

internal sealed record UpdateToolRequest(
    string Name,
    string Type,
    string Location,
    string Description,
    int DefaultSlotMinutes,
    int TypicalBookingMinutes) : IToolDetails;

internal sealed class UpdateToolRequestValidator : ToolDetailsValidator<UpdateToolRequest>;

internal sealed class UpdateToolHandler(ToolsDbContext db)
{
    public async Task<Result<ToolResponse>> HandleAsync(Guid id, UpdateToolRequest request, CancellationToken ct)
    {
        var tool = await db.Tools.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (tool is null)
        {
            return ToolErrors.NotFound(id);
        }

        var name = request.Name.Trim();
        if (await db.Tools.AnyAsync(t => t.Id != id && t.Name == name, ct))
        {
            return ToolErrors.DuplicateName(name);
        }

        tool.Update(request.Name, request.Type, request.Location, request.Description,
            request.DefaultSlotMinutes, request.TypicalBookingMinutes);
        await db.SaveChangesAsync(ct);
        return ToolResponse.From(tool);
    }
}

internal static class UpdateToolEndpoint
{
    public static void MapUpdateTool(this IEndpointRouteBuilder app) =>
        app.MapPut("/{id:guid}", async (Guid id, UpdateToolRequest request, UpdateToolHandler handler, CancellationToken ct) =>
                (await handler.HandleAsync(id, request, ct)).ToHttp(TypedResults.Ok))
            .WithValidation<UpdateToolRequest>()
            .RequireAuthorization(Policies.Admin)
            .WithName("UpdateTool");
}
