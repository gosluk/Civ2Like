using Avalonia.Media;
using Civ2Like.View.Core.Interfaces;

namespace Civ2Like.View
{
    public sealed class Player : IIdObject
    {
        public Guid Id { get; }

        public Player(Guid id) => Id = id;

        public string Name { get; set; } = "Player";

        public Color ColorA { get; set; } = Colors.Black;

        public Color ColorB { get; set; } = Colors.Black;

        public override bool Equals(object? obj) => obj is Player p && Id == p.Id;

        public override int GetHashCode() => Id.GetHashCode();
    }
}
