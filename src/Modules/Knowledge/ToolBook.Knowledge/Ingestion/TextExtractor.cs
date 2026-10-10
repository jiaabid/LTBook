using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace ToolBook.Knowledge.Ingestion;

/// <summary>Text of one page. <see cref="PageNumber"/> is null for formats without pages (Markdown, plain text).</summary>
internal sealed record PageText(int? PageNumber, string Text);

internal static class TextExtractor
{
    public static readonly IReadOnlyDictionary<string, string> SupportedTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = "application/pdf",
        [".md"] = "text/markdown",
        [".txt"] = "text/plain",
    };

    public static bool IsSupported(string fileName) => SupportedTypes.ContainsKey(Path.GetExtension(fileName));

    public static IReadOnlyList<PageText> Extract(string fileName, byte[] content)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => ExtractPdf(content),
            ".md" or ".txt" => [new PageText(null, DecodeText(content))],
            _ => throw new NotSupportedException($"Unsupported file type '{extension}'."),
        };
    }

    private static List<PageText> ExtractPdf(byte[] content)
    {
        using var pdf = PdfDocument.Open(content);
        return [.. pdf.GetPages().Select(p => new PageText(p.Number, ContentOrderTextExtractor.GetText(p)))];
    }

    private static string DecodeText(byte[] content)
    {
        // Detects a UTF-8/UTF-16 BOM, falls back to UTF-8.
        using var reader = new StreamReader(new MemoryStream(content), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }
}
