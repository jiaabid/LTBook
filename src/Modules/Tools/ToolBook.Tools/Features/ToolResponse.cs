using ToolBook.Tools.Domain;

namespace ToolBook.Tools.Features;

/// <summary>API representation of a tool, shared by the tool slices.</summary>
internal sealed record ToolResponse(
    Guid Id,
    string Name,
    string Type,
    string Location,
    string Description,
    bool IsActive,
    int DefaultSlotMinutes,
    int TypicalBookingMinutes)
{
    public static ToolResponse From(Tool t) =>
        new(t.Id, t.Name, t.Type, t.Location, t.Description, t.IsActive, t.DefaultSlotMinutes, t.TypicalBookingMinutes);
}
