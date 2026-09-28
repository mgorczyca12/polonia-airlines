using System.Runtime.CompilerServices;

namespace Shared.Domain.Entities;

public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull
{
    protected Entity() { }

    protected Entity(TId id)
    {
        Id = id;
    }

    public TId Id { get; private set; } = default!;

    public bool Equals(Entity<TId>? other)
    {
        if (other is null || other.GetType() != GetType())
            return false;

        if (ReferenceEquals(this, other))
            return true;

        var comparer = EqualityComparer<TId>.Default;
        return !comparer.Equals(Id, default!) &&
               !comparer.Equals(other.Id, default!) &&
               comparer.Equals(Id, other.Id);
    }

    public override bool Equals(object? obj) => obj is Entity<TId> other && Equals(other);

    public override int GetHashCode() =>
        EqualityComparer<TId>.Default.Equals(Id, default!)
            ? RuntimeHelpers.GetHashCode(this)
            : HashCode.Combine(GetType(), Id);
}