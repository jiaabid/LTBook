using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ToolBook.SharedKernel.Persistence;
using ToolBook.Users.Contracts;
using ToolBook.Users.Domain;
using ToolBook.Users.Features.GetCurrentUser;
using ToolBook.Users.Features.Register;
using ToolBook.Users.Persistence;
using ToolBook.Users.Seeding;

namespace ToolBook.Users;

/// <summary>The only public entry point of the Users module: service registration and endpoint mapping.</summary>
public static class UsersModule
{
    public static IServiceCollection AddUsersModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<UsersDbContext>(UsersDbContext.Schema);
        services.AddHostedService<IdentitySeeder>();

        // Identity stores and token endpoints. The authentication scheme itself is configured by the Host.
        services.AddIdentityCore<User>(o => o.User.RequireUniqueEmail = true)
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<UsersDbContext>()
            .AddApiEndpoints();

        services.AddScoped<IUserDirectory, UserDirectory>();
        services.AddValidatorsFromAssemblyContaining<UsersDbContext>(includeInternalTypes: true);
        return services;
    }

    public static IEndpointRouteBuilder MapUsersEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Built-in Identity endpoints: /auth/login, /auth/refresh, /auth/manage/info, ...
        // Its /register is disabled in favour of /users/register, which also captures a display name.
        endpoints.MapGroup("/auth")
            .WithTags("Auth")
            .MapIdentityApi<User>()
            .AddEndpointFilter(async (context, next) =>
                context.HttpContext.Request.Path.Value?.EndsWith("/auth/register", StringComparison.OrdinalIgnoreCase) == true
                    ? TypedResults.NotFound()
                    : await next(context));

        var users = endpoints.MapGroup("/users").WithTags("Users");
        users.MapRegister();
        users.MapGetCurrentUser();
        return endpoints;
    }
}
