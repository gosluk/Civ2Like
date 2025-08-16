using Civ2Like.Core.NameGeneration;
using Civ2Like.Hexagon;
using Civ2Like.View;
using Civ2Like.View.Core.Interfaces;

namespace Civ2Like.Core;

public sealed class Unit : IIdObject, IEquatable<Unit>
{
    public Guid Id { get; internal set; }

    public string Name { get; set; } = "Unit";

    public Player Player { get; }

    public Hex Pos { get; set; }

    public int MoveAllowance { get; } = 2;

    public int MovesLeft { get; set; }

    public MovementRules Rules { get; }

    public Unit(Player owner, Hex pos, MovementRules? rules = null)
    {
        Player = owner;
        Pos = pos;
        Rules = rules ?? MovementRules.LandOnly();
        MovesLeft = MoveAllowance;
    }

    public Unit(Player owner, Hex pos, MovementPreset preset) : this(owner, pos, MovementRules.FromPreset(preset)) { }

    public bool Equals(Unit? other) => Id.Equals(other?.Id);

    public override int GetHashCode() => Id.GetHashCode();

    public override bool Equals(object? obj) => obj is Unit other && Equals(other);
}
