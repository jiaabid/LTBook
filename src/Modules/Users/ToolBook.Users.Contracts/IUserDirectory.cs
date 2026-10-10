namespace ToolBook.Users.Contracts;

/// <summary>Read-only access to user profile data for other modules, so they never touch Identity tables.</summary>
public interface IUserDirectory
{
    Task<string?> GetDisplayNameAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Display names for the given ids. Unknown ids are omitted from the result.</summary>
    Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken);
}
