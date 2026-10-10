using FluentValidation;
using ToolBook.Tools.Domain;

namespace ToolBook.Tools.Features;

/// <summary>Field rules shared by the create and update requests.</summary>
internal interface IToolDetails
{
    string Name { get; }
    string Type { get; }
    string Location { get; }
    string Description { get; }
    int DefaultSlotMinutes { get; }
    int TypicalBookingMinutes { get; }
}

internal abstract class ToolDetailsValidator<T> : AbstractValidator<T>
    where T : IToolDetails
{
    protected ToolDetailsValidator()
    {
        RuleFor(r => r.Name).NotEmpty().MaximumLength(Tool.NameMaxLength);
        RuleFor(r => r.Type).NotEmpty().MaximumLength(Tool.TypeMaxLength);
        RuleFor(r => r.Location).NotEmpty().MaximumLength(Tool.LocationMaxLength);
        RuleFor(r => r.Description).MaximumLength(Tool.DescriptionMaxLength);
        RuleFor(r => r.DefaultSlotMinutes).InclusiveBetween(Tool.MinSlotMinutes, Tool.MaxSlotMinutes);
        RuleFor(r => r.TypicalBookingMinutes).InclusiveBetween(Tool.MinSlotMinutes, Tool.MaxSlotMinutes);
    }
}
