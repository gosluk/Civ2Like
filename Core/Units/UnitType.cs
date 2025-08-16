namespace Civ2Like.Core.Units;

public sealed class UnitType : UuidObject, IEquatable<UnitType>
{
    public Guid Id { get; } = Guid.NewGuid();

    public bool Equals(UnitType? other) => Id.Equals(other?.Id);

    public override int GetHashCode() => Id.GetHashCode();

    public override bool Equals(object? obj) => obj is Unit other && Equals(other);
}
