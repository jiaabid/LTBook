namespace ToolBook.ArchitectureTests;

public class ModuleBoundaryTests
{
    [Theory]
    [MemberData(nameof(Modules.All), MemberType = typeof(Modules))]
    public void Module_does_not_reference_other_modules_internals(string module)
    {
        var otherImplementations = Modules.Names.Where(n => n != module).Select(n => $"ToolBook.{n}");

        var violations = Modules.ReferencedToolBookAssemblies(Modules.Implementation(module))
            .Intersect(otherImplementations)
            .ToList();

        Assert.True(violations.Count == 0,
            $"ToolBook.{module} references other modules' internals: {string.Join(", ", violations)}. Use their *.Contracts instead.");
    }

    [Theory]
    [MemberData(nameof(Modules.All), MemberType = typeof(Modules))]
    public void Module_does_not_reference_the_host(string module)
    {
        Assert.DoesNotContain("ToolBook.Host", Modules.ReferencedToolBookAssemblies(Modules.Implementation(module)));
    }

    [Theory]
    [MemberData(nameof(Modules.All), MemberType = typeof(Modules))]
    public void Contracts_reference_only_the_shared_kernel(string module)
    {
        var violations = Modules.ReferencedToolBookAssemblies(Modules.Contracts(module))
            .Where(n => n != "ToolBook.SharedKernel")
            .ToList();

        Assert.True(violations.Count == 0,
            $"ToolBook.{module}.Contracts must stay dependency-free but references: {string.Join(", ", violations)}");
    }

    [Fact]
    public void Shared_kernel_does_not_reference_any_module()
    {
        Assert.Empty(Modules.ReferencedToolBookAssemblies(Modules.SharedKernel));
    }

    [Theory]
    [MemberData(nameof(Modules.All), MemberType = typeof(Modules))]
    public void Module_exposes_only_its_entry_point(string module)
    {
        // Everything inside a module is internal except the Add/Map entry point.
        // EF Core migrations are generated as public classes and are excluded.
        var publicTypes = Modules.Implementation(module).GetExportedTypes()
            .Where(t => t.Namespace is null || !t.Namespace.Contains(".Migrations", StringComparison.Ordinal))
            .Select(t => t.FullName)
            .ToList();

        Assert.Equal([$"ToolBook.{module}.{module}Module"], publicTypes);
    }
}
