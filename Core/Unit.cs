using Civ2Like.Core.Interfaces;

namespace Civ2Like
{
    public sealed class Unit : IIdObject
    {
        public Guid Id { get; internal set; }
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
}
