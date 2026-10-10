using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using ToolBook.Knowledge.Domain;
using ToolBook.Knowledge.Persistence;
using ToolBook.SharedKernel.Time;

namespace ToolBook.Knowledge.Ingestion;

/// <summary>Extract → chunk → embed → store, for one document.</summary>
internal sealed partial class DocumentIngestor(
    KnowledgeDbContext db,
    Chunker chunker,
    IClock clock,
    ILogger<DocumentIngestor> logger,
    IEmbeddingGenerator<string, Embedding<float>>? embeddings = null)
{
    private const int EmbeddingBatchSize = 32;

    public async Task IngestAsync(Guid documentId, CancellationToken ct)
    {
        var document = await db.Documents.FirstOrDefaultAsync(d => d.Id == documentId, ct);
        if (document is null || document.Status == DocumentStatus.Ready)
        {
            return;
        }

        if (embeddings is null)
        {
            // Stays Pending and is picked up at the next start once an API key is configured.
            LogAssistantDisabled(documentId);
            return;
        }

        document.StartProcessing();
        await db.SaveChangesAsync(ct);

        try
        {
            var pages = TextExtractor.Extract(document.FileName, document.Content);
            var chunks = chunker.Chunk(pages);
            if (chunks.Count == 0)
            {
                throw new InvalidOperationException("No text could be extracted from the document.");
            }

            // Re-ingestion replaces earlier chunks.
            await db.Chunks.Where(c => c.DocumentId == document.Id).ExecuteDeleteAsync(ct);

            foreach (var batch in chunks.Chunk(EmbeddingBatchSize))
            {
                var vectors = await embeddings.GenerateAsync(batch.Select(c => c.Text), cancellationToken: ct);
                for (var i = 0; i < batch.Length; i++)
                {
                    var c = batch[i];
                    db.Chunks.Add(DocumentChunk.Create(document.Id, document.ToolId, c.Index, c.Text, c.Section, c.Page,
                        vectors[i].Vector.ToArray()));
                }
            }

            document.MarkReady(chunks.Count, clock.UtcNow);
            await db.SaveChangesAsync(ct);
            LogIngested(documentId, chunks.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogIngestionFailed(ex, documentId);
            db.ChangeTracker.Clear();
            var failed = await db.Documents.FirstAsync(d => d.Id == documentId, ct);
            failed.MarkFailed(ex.Message, clock.UtcNow);
            await db.SaveChangesAsync(ct);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Assistant is not configured; document {DocumentId} stays pending")]
    private partial void LogAssistantDisabled(Guid documentId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Ingested document {DocumentId} into {ChunkCount} chunks")]
    private partial void LogIngested(Guid documentId, int chunkCount);

    [LoggerMessage(Level = LogLevel.Error, Message = "Ingestion of document {DocumentId} failed")]
    private partial void LogIngestionFailed(Exception exception, Guid documentId);
}
