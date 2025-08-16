using Civ2Like.Core;

namespace Civ2Like.Events.Items;

public sealed class GameStartedEvent : IGameEvent
{
    public int Width { get; set; }
    public int Height { get; set; }
    public int Seed { get; set; }
    public DateTime Utc { get; set; } = DateTime.UtcNow;
    public void Apply(Game game) { }
}
