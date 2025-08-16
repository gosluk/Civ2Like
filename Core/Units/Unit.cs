using Civ2Like.Hexagon;

namespace Civ2Like.Core.Units;

public sealed class Unit : UuidObject, IEquatable<Unit>
{
    public string Name { get; set; } = "Unit";

    public Player Player { get; }

    public Hex Pos { get; set; }

    public int MoveAllowance { get; } = 2;

    public int MovesLeft { get; set; }

    public MovementRules Rules { get; }

    public UnitState State { get; set; }

    public uint Health { get; set; } = 100;

    public Unit(Player owner, Hex pos, MovementRules? rules = null)
    {
        Player = owner;
        Pos = pos;
        Rules = rules ?? MovementRules.LandOnly();
        MovesLeft = MoveAllowance;
    }

    public Unit(Player owner, Hex pos, MovementPreset preset) : this(owner, pos, MovementRules.FromPreset(preset)) { }

    public bool Equals(Unit? other) => Id.Equals(other?.Id);
}
