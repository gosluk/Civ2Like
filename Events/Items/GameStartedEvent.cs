using Civ2Like.Core;

namespace Civ2Like.Events.Items;

public sealed class GameStartedEvent : IGameEvent
{
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required int Seed { get; init; }
    public void Apply(Game game) { }
}
