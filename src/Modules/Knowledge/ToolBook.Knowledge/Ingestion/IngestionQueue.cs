using System.Threading.Channels;

namespace ToolBook.Knowledge.Ingestion;

/// <summary>In-process queue of document ids waiting for ingestion. The database status is the source of truth (see <see cref="IngestionWorker"/>).</summary>
internal sealed class IngestionQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions { SingleReader = true });

    public ValueTask EnqueueAsync(Guid documentId, CancellationToken cancellationToken = default) =>
        _channel.Writer.WriteAsync(documentId, cancellationToken);

    public IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}
