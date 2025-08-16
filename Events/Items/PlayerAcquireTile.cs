using Civ2Like.Core;
using Civ2Like.Hexagon;

namespace Civ2Like.Events.Items;

public sealed class PlayerAcquireTile : IGameEvent
{
    public Guid PlayerId { get; set; }

    public Hex Pos { get; set; }

    public void Apply(Game game)
    {
        game.Map[Pos].Owner = game.Players[PlayerId];
    }
}
