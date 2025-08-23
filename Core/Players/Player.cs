using Avalonia.Media;
using Civ2Like.Core.Nations;

namespace Civ2Like.Core.Players;

public sealed class Player : UuidObject, IEquatable<Player>
{
    public Player(Guid id) => Id = id;

    public string Name { get; set; } = "Player";

    public Color ColorA { get; set; } = Colors.Black;

    public Color ColorB { get; set; } = Colors.Black;

    public decimal Gold { get; set; } = 0;

    public required Nation Founder { get; set; }

    public PlayerProgression Progress { get; } = new();

    public bool Equals(Player? other) => other is not null && Id.Equals(other.Id);
}
