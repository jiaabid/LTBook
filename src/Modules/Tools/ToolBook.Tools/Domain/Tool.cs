using ToolBook.SharedKernel.Domain;

namespace ToolBook.Tools.Domain;

internal sealed class Tool : Entity
{
    public const int NameMaxLength = 100;
    public const int TypeMaxLength = 60;
    public const int LocationMaxLength = 100;
    public const int DescriptionMaxLength = 2000;
    public const int MinSlotMinutes = 15;
    public const int MaxSlotMinutes = 8 * 60;

    private Tool(Guid id, string name, string type, string location, string description, int defaultSlotMinutes, int typicalBookingMinutes)
        : base(id)
    {
        Name = name;
        Type = type;
        Location = location;
        Description = description;
        DefaultSlotMinutes = defaultSlotMinutes;
        TypicalBookingMinutes = typicalBookingMinutes;
        IsActive = true;
    }

    public string Name { get; private set; }

    /// <summary>Instrument category, e.g. "Microscopy".</summary>
    public string Type { get; private set; }

    public string Location { get; private set; }

    public string Description { get; private set; }

    public bool IsActive { get; private set; }

    /// <summary>Smallest useful booking. Availability estimation looks for gaps at least this long.</summary>
    public int DefaultSlotMinutes { get; private set; }

    /// <summary>How long a session usually takes. Used as a hint when booking.</summary>
    public int TypicalBookingMinutes { get; private set; }

    public static Tool Create(string name, string type, string location, string description, int defaultSlotMinutes, int typicalBookingMinutes) =>
        new(Guid.CreateVersion7(), name.Trim(), type.Trim(), location.Trim(), description.Trim(), defaultSlotMinutes, typicalBookingMinutes);

    /// <summary>For seed data with stable ids.</summary>
    public static Tool Seed(Guid id, string name, string type, string location, string description, int defaultSlotMinutes, int typicalBookingMinutes) =>
        new(id, name, type, location, description, defaultSlotMinutes, typicalBookingMinutes);

    public void Update(string name, string type, string location, string description, int defaultSlotMinutes, int typicalBookingMinutes)
    {
        Name = name.Trim();
        Type = type.Trim();
        Location = location.Trim();
        Description = description.Trim();
        DefaultSlotMinutes = defaultSlotMinutes;
        TypicalBookingMinutes = typicalBookingMinutes;
    }

    /// <returns><c>false</c> if the tool was already inactive (nothing changed).</returns>
    public bool Deactivate()
    {
        if (!IsActive)
        {
            return false;
        }

        IsActive = false;
        return true;
    }
}
