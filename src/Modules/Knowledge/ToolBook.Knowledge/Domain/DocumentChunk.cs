namespace ToolBook.Knowledge.Domain;

/// <summary>A retrievable piece of a manual with its embedding. <see cref="ToolId"/> is denormalised so search can scope by tool.</summary>
internal sealed class DocumentChunk
{
    private DocumentChunk(Guid id, Guid documentId, Guid toolId, int index, string text, string? section, int? page, float[] embedding)
    {
        Id = id;
        DocumentId = documentId;
        ToolId = toolId;
        Index = index;
        Text = text;
        Section = section;
        Page = page;
        Embedding = embedding;
    }

    public Guid Id { get; private set; }

    public Guid DocumentId { get; private set; }

    public Guid ToolId { get; private set; }

    /// <summary>Position in the document, for stable ordering and neighbour lookup.</summary>
    public int Index { get; private set; }

    public string Text { get; private set; }

    /// <summary>Nearest heading path, e.g. "Operation > Venting the chamber". Null for PDFs without structure.</summary>
    public string? Section { get; private set; }

    /// <summary>1-based page number for PDFs, null for Markdown/text.</summary>
    public int? Page { get; private set; }

    public float[] Embedding { get; private set; }

    public static DocumentChunk Create(Guid documentId, Guid toolId, int index, string text, string? section, int? page, float[] embedding) =>
        new(Guid.CreateVersion7(), documentId, toolId, index, text, section, page, embedding);
}
