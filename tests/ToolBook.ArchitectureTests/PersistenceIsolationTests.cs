using Microsoft.EntityFrameworkCore;

namespace ToolBook.ArchitectureTests;

public class PersistenceIsolationTests
{
    public static TheoryData<string, Type> DbContexts()
    {
        var data = new TheoryData<string, Type>();
        foreach (var module in Modules.Names)
        {
            foreach (var type in Modules.Implementation(module).GetTypes()
                         .Where(t => t is { IsAbstract: false } && typeof(DbContext).IsAssignableFrom(t)))
            {
                data.Add(module, type);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(DbContexts))]
    public void DbContext_maps_only_its_own_schema_and_entities(string module, Type dbContextType)
    {
        using var db = CreateOffline(dbContextType);
        var expectedSchema = module.ToLowerInvariant();
        var moduleAssembly = Modules.Implementation(module);

        Assert.Equal(expectedSchema, db.Model.GetDefaultSchema());
        foreach (var entity in db.Model.GetEntityTypes())
        {
            Assert.True(entity.GetSchema() == expectedSchema,
                $"{dbContextType.Name} maps {entity.DisplayName()} to schema '{entity.GetSchema()}', expected '{expectedSchema}'.");

            // Entities must belong to the module itself (framework types such as Identity's are allowed).
            var owner = entity.ClrType.Assembly;
            Assert.True(owner == moduleAssembly || owner.GetName().Name!.StartsWith("Microsoft.", StringComparison.Ordinal),
                $"{dbContextType.Name} maps {entity.ClrType.FullName} from {owner.GetName().Name}; modules may only map their own entities.");
        }
    }

    [Theory]
    [MemberData(nameof(Modules.All), MemberType = typeof(Modules))]
    public void Module_has_exactly_one_DbContext(string module)
    {
        var count = Modules.Implementation(module).GetTypes()
            .Count(t => !t.IsAbstract && typeof(DbContext).IsAssignableFrom(t));

        Assert.Equal(1, count);
    }

    /// <summary>Builds the model without a database: model building never opens a connection.</summary>
    private static DbContext CreateOffline(Type dbContextType)
    {
        var builder = (DbContextOptionsBuilder)Activator.CreateInstance(
            typeof(DbContextOptionsBuilder<>).MakeGenericType(dbContextType))!;
        builder.UseSqlServer("Server=offline;Database=offline");
        return (DbContext)Activator.CreateInstance(dbContextType, builder.Options)!;
    }
}
