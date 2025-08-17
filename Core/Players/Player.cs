using Avalonia.Media;

namespace Civ2Like.Core.Players;

public sealed class Player : UuidObject, IEquatable<Player>
{
    public Player(Guid id) => Id = id;

    public string Name { get; set; } = "Player";

    public Color ColorA { get; set; } = Colors.Black;

    public Color ColorB { get; set; } = Colors.Black;

    public long Gold { get; set; } = 0;

    public bool Equals(Player? other) => other is not null && Id.Equals(other.Id);
}
