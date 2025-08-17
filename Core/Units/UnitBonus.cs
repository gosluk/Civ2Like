namespace Civ2Like.Core.Units;

public sealed class UnitBonus : UuidObject, IEquatable<UnitType>
{
    public string Name { get; init; } = "No Bonus";

    public uint MaxHealth { get; init; }

    public uint MoveAllowance { get; init; }

    public uint TileVisibility { get; init; }

    public uint AttackRange { get; init; }

    public uint AttackRanged { get; init; }

    public uint AttackMelee { get; init; }

    public uint DefenseRanged { get; init; }

    public uint DefenseMelee { get; init; }

    public bool Equals(UnitType? other) => Id.Equals(other?.Id);

    public static UnitBonus operator +(UnitBonus unitType, UnitBonus bonus)
    {
        return new UnitBonus
        {
            Name = unitType.Name,
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
