using System.Text.RegularExpressions;
using ToolBook.Knowledge.Seeding;
using ToolBook.Tools.Persistence;

namespace ToolBook.UnitTests.Knowledge;

/// <summary>Guards the demo knowledge base in docs/knowledge-base: embedded into the build and mapped to real tools.</summary>
public partial class KnowledgeBaseTests
{
    private static readonly System.Reflection.Assembly KnowledgeAssembly = typeof(SampleManualSeeder).Assembly;

    private static IEnumerable<string> Manuals() =>
        KnowledgeAssembly.GetManifestResourceNames().Where(n => n.StartsWith(SampleManualSeeder.ResourcePrefix, StringComparison.Ordinal));

    [Fact]
    public void Every_manual_in_docs_knowledge_base_is_embedded()
    {
        var folder = Path.Combine(RepositoryRoot(), "docs", "knowledge-base");
        var expected = Directory.GetFiles(folder, "*.md")
            .Select(Path.GetFileName)
            .Where(f => f != "README.md")
            .Order();

        Assert.Equal(expected, Manuals().Select(r => r[SampleManualSeeder.ResourcePrefix.Length..]).Order());
    }

    [Fact]
    public void Every_manual_names_a_seeded_tool_and_is_marked_fictional()
    {
        var toolNames = SeedTools.All.Select(t => t.Name).ToHashSet();

        foreach (var resource in Manuals())
        {
            using var reader = new StreamReader(KnowledgeAssembly.GetManifestResourceStream(resource)!);
            var text = reader.ReadToEnd();

            var header = ToolHeaderRegex().Match(text);
            Assert.True(header.Success, $"{resource} has no '<!-- tool: ... -->' header.");
            Assert.Contains(header.Groups[1].Value.Trim(), toolNames);
            Assert.Contains("Fictional sample content", text);
        }
    }

    private static string RepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "ToolBook.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }

    [GeneratedRegex(@"^<!--\s*tool:\s*(.+?)\s*-->\s*$", RegexOptions.Multiline)]
    private static partial Regex ToolHeaderRegex();
}
