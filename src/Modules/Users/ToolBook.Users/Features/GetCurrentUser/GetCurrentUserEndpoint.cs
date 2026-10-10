using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using ToolBook.SharedKernel.Http;
using ToolBook.Users.Persistence;

namespace ToolBook.Users.Features.GetCurrentUser;

internal sealed record CurrentUserResponse(Guid Id, string Email, string DisplayName, IReadOnlyList<string> Roles);

internal static class GetCurrentUserEndpoint
{
    public static void MapGetCurrentUser(this IEndpointRouteBuilder app) =>
        app.MapGet("/me", HandleAsync)
            .RequireAuthorization()
            .WithName("GetCurrentUser");

    private static async Task<Results<Ok<CurrentUserResponse>, NotFound>> HandleAsync(
        ClaimsPrincipal principal, UsersDbContext db, CancellationToken ct)
    {
        var userId = principal.GetUserId();
        var user = await db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.Id, u.Email, u.DisplayName })
            .FirstOrDefaultAsync(ct);
        if (user is null)
        {
            return TypedResults.NotFound();
        }

        var roles = await db.UserRoles
            .Where(ur => ur.UserId == userId)
            .Join(db.Roles, ur => ur.RoleId, r => r.Id, (_, r) => r.Name!)
            .ToListAsync(ct);

        return TypedResults.Ok(new CurrentUserResponse(user.Id, user.Email!, user.DisplayName, roles));
    }
}
