namespace ToolBook.SharedKernel.Domain;

/// <summary>Base type for entities: identity by <see cref="Id"/>.</summary>
public abstract class Entity
{
    protected Entity(Guid id) => Id = id;

    public Guid Id { get; private init; }

    public override bool Equals(object? obj) =>
        obj is Entity other && other.GetType() == GetType() && other.Id == Id;

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}
