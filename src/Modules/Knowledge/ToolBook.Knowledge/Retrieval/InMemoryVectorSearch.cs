using System.Numerics.Tensors;
using Microsoft.EntityFrameworkCore;
using ToolBook.Knowledge.Domain;
using ToolBook.Knowledge.Persistence;

namespace ToolBook.Knowledge.Retrieval;

/// <summary>A stored chunk with its embedding, as loaded for ranking.</summary>
internal sealed record EmbeddedChunk(Guid ChunkId, Guid DocumentId, string FileName, string? Section, int? Page, string Text, float[] Embedding);

/// <summary>
/// Loads the tool's chunk embeddings and ranks them by cosine similarity in memory (SIMD via TensorPrimitives).
/// O(chunks of one tool) per query: fine for a few manuals per tool, with no extra infrastructure (ADR-0008).
/// </summary>
internal sealed class InMemoryVectorSearch(KnowledgeDbContext db) : IVectorSearch
{
    public async Task<IReadOnlyList<ChunkMatch>> SearchAsync(
        Guid toolId, ReadOnlyMemory<float> query, int topK, CancellationToken cancellationToken)
    {
        // Scoped to the tool, and only chunks of documents whose ingestion completed.
        var candidates = await db.Chunks.AsNoTracking()
            .Where(c => c.ToolId == toolId)
            .Join(db.Documents.Where(d => d.Status == DocumentStatus.Ready),
                c => c.DocumentId,
                d => d.Id,
                (c, d) => new EmbeddedChunk(c.Id, c.DocumentId, d.FileName, c.Section, c.Page, c.Text, c.Embedding))
            .ToListAsync(cancellationToken);

        return Rank(query, candidates, topK);
    }

    /// <summary>Pure ranking step: cosine similarity, best first. Vectors of a different size (another embedding model) are skipped.</summary>
    public static IReadOnlyList<ChunkMatch> Rank(ReadOnlyMemory<float> query, IEnumerable<EmbeddedChunk> candidates, int topK) =>
        [.. candidates
            .Where(c => c.Embedding.Length == query.Length)
            .Select(c => new ChunkMatch(c.ChunkId, c.DocumentId, c.FileName, c.Section, c.Page, c.Text,
                TensorPrimitives.CosineSimilarity(query.Span, c.Embedding)))
            .OrderByDescending(m => m.Score)
            .Take(topK)];
}
