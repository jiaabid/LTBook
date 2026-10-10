using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ToolBook.SharedKernel.Persistence;

/// <summary>
/// Applies a module's EF Core migrations at startup when <c>Database:MigrateOnStartup</c> is true (dev and tests).
/// Hosted services start in registration order, so modules registered first are migrated first.
/// </summary>
public sealed partial class DatabaseMigrator<TContext>(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<DatabaseMigrator<TContext>> logger) : IHostedService
    where TContext : DbContext
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!configuration.GetValue<bool>("Database:MigrateOnStartup"))
        {
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TContext>();
        LogMigrating(typeof(TContext).Name);
        await db.Database.MigrateAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(Level = LogLevel.Information, Message = "Applying migrations for {DbContext}")]
    private partial void LogMigrating(string dbContext);
}
