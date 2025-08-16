using Civ2Like.Core;
using Civ2Like.Hexagon;

namespace Civ2Like.Events.Items;

public sealed class UnitMovedEvent : IGameEvent
{
    public Guid UnitId { get; set; }
    public int FromQ { get; set; }
    public int FromR { get; set; }
    public int ToQ { get; set; }
    public int ToR { get; set; }
    public DateTime Utc { get; set; } = DateTime.UtcNow;
    public void Apply(Game game)
    {
        var u = game.Units.First(x => x.Id == UnitId);
        if (u == null) return;
        u.Pos = game.Map.Canonical(new Hex(ToQ, ToR));
    }
}
