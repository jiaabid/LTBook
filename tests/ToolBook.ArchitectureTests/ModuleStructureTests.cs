using System.Text.RegularExpressions;
using NetArchTest.Rules;

namespace ToolBook.ArchitectureTests;

public class ModuleStructureTests
{
    [Theory]
    [MemberData(nameof(Modules.All), MemberType = typeof(Modules))]
    public void Domain_does_not_depend_on_persistence_or_http(string module)
    {
        var result = Types.InAssembly(Modules.Implementation(module))
            .That().ResideInNamespace($"ToolBook.{module}.Domain")
            .ShouldNot().HaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore.Http",
                "Microsoft.AspNetCore.Routing",
                $"ToolBook.{module}.Features",
                $"ToolBook.{module}.Persistence")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Theory]
    [MemberData(nameof(Modules.All), MemberType = typeof(Modules))]
    public void Handlers_and_endpoints_live_in_feature_slices(string module)
    {
        var result = Types.InAssembly(Modules.Implementation(module))
            .That().HaveNameEndingWith("Handler").Or().HaveNameEndingWith("Endpoint")
            .Should().ResideInNamespaceStartingWith($"ToolBook.{module}.Features.")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Theory]
    [MemberData(nameof(Modules.All), MemberType = typeof(Modules))]
    public void Feature_slices_do_not_depend_on_each_other(string module)
    {
        var assembly = Modules.Implementation(module);
        var typesBySlice = assembly.GetTypes()
            .Where(t => t.Namespace?.StartsWith($"ToolBook.{module}.Features.", StringComparison.Ordinal) == true)
            .GroupBy(t => t.Namespace!)
            .ToDictionary(g => g.Key, g => g.Select(t => t.FullName!).ToList());

        foreach (var slice in typesBySlice.Keys)
        {
            // NetArchTest matches names by prefix, so "CancelBooking" would also match "CancelBookingsOfDeactivatedTool".
            // Select the slice with an anchored regex and list the other slices' types by full name.
            var others = typesBySlice.Where(kv => kv.Key != slice).SelectMany(kv => kv.Value).ToArray();
            if (others.Length == 0)
            {
                continue;
            }

            var result = Types.InAssembly(assembly)
                .That().ResideInNamespaceMatching($"^{Regex.Escape(slice)}$")
                .ShouldNot().HaveDependencyOnAny(others)
                .GetResult();

            Assert.True(result.IsSuccessful, $"Slice {slice}: {Describe(result)}");
        }
    }

    private static string Describe(TestResult result) =>
        result.IsSuccessful ? string.Empty : "Violations: " + string.Join(", ", result.FailingTypeNames ?? []);
}
