using Civ2Like.Hexagon;
using Civ2Like.View;
using Civ2Like.View.Core.Interfaces;

namespace Civ2Like.Core;

public sealed class Unit : IIdObject
{
    public Guid Id { get; internal set; }

    public string Name { get; set; } = "Unit";

    public Player Owner { get; }

    public Hex Pos { get; set; }

    public int MoveAllowance { get; } = 2;

    public int MovesLeft { get; set; }

    public MovementRules Rules { get; }

    public Unit(Player owner, Hex pos, MovementRules? rules = null)
    {
        Owner = owner;
        Pos = pos;
        Rules = rules ?? MovementRules.LandOnly();
        MovesLeft = MoveAllowance;
    }

    public Unit(Player owner, Hex pos, MovementPreset preset) : this(owner, pos, MovementRules.FromPreset(preset)) { }
}
