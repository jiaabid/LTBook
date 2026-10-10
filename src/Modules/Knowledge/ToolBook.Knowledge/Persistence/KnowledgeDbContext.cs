using System.Runtime.InteropServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using ToolBook.Knowledge.Domain;

namespace ToolBook.Knowledge.Persistence;

internal sealed class KnowledgeDbContext(DbContextOptions<KnowledgeDbContext> options) : DbContext(options)
{
    public const string Schema = "knowledge";

    public DbSet<ToolDocument> Documents => Set<ToolDocument>();

    public DbSet<DocumentChunk> Chunks => Set<DocumentChunk>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        var document = modelBuilder.Entity<ToolDocument>();
        document.ToTable("Documents");
        document.HasKey(d => d.Id);
        document.Property(d => d.FileName).HasMaxLength(ToolDocument.FileNameMaxLength).IsRequired();
        document.Property(d => d.ContentType).HasMaxLength(100).IsRequired();
        document.Property(d => d.Status).HasConversion<string>().HasMaxLength(20);
        document.Property(d => d.Error).HasMaxLength(500);
        document.HasIndex(d => d.ToolId);

        var chunk = modelBuilder.Entity<DocumentChunk>();
        chunk.ToTable("Chunks");
        chunk.HasKey(c => c.Id);
        chunk.Property(c => c.Section).HasMaxLength(300);
        chunk.HasIndex(c => c.ToolId);
        chunk.HasOne<ToolDocument>().WithMany().HasForeignKey(c => c.DocumentId).OnDelete(DeleteBehavior.Cascade);

        // Embeddings are stored as raw little-endian float32 bytes in varbinary(max). See ADR-0008 for when to move
        // to SQL Server's native VECTOR type or a search service.
        chunk.Property(c => c.Embedding)
            .HasConversion(
                v => EmbeddingBytes.FromFloats(v),
                b => EmbeddingBytes.ToFloats(b),
                new ValueComparer<float[]>(
                    (a, b) => a!.SequenceEqual(b!),
                    v => v.Length,
                    v => v.ToArray()));
    }
}

internal static class EmbeddingBytes
{
    public static byte[] FromFloats(float[] vector) => MemoryMarshal.AsBytes(vector.AsSpan()).ToArray();

    public static float[] ToFloats(byte[] bytes) => MemoryMarshal.Cast<byte, float>(bytes.AsSpan()).ToArray();
}
