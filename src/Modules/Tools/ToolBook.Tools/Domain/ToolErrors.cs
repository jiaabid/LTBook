using ToolBook.SharedKernel.Results;

namespace ToolBook.Tools.Domain;

internal static class ToolErrors
{
    public static Error NotFound(Guid id) => Error.NotFound("Tools.NotFound", $"Tool {id} was not found.");

    public static Error DuplicateName(string name) =>
        Error.Conflict("Tools.DuplicateName", $"A tool named '{name}' already exists.");
}
