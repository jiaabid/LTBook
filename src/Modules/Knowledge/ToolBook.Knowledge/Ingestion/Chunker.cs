using System.Text;
using System.Text.RegularExpressions;

namespace ToolBook.Knowledge.Ingestion;

internal sealed record TextChunk(int Index, string Text, string? Section, int? Page);

internal sealed record ChunkerOptions
{
    /// <summary>Target maximum size of a chunk (~500–800 tokens is a good range for manuals).</summary>
    public int MaxTokens { get; init; } = 700;

    /// <summary>Text carried over from the end of the previous chunk, so a sentence split across chunks is still retrievable.</summary>
    public int OverlapTokens { get; init; } = 100;
}

/// <summary>
/// Splits extracted pages into overlapping chunks along paragraph boundaries and tracks the Markdown heading path
/// and page number of each chunk for citations. Pure and deterministic.
/// Token counts are approximated as characters / 4, which is close enough for English prose and avoids a tokenizer dependency.
/// </summary>
internal sealed partial class Chunker(ChunkerOptions options)
{
    private const int CharsPerToken = 4;

    public Chunker() : this(new ChunkerOptions()) { }

    private int MaxChars => options.MaxTokens * CharsPerToken;

    private int OverlapChars => options.OverlapTokens * CharsPerToken;

    public static int EstimateTokens(string text) => (text.Length + CharsPerToken - 1) / CharsPerToken;

    public IReadOnlyList<TextChunk> Chunk(IReadOnlyList<PageText> pages)
    {
        var chunks = new List<TextChunk>();
        var headings = new List<(int Level, string Title)>();
        var buffer = new List<string>();
        var fresh = 0; // paragraphs in the buffer that are not overlap from the previous chunk
        int? bufferPage = null;
        string? bufferSection = null;

        void Flush(bool keepOverlap)
        {
            if (fresh == 0)
            {
                buffer.Clear(); // only overlap left: it is already in the previous chunk
                return;
            }

            var body = string.Join("\n\n", buffer);
            var text = bufferSection is null ? body : $"{bufferSection}\n\n{body}";
            chunks.Add(new TextChunk(chunks.Count, text, bufferSection, bufferPage));

            var overlap = keepOverlap ? TakeOverlap(buffer) : [];
            buffer.Clear();
            buffer.AddRange(overlap);
            fresh = 0;
            if (buffer.Count == 0)
            {
                bufferPage = null;
            }
        }

        foreach (var page in pages)
        {
            foreach (var block in SplitBlocks(page.Text))
            {
                if (HeadingRegex().Match(block) is { Success: true } heading)
                {
                    // A new section always starts a new chunk, so every chunk has exactly one section for its citation.
                    Flush(keepOverlap: false);
                    var level = heading.Groups[1].Value.Length;
                    headings.RemoveAll(h => h.Level >= level);
                    headings.Add((level, heading.Groups[2].Value.Trim()));
                    bufferSection = string.Join(" > ", headings.Select(h => h.Title));
                    continue;
                }

                foreach (var piece in SplitOversized(block))
                {
                    var size = buffer.Sum(b => b.Length + 2) + piece.Length;
                    if (size > MaxChars && buffer.Count > 0)
                    {
                        Flush(keepOverlap: true);
                        if (buffer.Sum(b => b.Length + 2) + piece.Length > MaxChars)
                        {
                            buffer.Clear(); // the overlap does not fit next to this piece
                        }
                    }

                    if (buffer.Count == 0)
                    {
                        bufferPage = page.PageNumber;
                    }

                    buffer.Add(piece);
                    fresh++;
                }
            }
        }

        Flush(keepOverlap: false);
        return chunks;
    }

    /// <summary>The trailing paragraphs of the flushed chunk that fit in the overlap budget.</summary>
    private List<string> TakeOverlap(List<string> paragraphs)
    {
        var overlap = new List<string>();
        var size = 0;
        for (var i = paragraphs.Count - 1; i >= 0; i--)
        {
            size += paragraphs[i].Length;
            if (size > OverlapChars)
            {
                break;
            }

            overlap.Insert(0, paragraphs[i]);
        }

        return overlap;
    }

    private static IEnumerable<string> SplitBlocks(string text) =>
        BlankLineRegex().Split(text.Replace("\r\n", "\n", StringComparison.Ordinal))
            .SelectMany(SplitHeadingLines)
            .Select(b => b.Trim())
            .Where(b => b.Length > 0);

    /// <summary>A heading directly followed by text (no blank line) is still its own block.</summary>
    private static IEnumerable<string> SplitHeadingLines(string block)
    {
        var current = new StringBuilder();
        foreach (var line in block.Split('\n'))
        {
            if (HeadingRegex().IsMatch(line.Trim()))
            {
                if (current.Length > 0)
                {
                    yield return current.ToString();
                    current.Clear();
                }

                yield return line.Trim();
            }
            else
            {
                current.AppendLine(line);
            }
        }

        if (current.Length > 0)
        {
            yield return current.ToString();
        }
    }

    /// <summary>Splits a paragraph that alone exceeds the chunk size: by sentence, then hard-wraps very long sentences.</summary>
    private IEnumerable<string> SplitOversized(string block)
    {
        if (block.Length <= MaxChars)
        {
            yield return block;
            yield break;
        }

        var current = new StringBuilder();
        foreach (var sentence in SentenceRegex().Split(block))
        {
            if (current.Length + sentence.Length + 1 > MaxChars && current.Length > 0)
            {
                yield return current.ToString().Trim();
                current.Clear();
            }

            for (var start = 0; start < sentence.Length; start += MaxChars)
            {
                var part = sentence.Substring(start, Math.Min(MaxChars, sentence.Length - start));
                if (part.Length == MaxChars)
                {
                    yield return part;
                }
                else
                {
                    current.Append(part).Append(' ');
                }
            }
        }

        if (current.Length > 0)
        {
            yield return current.ToString().Trim();
        }
    }

    [GeneratedRegex(@"^(#{1,6})\s+(.+)$")]
    private static partial Regex HeadingRegex();

    [GeneratedRegex(@"\n\s*\n")]
    private static partial Regex BlankLineRegex();

    [GeneratedRegex(@"(?<=[.!?])\s+")]
    private static partial Regex SentenceRegex();
}
