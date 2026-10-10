using ToolBook.SharedKernel.Domain;

namespace ToolBook.Knowledge.Domain;

internal enum DocumentStatus
{
    Pending,
    Processing,
    Ready,
    Failed,
}

/// <summary>A manual uploaded for one tool. Ingestion (extract → chunk → embed) runs in the background.</summary>
internal sealed class ToolDocument : Entity
{
    public const int FileNameMaxLength = 255;
    public const long MaxSizeBytes = 10 * 1024 * 1024;

    private ToolDocument(Guid id, Guid toolId, string fileName, string contentType, byte[] content, DateTimeOffset uploadedAt)
        : base(id)
    {
        ToolId = toolId;
        FileName = fileName;
        ContentType = contentType;
        Content = content;
        UploadedAt = uploadedAt;
        Status = DocumentStatus.Pending;
    }

    public Guid ToolId { get; private set; }

    public string FileName { get; private set; }

    public string ContentType { get; private set; }

    /// <summary>Original file bytes, kept so a document can be re-ingested (e.g. with a new embedding model).</summary>
    public byte[] Content { get; private set; }

    public DocumentStatus Status { get; private set; }

    public string? Error { get; private set; }

    public int ChunkCount { get; private set; }

    public DateTimeOffset UploadedAt { get; private set; }

    public DateTimeOffset? ProcessedAt { get; private set; }

    public static ToolDocument Upload(Guid toolId, string fileName, string contentType, byte[] content, DateTimeOffset now) =>
        new(Guid.CreateVersion7(), toolId, Path.GetFileName(fileName), contentType, content, now);

    public void StartProcessing()
    {
        Status = DocumentStatus.Processing;
        Error = null;
    }

    public void MarkReady(int chunkCount, DateTimeOffset now)
    {
        Status = DocumentStatus.Ready;
        ChunkCount = chunkCount;
        ProcessedAt = now;
    }

    public void MarkFailed(string error, DateTimeOffset now)
    {
        Status = DocumentStatus.Failed;
        Error = error.Length > 500 ? error[..500] : error;
        ProcessedAt = now;
    }
}
