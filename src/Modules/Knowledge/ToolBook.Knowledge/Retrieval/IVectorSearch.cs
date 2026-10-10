namespace ToolBook.Knowledge.Retrieval;

internal sealed record ChunkMatch(
    Guid ChunkId,
    Guid DocumentId,
    string FileName,
    string? Section,
    int? Page,
    string Text,
    double Score);

/// <summary>Similarity search over manual chunks, always scoped to one tool. See ADR-0008 for implementation choices.</summary>
internal interface IVectorSearch
{
    /// <returns>Up to <paramref name="topK"/> matches, best first.</returns>
    Task<IReadOnlyList<ChunkMatch>> SearchAsync(Guid toolId, ReadOnlyMemory<float> query, int topK, CancellationToken cancellationToken);
}
