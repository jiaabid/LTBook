using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ToolBook.Users.Contracts;
using ToolBook.Users.Domain;

namespace ToolBook.Users.Seeding;

/// <summary>
/// Ensures the roles exist, and outside Production creates the admin account from <c>Seed:Admin</c> configuration
/// (only appsettings.Development.json and the integration tests provide it).
/// Runs after the Users migrator (hosted services start in registration order).
/// </summary>
internal sealed partial class IdentitySeeder(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    IHostEnvironment environment,
    ILogger<IdentitySeeder> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!configuration.GetValue<bool>("Database:MigrateOnStartup"))
        {
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var role in new[] { Roles.User, Roles.Admin })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }

        var email = configuration["Seed:Admin:Email"];
        var password = configuration["Seed:Admin:Password"];
        if (environment.IsProduction() || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return;
        }

        var admin = new User { UserName = email, Email = email, EmailConfirmed = true, DisplayName = "Lab Admin" };
        var result = await userManager.CreateAsync(admin, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Could not seed admin user: {result}");
        }

        await userManager.AddToRolesAsync(admin, [Roles.User, Roles.Admin]);
        LogAdminSeeded(email);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(Level = LogLevel.Information, Message = "Seeded development admin {Email}")]
    private partial void LogAdminSeeded(string email);
}
