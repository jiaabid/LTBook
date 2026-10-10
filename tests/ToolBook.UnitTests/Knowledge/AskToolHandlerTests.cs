using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ToolBook.Knowledge.Assistant;
using ToolBook.Knowledge.Features.AskTool;
using ToolBook.Knowledge.Retrieval;
using ToolBook.SharedKernel.Results;
using ToolBook.Tools.Contracts;
using ToolBook.UnitTests.Fakes;

namespace ToolBook.UnitTests.Knowledge;

public class AskToolHandlerTests
{
    private static readonly Guid SemId = Guid.NewGuid();
    private static readonly Guid AfmId = Guid.NewGuid();
    private static readonly Guid DocId = Guid.NewGuid();

    private readonly HashingEmbeddingGenerator _embeddings = new();
    private readonly List<(Guid ToolId, EmbeddedChunk Chunk)> _chunks = [];

    public AskToolHandlerTests()
    {
        AddChunk(SemId, "Venting", "To vent the chamber, switch off the beam, click Vent and wait three minutes until the status shows vented.");
        AddChunk(SemId, "Imaging", "Start imaging at an accelerating voltage of 5 kV and a working distance of 10 mm.");
        AddChunk(SemId, "Sample preparation", "Coat non-conductive samples with gold or carbon to prevent charging.");
        AddChunk(AfmId, "Cantilever", "To vent the chamber of the AFM glovebox, open valve V2.");
    }

    private void AddChunk(Guid toolId, string section, string text) =>
        _chunks.Add((toolId, new EmbeddedChunk(Guid.NewGuid(), DocId, "guide.md", section, null, text, _embeddings.Embed($"{section}\n\n{text}"))));

    private AskToolHandler Handler(IChatClient? chat, double minSimilarity = 0.3) => new(
        new FakeToolCatalog(new ToolSummary(SemId, "Scanning Electron Microscope", true, 60, 120), new ToolSummary(AfmId, "AFM", true, 60, 120)),
        new FakeVectorSearch(_chunks),
        Options.Create(new AssistantOptions { TopK = 3, MinSimilarity = minSimilarity }),
        NullLogger<AskToolHandler>.Instance,
        _embeddings,
        chat);

    [Fact]
    public async Task Retrieval_ranks_the_relevant_chunk_first_and_only_for_the_asked_tool()
    {
        var search = new FakeVectorSearch(_chunks);

        var matches = await search.SearchAsync(SemId, _embeddings.Embed("How do I vent the chamber?"), 3, default);

        Assert.Equal("Venting", matches[0].Section);
        Assert.True(matches[0].Score > matches[1].Score);
        Assert.DoesNotContain(matches, m => m.Text.Contains("AFM", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Answers_from_context_with_citations()
    {
        var chat = new ScriptedChatClient(_ => "Switch off the beam, then click Vent and wait about 3 minutes [1].");

        var result = await Handler(chat).HandleAsync(SemId, "How do I vent the chamber?", default);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Grounded);
        Assert.StartsWith("Switch off the beam", result.Value.Answer);
        var citation = Assert.Single(result.Value.Citations);
        Assert.Equal(1, citation.Number);
        Assert.Equal("Venting", citation.Section);
        Assert.Equal("guide.md", citation.FileName);

        // The prompt is grounded: it contains the retrieved excerpt and the grounding rules.
        var prompt = Assert.Single(chat.Requests);
        Assert.Contains("Answer ONLY from the numbered manual excerpts", prompt[0].Text);
        Assert.Contains("[1] guide.md, section \"Venting\"", prompt[1].Text);
        Assert.Contains("click Vent and wait three minutes", prompt[1].Text);
        Assert.DoesNotContain("valve V2", prompt[1].Text); // another tool's manual never reaches the model
    }

    [Fact]
    public async Task Below_similarity_threshold_says_not_found_without_calling_the_model()
    {
        var chat = new ScriptedChatClient(_ => "should not be called");

        var result = await Handler(chat, minSimilarity: 0.3).HandleAsync(SemId, "What is the warranty period for the lamp?", default);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.Grounded);
        Assert.Equal(GroundedPrompt.NotFoundAnswer, result.Value.Answer);
        Assert.Empty(result.Value.Citations);
        Assert.Empty(chat.Requests);
    }

    [Fact]
    public async Task Model_reporting_not_found_is_returned_as_not_found()
    {
        var chat = new ScriptedChatClient(_ => GroundedPrompt.NotFoundToken);

        var result = await Handler(chat).HandleAsync(SemId, "How do I vent the chamber in an emergency?", default);

        Assert.False(result.Value.Grounded);
        Assert.Equal(GroundedPrompt.NotFoundAnswer, result.Value.Answer);
        Assert.Empty(result.Value.Citations);
    }

    [Fact]
    public async Task Citation_only_reply_falls_back_to_quoting_the_best_passage()
    {
        var chat = new ScriptedChatClient(_ => "[3]");

        var result = await Handler(chat).HandleAsync(SemId, "How do I vent the chamber?", default);

        Assert.True(result.Value.Grounded);
        Assert.Contains("click Vent and wait three minutes", result.Value.Answer);
        Assert.Equal("Venting", Assert.Single(result.Value.Citations).Section);
    }

    [Theory]
    [InlineData("[3]", true)]
    [InlineData(" [1][2]. ", true)]
    [InlineData("", true)]
    [InlineData("Click Vent [1].", false)]
    public void Detects_replies_without_content(string answer, bool degenerate)
    {
        Assert.Equal(degenerate, GroundedPrompt.IsDegenerate(answer));
    }

    [Fact]
    public async Task Answer_without_explicit_citations_cites_all_used_excerpts()
    {
        var chat = new ScriptedChatClient(_ => "Click Vent and wait.");

        var result = await Handler(chat, minSimilarity: 0.0).HandleAsync(SemId, "How do I vent the chamber?", default);

        Assert.Equal(result.Value.UsedChunks, result.Value.Citations.Count);
    }

    [Fact]
    public async Task Disabled_without_ai_clients()
    {
        var handler = new AskToolHandler(
            new FakeToolCatalog(), new FakeVectorSearch([]), Options.Create(new AssistantOptions()), NullLogger<AskToolHandler>.Instance);

        var result = await handler.HandleAsync(SemId, "anything", default);

        Assert.Equal(ErrorType.Unavailable, result.Error?.Type);
    }

    [Theory]
    [InlineData("Do X [1] then Y [3][1].", 3, new[] { 1, 3 })]
    [InlineData("See [7].", 3, new int[0])] // out of range numbers are ignored
    [InlineData("No citations.", 3, new int[0])]
    public void Parses_cited_excerpt_numbers(string answer, int count, int[] expected)
    {
        Assert.Equal(expected, GroundedPrompt.CitedNumbers(answer, count));
    }

    private sealed class FakeToolCatalog(params ToolSummary[] tools) : IToolCatalog
    {
        public Task<ToolSummary?> GetAsync(Guid toolId, CancellationToken cancellationToken) =>
            Task.FromResult(tools.FirstOrDefault(t => t.Id == toolId));

        public Task<IReadOnlyList<ToolSummary>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ToolSummary>>(tools);
    }

    /// <summary>Same ranking as production (<see cref="InMemoryVectorSearch.Rank"/>) over an in-memory list instead of the database.</summary>
    private sealed class FakeVectorSearch(List<(Guid ToolId, EmbeddedChunk Chunk)> chunks) : IVectorSearch
    {
        public Task<IReadOnlyList<ChunkMatch>> SearchAsync(Guid toolId, ReadOnlyMemory<float> query, int topK, CancellationToken cancellationToken) =>
            Task.FromResult(InMemoryVectorSearch.Rank(query, chunks.Where(c => c.ToolId == toolId).Select(c => c.Chunk), topK));
    }
}
