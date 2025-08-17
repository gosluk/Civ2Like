namespace Civ2Like.Core;

public abstract class UuidObject
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    public override int GetHashCode() => Id.GetHashCode();

    public override bool Equals(object? obj) => obj is UuidObject other && Id == other.Id;
}
