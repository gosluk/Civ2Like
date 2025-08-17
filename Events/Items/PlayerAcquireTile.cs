using Civ2Like.Core;
using Civ2Like.Hexagon;

namespace Civ2Like.Events.Items;

public sealed class PlayerAcquireTile : IGameEvent
{

    public required Guid PlayerId { get; init; }

    public required Hex Pos { get; init; }

    public void Apply(Game game)
    {
        game.Map[Pos].Owner = game.Players[PlayerId];
    }
}
