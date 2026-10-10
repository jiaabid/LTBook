using System.ClientModel;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;
using ToolBook.Knowledge.Assistant;
using ToolBook.Knowledge.Features.AskTool;
using ToolBook.Knowledge.Features.AssistantStatus;
using ToolBook.Knowledge.Features.ListDocuments;
using ToolBook.Knowledge.Features.UploadDocument;
using ToolBook.Knowledge.Ingestion;
using ToolBook.Knowledge.Persistence;
using ToolBook.Knowledge.Retrieval;
using ToolBook.Knowledge.Seeding;
using ToolBook.SharedKernel.Persistence;

namespace ToolBook.Knowledge;

/// <summary>The only public entry point of the Knowledge module: service registration and endpoint mapping.</summary>
public static class KnowledgeModule
{
    public static IServiceCollection AddKnowledgeModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<KnowledgeDbContext>(KnowledgeDbContext.Schema);

        // Order matters: the seeder adds Pending documents before the worker re-queues unfinished ones.
        services.AddHostedService<SampleManualSeeder>();
        services.AddSingleton<IngestionQueue>();
        services.AddHostedService<IngestionWorker>();

        services.AddSingleton(new Chunker());
        services.AddScoped<DocumentIngestor>();
        services.AddScoped<IVectorSearch, InMemoryVectorSearch>();
        services.AddScoped<UploadDocumentHandler>();
        services.AddScoped<AskToolHandler>();
        services.AddValidatorsFromAssemblyContaining<KnowledgeDbContext>(includeInternalTypes: true);

        var section = configuration.GetSection(AssistantOptions.SectionName);
        services.Configure<AssistantOptions>(section);
        AddAiClients(services, section.Get<AssistantOptions>() ?? new AssistantOptions(), configuration["GEMINI_API_KEY"]);
        return services;
    }

    public static IEndpointRouteBuilder MapKnowledgeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var tools = endpoints.MapGroup("/tools").WithTags("Knowledge").RequireAuthorization();
        tools.MapUploadDocument();
        tools.MapListDocuments();
        tools.MapAskTool();

        endpoints.MapGroup("/assistant").WithTags("Knowledge").RequireAuthorization().MapAssistantStatus();
        return endpoints;
    }

    /// <summary>
    /// Registers <see cref="IChatClient"/> and <see cref="IEmbeddingGenerator{TInput,TEmbedding}"/> against the provider's
    /// OpenAI-compatible endpoint (Ollama locally, or Gemini). Gemini without an API key registers nothing, and the
    /// assistant is disabled.
    /// </summary>
    private static void AddAiClients(IServiceCollection services, AssistantOptions options, string? fallbackApiKey)
    {
        var apiKey = options.ResolveApiKey(fallbackApiKey);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return;
        }

        var client = new OpenAIClient(new ApiKeyCredential(apiKey), new OpenAIClientOptions
        {
            Endpoint = new Uri(options.ResolvedEndpoint),
            NetworkTimeout = TimeSpan.FromSeconds(options.TimeoutSeconds),
        });

        services.AddChatClient(client.GetChatClient(options.ResolvedChatModel).AsIChatClient())
            .UseLogging();
        services.AddEmbeddingGenerator(client.GetEmbeddingClient(options.ResolvedEmbeddingModel).AsIEmbeddingGenerator(options.EmbeddingDimensions))
            .UseLogging();
    }
}
