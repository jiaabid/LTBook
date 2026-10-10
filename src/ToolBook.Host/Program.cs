using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Identity;
using ToolBook.Bookings;
using ToolBook.Knowledge;
using ToolBook.SharedKernel.Events;
using ToolBook.SharedKernel.Http;
using ToolBook.SharedKernel.Time;
using ToolBook.Tools;
using ToolBook.Users;
using ToolBook.Users.Contracts;

var builder = WebApplication.CreateBuilder(args);

// Cross-cutting infrastructure
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddScoped<IEventBus, InProcessEventBus>();

// Opaque bearer tokens issued by the Identity API endpoints (ADR-0005).
builder.Services.AddAuthentication(IdentityConstants.BearerScheme)
    .AddBearerToken(IdentityConstants.BearerScheme);
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Policies.Admin, p => p.RequireRole(Roles.Admin));

builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy(Policies.AssistantRateLimit, http => RateLimitPartition.GetFixedWindowLimiter(
        http.User.Identity?.Name ?? http.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = builder.Configuration.GetValue("Assistant:RequestsPerMinute", 10),
            Window = TimeSpan.FromMinutes(1),
        }));
});

// Modules. Registration order is also migration order at startup.
builder.Services
    .AddUsersModule(builder.Configuration)
    .AddToolsModule(builder.Configuration)
    .AddBookingsModule(builder.Configuration)
    .AddKnowledgeModule(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapHealthChecks("/health");

var api = app.MapGroup("/api");
api.MapUsersEndpoints();
api.MapToolsEndpoints();
api.MapBookingsEndpoints();
api.MapKnowledgeEndpoints();

app.Run();

/// <summary>Exposed for <c>WebApplicationFactory</c> in the integration tests.</summary>
public partial class Program;
