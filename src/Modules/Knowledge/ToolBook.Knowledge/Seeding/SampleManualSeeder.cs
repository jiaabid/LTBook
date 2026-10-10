using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ToolBook.Knowledge.Domain;
using ToolBook.Knowledge.Persistence;
using ToolBook.SharedKernel.Time;
using ToolBook.Tools.Contracts;

namespace ToolBook.Knowledge.Seeding;

/// <summary>
/// When <c>Knowledge:SeedSampleManuals</c> is true (Development), adds the demo knowledge base for the seeded tools as
/// Pending documents. The fictional manuals live in <c>docs/knowledge-base</c> and are embedded at build time.
/// The ingestion worker (registered after this) picks them up at startup.
/// </summary>
internal sealed partial class SampleManualSeeder(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    IClock clock,
    ILogger<SampleManualSeeder> logger) : IHostedService
{
    public const string ResourcePrefix = "KnowledgeBase.";

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!configuration.GetValue<bool>("Knowledge:SeedSampleManuals"))
        {
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<KnowledgeDbContext>();
        var tools = await scope.ServiceProvider.GetRequiredService<IToolCatalog>().ListAsync(cancellationToken);

        var assembly = typeof(SampleManualSeeder).Assembly;
        foreach (var resource in assembly.GetManifestResourceNames().Where(n => n.StartsWith(ResourcePrefix, StringComparison.Ordinal)))
        {
            await using var stream = assembly.GetManifestResourceStream(resource)!;
            using var reader = new StreamReader(stream);
            var text = await reader.ReadToEndAsync(cancellationToken);

            var header = ToolHeaderRegex().Match(text);
            var tool = tools.FirstOrDefault(t => header.Success && t.Name == header.Groups[1].Value.Trim());
            if (tool is null)
            {
                continue;
            }

            var fileName = ResourceFileName(resource);
            if (await db.Documents.AnyAsync(d => d.ToolId == tool.Id && d.FileName == fileName, cancellationToken))
            {
                continue;
            }

            var content = Encoding.UTF8.GetBytes(text.Remove(header.Index, header.Length).TrimStart());
            db.Documents.Add(ToolDocument.Upload(tool.Id, fileName, "text/markdown", content, clock.UtcNow));
            LogSeeded(fileName, tool.Name);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    // "KnowledgeBase.sem-quick-guide.md" -> "sem-quick-guide.md" (LogicalName set in the .csproj)
    private static string ResourceFileName(string resource) => resource[ResourcePrefix.Length..];

    [GeneratedRegex(@"^<!--\s*tool:\s*(.+?)\s*-->\s*$", RegexOptions.Multiline)]
    private static partial Regex ToolHeaderRegex();

    [LoggerMessage(Level = LogLevel.Information, Message = "Seeded sample manual {FileName} for {ToolName}")]
    private partial void LogSeeded(string fileName, string toolName);
}
