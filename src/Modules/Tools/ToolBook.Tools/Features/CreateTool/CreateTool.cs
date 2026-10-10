using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using ToolBook.SharedKernel.Http;
using ToolBook.SharedKernel.Results;
using ToolBook.Tools.Domain;
using ToolBook.Tools.Persistence;

namespace ToolBook.Tools.Features.CreateTool;

internal sealed record CreateToolRequest(
    string Name,
    string Type,
    string Location,
    string Description,
    int DefaultSlotMinutes,
    int TypicalBookingMinutes) : IToolDetails;

internal sealed class CreateToolRequestValidator : ToolDetailsValidator<CreateToolRequest>;

internal sealed class CreateToolHandler(ToolsDbContext db)
{
    public async Task<Result<ToolResponse>> HandleAsync(CreateToolRequest request, CancellationToken ct)
    {
        var name = request.Name.Trim();
        if (await db.Tools.AnyAsync(t => t.Name == name, ct))
        {
            return ToolErrors.DuplicateName(name);
        }

        var tool = Tool.Create(request.Name, request.Type, request.Location, request.Description,
            request.DefaultSlotMinutes, request.TypicalBookingMinutes);
        db.Tools.Add(tool);
        await db.SaveChangesAsync(ct);
        return ToolResponse.From(tool);
    }
}

internal static class CreateToolEndpoint
{
    public static void MapCreateTool(this IEndpointRouteBuilder app) =>
        app.MapPost("/", async (CreateToolRequest request, CreateToolHandler handler, CancellationToken ct) =>
                (await handler.HandleAsync(request, ct)).ToHttp(tool => TypedResults.Created($"/api/tools/{tool.Id}", tool)))
            .WithValidation<CreateToolRequest>()
            .RequireAuthorization(Policies.Admin)
            .WithName("CreateTool");
}
