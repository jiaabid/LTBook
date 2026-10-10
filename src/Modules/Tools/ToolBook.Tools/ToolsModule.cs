using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ToolBook.SharedKernel.Persistence;
using ToolBook.Tools.Contracts;
using ToolBook.Tools.Features.CreateTool;
using ToolBook.Tools.Features.DeactivateTool;
using ToolBook.Tools.Features.GetTool;
using ToolBook.Tools.Features.ListTools;
using ToolBook.Tools.Features.UpdateTool;
using ToolBook.Tools.Persistence;

namespace ToolBook.Tools;

/// <summary>The only public entry point of the Tools module: service registration and endpoint mapping.</summary>
public static class ToolsModule
{
    public static IServiceCollection AddToolsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<ToolsDbContext>(ToolsDbContext.Schema);
        services.AddScoped<IToolCatalog, ToolCatalog>();
        services.AddScoped<CreateToolHandler>();
        services.AddScoped<UpdateToolHandler>();
        services.AddScoped<DeactivateToolHandler>();
        services.AddValidatorsFromAssemblyContaining<ToolsDbContext>(includeInternalTypes: true);
        return services;
    }

    public static IEndpointRouteBuilder MapToolsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var tools = endpoints.MapGroup("/tools").WithTags("Tools").RequireAuthorization();
        tools.MapListTools();
        tools.MapGetTool();
        tools.MapCreateTool();
        tools.MapUpdateTool();
        tools.MapDeactivateTool();
        return endpoints;
    }
}
