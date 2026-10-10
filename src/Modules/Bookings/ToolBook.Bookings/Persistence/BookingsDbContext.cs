using Microsoft.EntityFrameworkCore;
using ToolBook.Bookings.Domain;

namespace ToolBook.Bookings.Persistence;

internal sealed class BookingsDbContext(DbContextOptions<BookingsDbContext> options) : DbContext(options)
{
    public const string Schema = "bookings";

    public DbSet<Booking> Bookings => Set<Booking>();

    public DbSet<ToolSchedule> ToolSchedules => Set<ToolSchedule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        var booking = modelBuilder.Entity<Booking>();
        booking.ToTable("Bookings");
        booking.HasKey(b => b.Id);
        booking.Property(b => b.Purpose).HasMaxLength(Booking.PurposeMaxLength).IsRequired();
        booking.Property(b => b.Status).HasConversion<string>().HasMaxLength(20);
        booking.Property(b => b.CancellationReason).HasMaxLength(200);
        booking.Ignore(b => b.IsActive);

        // ToolId and UserId belong to other modules: plain columns, no cross-schema foreign keys (ADR-0002).
        booking.HasIndex(b => new { b.ToolId, b.Start });
        booking.HasIndex(b => new { b.UserId, b.Start });

        var schedule = modelBuilder.Entity<ToolSchedule>();
        schedule.ToTable("ToolSchedules");
        schedule.HasKey(s => s.ToolId);
        schedule.Property(s => s.ToolId).ValueGeneratedNever();
        schedule.Property(s => s.Version).IsRowVersion();
    }
}
