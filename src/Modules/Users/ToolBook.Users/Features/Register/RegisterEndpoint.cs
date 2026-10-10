using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using ToolBook.SharedKernel.Http;
using ToolBook.Users.Contracts;
using ToolBook.Users.Domain;

namespace ToolBook.Users.Features.Register;

internal sealed record RegisterRequest(string Email, string Password, string DisplayName);

internal sealed record RegisterResponse(Guid Id, string Email, string DisplayName);

internal sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(r => r.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(r => r.Password).NotEmpty();
        RuleFor(r => r.DisplayName).NotEmpty().MaximumLength(100);
    }
}

/// <summary>
/// Registration with a display name. The built-in Identity <c>/register</c> cannot carry extra profile fields,
/// so it is disabled and replaced by this endpoint (ADR-0005).
/// </summary>
internal static class RegisterEndpoint
{
    public static void MapRegister(this IEndpointRouteBuilder app) =>
        app.MapPost("/register", HandleAsync)
            .WithValidation<RegisterRequest>()
            .AllowAnonymous()
            .WithName("Register");

    private static async Task<Results<Ok<RegisterResponse>, ValidationProblem>> HandleAsync(
        RegisterRequest request, UserManager<User> userManager)
    {
        var user = new User
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName.Trim(),
        };

        var created = await userManager.CreateAsync(user, request.Password);
        if (!created.Succeeded)
        {
            return TypedResults.ValidationProblem(ToErrors(created));
        }

        var roleAdded = await userManager.AddToRoleAsync(user, Roles.User);
        if (!roleAdded.Succeeded)
        {
            throw new InvalidOperationException($"Could not assign role {Roles.User}: {roleAdded}");
        }

        return TypedResults.Ok(new RegisterResponse(user.Id, request.Email, user.DisplayName));
    }

    // Same shape as the built-in Identity endpoints: error code -> descriptions.
    private static Dictionary<string, string[]> ToErrors(IdentityResult result) =>
        result.Errors
            .GroupBy(e => e.Code)
            .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());
}
