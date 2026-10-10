using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ToolBook.SharedKernel.Persistence;

public static class ModuleDbContextExtensions
{
    public const string ConnectionStringName = "ToolBook";

    /// <summary>
    /// Registers a module's DbContext against the shared database, with the migrations history table in the module's
    /// own schema (so each module migrates independently), a startup migrator and a health check.
    /// </summary>
    public static IServiceCollection AddModuleDbContext<TContext>(this IServiceCollection services, string schema)
        where TContext : DbContext
    {
        services.AddDbContext<TContext>((sp, options) => options.UseSqlServer(
            sp.GetRequiredService<IConfiguration>().GetConnectionString(ConnectionStringName),
            sql => sql.MigrationsHistoryTable(HistoryRepository.DefaultTableName, schema)));

        services.AddHostedService<DatabaseMigrator<TContext>>();
        services.AddHealthChecks().AddDbContextCheck<TContext>($"db-{schema}");
        return services;
    }
}
