using Civ2Like.Core;
using Civ2Like.Hexagon;

namespace Civ2Like.Events.Items;

internal class UnitCreatedEvent : IGameEvent
{
    public Hex Pos { get; set; }

    public Guid PlayerId { get; set; }

    public void Apply(Game game)
    {
        var player = game.Players[PlayerId];

        game.Units.Add(new Unit(player, Pos, MovementPreset.Land)
        {
            Name = game.UnitNameGenerator.Next(),
        });
    }
}
