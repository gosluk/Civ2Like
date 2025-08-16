using Civ2Like.Core;
using Civ2Like.Hexagon;

namespace Civ2Like.Events.Items;

public sealed class UnitMovedEvent : IGameEvent
{
    public required Guid UnitId { get; init; }
    public required int FromQ { get; init; }
    public required int FromR { get; init; }
    public required int ToQ { get; init; }
    public required int ToR { get; init; }

    public void Apply(Game game)
    {
        var u = game.Units.First(x => x.Id == UnitId);
        if (u == null) return;
        u.Pos = game.Map.Canonical(new Hex(ToQ, ToR));
    }
}
