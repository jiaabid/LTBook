using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ToolBook.Knowledge.Assistant;
using ToolBook.Knowledge.Retrieval;
using ToolBook.SharedKernel.Http;
using ToolBook.SharedKernel.Results;
using ToolBook.Tools.Contracts;

namespace ToolBook.Knowledge.Features.AskTool;

internal sealed record AskRequest(string Question);

internal sealed record Citation(int Number, Guid DocumentId, string FileName, string? Section, int? Page, double Score, string Excerpt);

internal sealed record AskResponse(string Answer, bool Grounded, IReadOnlyList<Citation> Citations, int UsedChunks);

internal sealed class AskRequestValidator : AbstractValidator<AskRequest>
{
    public AskRequestValidator() => RuleFor(r => r.Question).NotEmpty().MaximumLength(500);
}

/// <summary>
/// Retrieval-augmented answer about one tool: embed the question, retrieve that tool's chunks only, refuse if nothing
/// is similar enough, otherwise ask the model to answer strictly from the retrieved excerpts with [n] citations.
/// </summary>
internal sealed partial class AskToolHandler(
    IToolCatalog tools,
    IVectorSearch search,
    IOptions<AssistantOptions> options,
    ILogger<AskToolHandler> logger,
    IEmbeddingGenerator<string, Embedding<float>>? embeddings = null,
    IChatClient? chat = null)
{
    private const int ExcerptLength = 300;

    public async Task<Result<AskResponse>> HandleAsync(Guid toolId, string question, CancellationToken ct)
    {
        if (embeddings is null || chat is null)
        {
            return Error.Unavailable("Assistant.Disabled", "The tool assistant is not configured on this server (no AI provider: set Assistant:Provider to Ollama, or add a Gemini API key).");
        }

        var tool = await tools.GetAsync(toolId, ct);
        if (tool is null)
        {
            return Error.NotFound("Assistant.ToolNotFound", $"Tool {toolId} was not found.");
        }

        try
        {
            return await AnswerAsync(tool, question, embeddings, chat, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or System.ClientModel.ClientResultException
                                       || (ex is TaskCanceledException && !ct.IsCancellationRequested))
        {
            // Provider down (e.g. Ollama not running), timed out, or rejected the call (quota): an expected failure.
            LogProviderFailure(ex, toolId);
            return Error.Unavailable("Assistant.ProviderUnavailable",
                "The AI provider did not respond. Check that it is running (for Ollama: `ollama serve`) and try again.");
        }
    }

    private async Task<Result<AskResponse>> AnswerAsync(
        ToolSummary tool, string question, IEmbeddingGenerator<string, Embedding<float>> embeddings, IChatClient chat, CancellationToken ct)
    {
        var toolId = tool.Id;
        var settings = options.Value;
        var queryVector = await embeddings.GenerateVectorAsync(question, cancellationToken: ct);
        var matches = await search.SearchAsync(toolId, queryVector, settings.TopK, ct);
        var relevant = matches.Where(m => m.Score >= settings.MinSimilarity).ToList();

        LogRetrieval(toolId, matches.Count, matches.Count > 0 ? matches[0].Score : 0, relevant.Count);
        if (relevant.Count == 0)
        {
            return NotFound();
        }

        var response = await chat.GetResponseAsync(
            GroundedPrompt.Build(tool.Name, question, relevant),
            new ChatOptions { Temperature = 0.1f },
            ct);

        var answer = response.Text.Trim();
        if (GroundedPrompt.IsNotFound(answer))
        {
            return NotFound(relevant.Count);
        }

        if (GroundedPrompt.IsDegenerate(answer))
        {
            // The model produced no actual text (e.g. only "[3]"). Fall back to quoting the best passage verbatim:
            // still grounded and cited, never empty.
            LogDegenerateAnswer(toolId, answer);
            answer = $"Here is the most relevant passage from the manual:\n\n{relevant[0].Text} [1]";
        }

        // Cite what the model referenced. If it cited nothing, show everything it was given rather than nothing.
        var cited = GroundedPrompt.CitedNumbers(answer, relevant.Count);
        var numbers = cited.Count > 0 ? cited : Enumerable.Range(1, relevant.Count).ToList();
        var citations = numbers
            .Select(n => (Number: n, Match: relevant[n - 1]))
            .Select(x => new Citation(x.Number, x.Match.DocumentId, x.Match.FileName, x.Match.Section, x.Match.Page,
                Math.Round(x.Match.Score, 3), Excerpt(x.Match.Text)))
            .ToList();

        return new AskResponse(answer, Grounded: true, citations, relevant.Count);
    }

    private static AskResponse NotFound(int usedChunks = 0) => new(GroundedPrompt.NotFoundAnswer, Grounded: false, [], usedChunks);

    private static string Excerpt(string text) => text.Length <= ExcerptLength ? text : text[..ExcerptLength].TrimEnd() + "…";

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Assistant retrieval for tool {ToolId}: {Matches} matches, best score {BestScore:0.000}, {Relevant} above threshold")]
    private partial void LogRetrieval(Guid toolId, int matches, double bestScore, int relevant);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Model returned no usable text for tool {ToolId} ({Answer}); quoting the best passage instead")]
    private partial void LogDegenerateAnswer(Guid toolId, string answer);

    [LoggerMessage(Level = LogLevel.Warning, Message = "AI provider call failed for tool {ToolId}")]
    private partial void LogProviderFailure(Exception exception, Guid toolId);
}

internal static class AskToolEndpoint
{
    public static void MapAskTool(this IEndpointRouteBuilder app) =>
        app.MapPost("/{toolId:guid}/ask", async (Guid toolId, AskRequest request, AskToolHandler handler, CancellationToken ct) =>
                (await handler.HandleAsync(toolId, request.Question.Trim(), ct)).ToHttp(TypedResults.Ok))
            .WithValidation<AskRequest>()
            .RequireRateLimiting(Policies.AssistantRateLimit)
            .WithName("AskTool");
}
