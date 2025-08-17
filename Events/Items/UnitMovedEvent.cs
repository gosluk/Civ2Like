using Civ2Like.Core;
using Civ2Like.Hexagon;

namespace Civ2Like.Events.Items;

public sealed class UnitMovedEvent : IGameEvent
{
    public required Guid UnitId { get; init; }
    public required Hex From { get; init; }
    public required Hex To { get; init; }

    public void Apply(Game game)
    {
        var u = game.Units[UnitId];
       
        u.Pos = game.Map.Canonical(To);
    }
}
