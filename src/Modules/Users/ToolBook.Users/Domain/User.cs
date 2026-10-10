using Microsoft.AspNetCore.Identity;

namespace ToolBook.Users.Domain;

internal sealed class User : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
}
