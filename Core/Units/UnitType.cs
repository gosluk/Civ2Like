namespace Civ2Like.Core.Units;

public sealed class UnitType : UuidObject, IEquatable<UnitType>
{
    public required string Name { get; init; }

    public required uint MaxHealth { get; init; }

    public required uint MoveAllowance { get; init; }

    public required MovementRules Rules { get; init; }

    public required uint TileVisibility { get; init; }

    public required uint AttackRange { get; init; }

    public required uint AttackRanged { get; init; }

    public required uint AttackMelee { get; init; }

    public required uint DefenseRanged { get; init; }

    public required uint DefenseMelee { get; init; }

    public bool Equals(UnitType? other) => Id.Equals(other?.Id);

    public static UnitBonus operator +(UnitType unitType, UnitBonus bonus)
    {
        return new UnitBonus
        {
            Name = "Combined",
            MaxHealth = unitType.MaxHealth + bonus.MaxHealth,
            MoveAllowance = unitType.MoveAllowance + bonus.MoveAllowance,
            TileVisibility = unitType.TileVisibility + bonus.TileVisibility,
            AttackRange = unitType.AttackRange + bonus.AttackRange,
            AttackRanged = unitType.AttackRanged + bonus.AttackRanged,
            AttackMelee = unitType.AttackMelee + bonus.AttackMelee,
            DefenseRanged = unitType.DefenseRanged + bonus.DefenseRanged,
            DefenseMelee = unitType.DefenseMelee + bonus.DefenseMelee
        };
    }
}
