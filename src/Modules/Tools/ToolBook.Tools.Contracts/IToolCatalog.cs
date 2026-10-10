namespace ToolBook.Tools.Contracts;

/// <summary>What other modules may know about a tool.</summary>
public sealed record ToolSummary(
    Guid Id,
    string Name,
    bool IsActive,
    int DefaultSlotMinutes,
    int TypicalBookingMinutes);

/// <summary>Synchronous, read-only access to the tool catalog for other modules.</summary>
public interface IToolCatalog
{
    Task<ToolSummary?> GetAsync(Guid toolId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ToolSummary>> ListAsync(CancellationToken cancellationToken);
}
