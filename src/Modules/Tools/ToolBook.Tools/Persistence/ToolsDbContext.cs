using Microsoft.EntityFrameworkCore;
using ToolBook.Tools.Domain;

namespace ToolBook.Tools.Persistence;

internal sealed class ToolsDbContext(DbContextOptions<ToolsDbContext> options) : DbContext(options)
{
    public const string Schema = "tools";

    public DbSet<Tool> Tools => Set<Tool>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        var tool = modelBuilder.Entity<Tool>();
        tool.ToTable("Tools");
        tool.HasKey(t => t.Id);
        tool.Property(t => t.Name).HasMaxLength(Tool.NameMaxLength).IsRequired();
        tool.Property(t => t.Type).HasMaxLength(Tool.TypeMaxLength).IsRequired();
        tool.Property(t => t.Location).HasMaxLength(Tool.LocationMaxLength).IsRequired();
        tool.Property(t => t.Description).HasMaxLength(Tool.DescriptionMaxLength).IsRequired();
        tool.HasIndex(t => t.Name).IsUnique();
        tool.HasData(SeedTools.All);
    }
}
