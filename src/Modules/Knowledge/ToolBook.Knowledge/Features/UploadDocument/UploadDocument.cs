using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ToolBook.Knowledge.Domain;
using ToolBook.Knowledge.Ingestion;
using ToolBook.Knowledge.Persistence;
using ToolBook.SharedKernel.Http;
using ToolBook.SharedKernel.Results;
using ToolBook.SharedKernel.Time;
using ToolBook.Tools.Contracts;

namespace ToolBook.Knowledge.Features.UploadDocument;

/// <summary>Stores the file as Pending and queues it. Extraction and embedding happen in the background (202 Accepted).</summary>
internal sealed class UploadDocumentHandler(KnowledgeDbContext db, IToolCatalog tools, IngestionQueue queue, IClock clock)
{
    public async Task<Result<DocumentResponse>> HandleAsync(Guid toolId, IFormFile file, CancellationToken ct)
    {
        if (await tools.GetAsync(toolId, ct) is null)
        {
            return Error.NotFound("Knowledge.ToolNotFound", $"Tool {toolId} was not found.");
        }

        if (!TextExtractor.IsSupported(file.FileName))
        {
            return Error.Validation("Knowledge.UnsupportedFileType", "Only .pdf, .md and .txt manuals are supported.");
        }

        if (file.Length is 0 or > ToolDocument.MaxSizeBytes)
        {
            return Error.Validation("Knowledge.InvalidFileSize", $"The file must be between 1 byte and {ToolDocument.MaxSizeBytes / 1024 / 1024} MB.");
        }

        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, ct);

        var contentType = TextExtractor.SupportedTypes[Path.GetExtension(file.FileName)];
        var document = ToolDocument.Upload(toolId, file.FileName, contentType, buffer.ToArray(), clock.UtcNow);
        db.Documents.Add(document);
        await db.SaveChangesAsync(ct);
        await queue.EnqueueAsync(document.Id, ct);

        return new DocumentResponse(document.Id, toolId, document.FileName, document.Status, null, 0, document.UploadedAt, null);
    }
}

internal static class UploadDocumentEndpoint
{
    public static void MapUploadDocument(this IEndpointRouteBuilder app) =>
        app.MapPost("/{toolId:guid}/documents", async (Guid toolId, IFormFile file, UploadDocumentHandler handler, CancellationToken ct) =>
                (await handler.HandleAsync(toolId, file, ct))
                    .ToHttp(d => TypedResults.Accepted($"/api/tools/{toolId}/documents/{d.Id}", d)))
            .RequireAuthorization(Policies.Admin)
            // Bearer-token API: no ambient credentials (cookies) for a cross-site form post to abuse.
            .DisableAntiforgery()
            .WithName("UploadDocument");
}
