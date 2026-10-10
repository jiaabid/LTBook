using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.MsSql;
using ToolBook.Knowledge.Assistant;
using ToolBook.SharedKernel.Time;
using ToolBook.UnitTests.Fakes;

namespace ToolBook.IntegrationTests.Infrastructure;

/// <summary>
/// The real application against a real SQL Server in Docker (Testcontainers). Migrations run at startup, exactly as in
/// development. Only the clock and the AI clients are replaced: the clock to control time, the AI clients to stay offline
/// and deterministic.
/// </summary>
public sealed class ToolBookApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string AdminEmail = "admin@test.local";
    public const string AdminPassword = "Admin_Test_Pass1!";

    private readonly MsSqlContainer _sql = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public TestClock Clock { get; } = new();

    public HashingEmbeddingGenerator Embeddings { get; } = new();

    /// <summary>Echoes the first excerpt it was given, with a citation, so tests can check grounding end to end.</summary>
    public ScriptedChatClient Chat { get; } = new(messages =>
    {
        var prompt = messages[^1].Text;
        var start = prompt.IndexOf("<<<", StringComparison.Ordinal) + 3;
        var end = prompt.IndexOf(">>>", start, StringComparison.Ordinal);
        return $"According to the manual: {prompt[start..end].Trim()} [1]";
    });

    public async Task InitializeAsync() => await _sql.StartAsync();

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _sql.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var connectionString = new SqlConnectionStringBuilder(_sql.GetConnectionString()) { InitialCatalog = "ToolBook" }.ConnectionString;

        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:ToolBook", connectionString);
        builder.UseSetting("Database:MigrateOnStartup", "true");
        builder.UseSetting("Seed:Admin:Email", AdminEmail);
        builder.UseSetting("Seed:Admin:Password", AdminPassword);
        builder.UseSetting("Assistant:RequestsPerMinute", "1000");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IClock>();
            services.AddSingleton<IClock>(Clock);

            services.RemoveAll<IChatClient>();
            services.RemoveAll<IEmbeddingGenerator<string, Embedding<float>>>();
            services.AddSingleton<IChatClient>(Chat);
            services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(Embeddings);
            services.Configure<AssistantOptions>(o => o.MinSimilarity = 0.3);
        });
    }
}

public sealed class TestClock : IClock
{
    private DateTimeOffset? _fixed;

    public DateTimeOffset UtcNow => _fixed ?? DateTimeOffset.UtcNow;

    public void Set(DateTimeOffset utcNow) => _fixed = utcNow;

    public void Reset() => _fixed = null;
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ToolBookApiFactory>
{
    public const string Name = "api";
}
