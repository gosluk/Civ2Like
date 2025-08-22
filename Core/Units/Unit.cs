using Civ2Like.Core.Nations;
using Civ2Like.Core.Players;
using Civ2Like.Hexagon;

namespace Civ2Like.Core.Units;

public sealed class Unit : UuidObject, IEquatable<Unit>
{
    public required string Name { get; set; }

    public Player Player { get; }

    public Hex Pos { get; set; }

    public uint MovesLeft { get; set; }

    public UnitState State { get; set; }

    public UnitType UnitType { get; set; }

    public uint Health { get; set; }

    public uint Kills { get; set; } = 0;

    public List<UnitBonus> Bonuses { get; } = new();

    public Nation? Nation { get; set; }

    public Unit(Player owner, Hex pos, UnitType unitType)
    {
        Player = owner;
        Pos = pos;
        UnitType = unitType;
        MovesLeft = unitType.MoveAllowance;
        Health = UnitType.MaxHealth;
    }

    public bool Equals(Unit? other) => Id.Equals(other?.Id);

    public UnitBonus EffectiveStats()
    {
        var effectiveStats = UnitType + new UnitBonus();

        foreach (var bonus in Bonuses)
        {
            effectiveStats += bonus;
        }

        return effectiveStats;
    }
}
