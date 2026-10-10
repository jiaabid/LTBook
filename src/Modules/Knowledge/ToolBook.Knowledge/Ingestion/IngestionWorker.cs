using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ToolBook.Knowledge.Domain;
using ToolBook.Knowledge.Persistence;

namespace ToolBook.Knowledge.Ingestion;

/// <summary>
/// Processes queued documents one at a time, each in its own DI scope. On start it re-queues documents left
/// Pending/Processing by a previous run, so an in-memory queue lost in a restart does not lose work.
/// </summary>
internal sealed partial class IngestionWorker(
    IngestionQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<IngestionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RequeueUnfinishedAsync(stoppingToken);

        await foreach (var documentId in queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<DocumentIngestor>().IngestAsync(documentId, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Never let one document stop the worker.
                LogUnexpectedFailure(ex, documentId);
            }
        }
    }

    private async Task RequeueUnfinishedAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<KnowledgeDbContext>();
            var unfinished = await db.Documents
                .Where(d => d.Status == DocumentStatus.Pending || d.Status == DocumentStatus.Processing)
                .Select(d => d.Id)
                .ToListAsync(ct);

            foreach (var id in unfinished)
            {
                await queue.EnqueueAsync(id, ct);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogRequeueFailed(ex);
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Unexpected failure while ingesting document {DocumentId}")]
    private partial void LogUnexpectedFailure(Exception exception, Guid documentId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not re-queue unfinished documents at startup")]
    private partial void LogRequeueFailed(Exception exception);
}
