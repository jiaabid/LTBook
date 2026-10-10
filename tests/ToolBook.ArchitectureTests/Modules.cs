using System.Reflection;

namespace ToolBook.ArchitectureTests;

/// <summary>The modules of the system and the assemblies that make them up.</summary>
public static class Modules
{
    public static readonly string[] Names = ["Users", "Tools", "Bookings", "Knowledge"];

    public static Assembly Implementation(string module) => Assembly.Load($"ToolBook.{module}");

    public static Assembly Contracts(string module) => Assembly.Load($"ToolBook.{module}.Contracts");

    public static Assembly SharedKernel => Assembly.Load("ToolBook.SharedKernel");

    public static Assembly Host => typeof(Program).Assembly;

    public static TheoryData<string> All()
    {
        var data = new TheoryData<string>();
        foreach (var name in Names)
        {
            data.Add(name);
        }

        return data;
    }

    /// <summary>Names of assemblies the given assembly references at compile time (unused references are dropped by the compiler).</summary>
    public static IReadOnlyList<string> ReferencedToolBookAssemblies(Assembly assembly) =>
        [.. assembly.GetReferencedAssemblies()
            .Select(a => a.Name!)
            .Where(n => n.StartsWith("ToolBook.", StringComparison.Ordinal))];
}
