using Microsoft.EntityFrameworkCore;
using ToolBook.Tools.Contracts;

namespace ToolBook.Tools.Persistence;

internal sealed class ToolCatalog(ToolsDbContext db) : IToolCatalog
{
    public Task<ToolSummary?> GetAsync(Guid toolId, CancellationToken cancellationToken) =>
        db.Tools.AsNoTracking()
            .Where(t => t.Id == toolId)
            .Select(t => new ToolSummary(t.Id, t.Name, t.IsActive, t.DefaultSlotMinutes, t.TypicalBookingMinutes))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<ToolSummary>> ListAsync(CancellationToken cancellationToken) =>
        await db.Tools.AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new ToolSummary(t.Id, t.Name, t.IsActive, t.DefaultSlotMinutes, t.TypicalBookingMinutes))
            .ToListAsync(cancellationToken);
}
