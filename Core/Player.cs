using Civ2Like.Core.Interfaces;

namespace Civ2Like
{
    public sealed class Player : IIdObject
    {
        public Guid Id { get; }

        public Player(Guid id) => Id = id;

        public override bool Equals(object? obj) => obj is Player p && Id == p.Id;

        public override int GetHashCode() => Id.GetHashCode();
    }
}
