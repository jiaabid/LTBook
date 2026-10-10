using ToolBook.Knowledge.Domain;

namespace ToolBook.Knowledge.Features;

/// <summary>Document metadata (never the file content), shared by the document slices.</summary>
internal sealed record DocumentResponse(
    Guid Id,
    Guid ToolId,
    string FileName,
    DocumentStatus Status,
    string? Error,
    int ChunkCount,
    DateTimeOffset UploadedAt,
    DateTimeOffset? ProcessedAt);
