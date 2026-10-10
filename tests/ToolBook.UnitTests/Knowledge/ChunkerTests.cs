using ToolBook.Knowledge.Ingestion;

namespace ToolBook.UnitTests.Knowledge;

public class ChunkerTests
{
    private static readonly Chunker Small = new(new ChunkerOptions { MaxTokens = 50, OverlapTokens = 15 }); // 200 / 60 chars

    private static string Sentence(int i) => $"Sentence number {i} explains one step of the procedure in detail.";

    private static string Paragraph(int i) => $"{Sentence(i)} {Sentence(i + 100)}";

    [Fact]
    public void Empty_document_produces_no_chunks()
    {
        Assert.Empty(new Chunker().Chunk([new PageText(null, "  \n\n  ")]));
    }

    [Fact]
    public void Short_document_is_a_single_chunk()
    {
        var chunks = new Chunker().Chunk([new PageText(null, "Switch on the lamp.\n\nWait 20 minutes.")]);

        var chunk = Assert.Single(chunks);
        Assert.Equal("Switch on the lamp.\n\nWait 20 minutes.", chunk.Text);
        Assert.Null(chunk.Section);
        Assert.Equal(0, chunk.Index);
    }

    [Fact]
    public void Chunks_respect_the_size_limit()
    {
        var text = string.Join("\n\n", Enumerable.Range(1, 20).Select(Paragraph));

        var chunks = Small.Chunk([new PageText(null, text)]);

        Assert.True(chunks.Count > 1);
        Assert.All(chunks, c => Assert.True(Chunker.EstimateTokens(c.Text) <= 50 + 5, $"Chunk too large: {c.Text.Length} chars"));
        Assert.Equal(Enumerable.Range(0, chunks.Count), chunks.Select(c => c.Index));
    }

    [Fact]
    public void Consecutive_chunks_overlap()
    {
        var paragraphs = Enumerable.Range(1, 12).Select(i => $"Step {i}: do the thing.").ToList();

        var chunks = Small.Chunk([new PageText(null, string.Join("\n\n", paragraphs))]);

        Assert.True(chunks.Count > 1);
        for (var i = 1; i < chunks.Count; i++)
        {
            // The next chunk starts with the trailing paragraph(s) of the previous one.
            var lastOfPrevious = chunks[i - 1].Text.Split("\n\n")[^1];
            var firstOfNext = chunks[i].Text.Split("\n\n")[0];
            Assert.Contains(lastOfPrevious, chunks[i].Text, StringComparison.Ordinal);
            Assert.Contains(firstOfNext, chunks[i - 1].Text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Every_paragraph_is_kept()
    {
        var paragraphs = Enumerable.Range(1, 15).Select(Paragraph).ToList();

        var chunks = Small.Chunk([new PageText(null, string.Join("\n\n", paragraphs))]);

        Assert.All(paragraphs, p => Assert.Contains(chunks, c => c.Text.Contains(p, StringComparison.Ordinal)));
    }

    [Fact]
    public void Markdown_headings_become_the_section_path_and_start_new_chunks()
    {
        const string markdown = """
            # SEM Guide
            Intro text.

            ## Venting
            Click Vent and wait.

            ### Safety
            Beam must be off.

            ## Imaging
            Start at 5 kV.
            """;

        var chunks = new Chunker().Chunk([new PageText(null, markdown)]);

        Assert.Collection(chunks,
            c => Assert.Equal("SEM Guide", c.Section),
            c => Assert.Equal("SEM Guide > Venting", c.Section),
            c => Assert.Equal("SEM Guide > Venting > Safety", c.Section),
            c =>
            {
                Assert.Equal("SEM Guide > Imaging", c.Section);
                Assert.Equal("SEM Guide > Imaging\n\nStart at 5 kV.", c.Text); // heading path is part of the embedded text
            });
    }

    [Fact]
    public void Pdf_page_numbers_are_kept_per_chunk()
    {
        PageText[] pages =
        [
            new(1, string.Join("\n\n", Enumerable.Range(1, 3).Select(Paragraph))),
            new(2, string.Join("\n\n", Enumerable.Range(10, 3).Select(Paragraph))),
        ];

        var chunks = Small.Chunk(pages);

        Assert.Equal(1, chunks[0].Page);
        Assert.Equal(2, chunks[^1].Page);
        Assert.Equal(2, chunks.Single(c => c.Text.StartsWith(Sentence(10), StringComparison.Ordinal)).Page);
    }

    [Fact]
    public void Oversized_paragraph_is_split_by_sentence()
    {
        var huge = string.Join(" ", Enumerable.Range(1, 30).Select(Sentence));

        var chunks = Small.Chunk([new PageText(null, huge)]);

        Assert.True(chunks.Count > 3);
        Assert.All(chunks, c => Assert.True(c.Text.Length <= 200 + 70));
        Assert.Contains(chunks, c => c.Text.Contains(Sentence(30), StringComparison.Ordinal));
    }
}
