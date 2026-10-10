using System.Linq.Expressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using ToolBook.Knowledge.Domain;
using ToolBook.Knowledge.Persistence;

namespace ToolBook.Knowledge.Features.ListDocuments;

internal static class ListDocumentsEndpoint
{
    public static void MapListDocuments(this IEndpointRouteBuilder app)
    {
        app.MapGet("/{toolId:guid}/documents", async (Guid toolId, KnowledgeDbContext db, CancellationToken ct) =>
                TypedResults.Ok(await Query(db, d => d.ToolId == toolId).ToListAsync(ct)))
            .WithName("ListDocuments");

        app.MapGet("/{toolId:guid}/documents/{id:guid}", async (Guid toolId, Guid id, KnowledgeDbContext db, CancellationToken ct) =>
                await Query(db, d => d.ToolId == toolId && d.Id == id).FirstOrDefaultAsync(ct) is { } document
                    ? Results.Ok(document)
                    : Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Knowledge.DocumentNotFound"))
            .WithName("GetDocument");
    }

    // Filter and sort on the entity, then project: never load the file content just to list metadata.
    // (EF Core cannot translate Where/OrderBy on a record built through its constructor, so they must come first.)
    private static IQueryable<DocumentResponse> Query(KnowledgeDbContext db, Expression<Func<ToolDocument, bool>> filter) =>
        db.Documents.AsNoTracking().Where(filter).OrderByDescending(d => d.UploadedAt).Select(d =>
            new DocumentResponse(d.Id, d.ToolId, d.FileName, d.Status, d.Error, d.ChunkCount, d.UploadedAt, d.ProcessedAt));
}
