using Civ2Like.Core;
using Civ2Like.Core.Units;
using Civ2Like.Hexagon;

namespace Civ2Like.Events.Items;

internal class UnitCreatedEvent : IGameEvent
{
    public required Hex Pos { get; init; }

    public required Guid PlayerId { get; init; }

    public void Apply(Game game)
    {
        var player = game.Players[PlayerId];

        game.Units.Add(new Unit(player, Pos, MovementPreset.Land)
        {
            Name = game.UnitNameGenerator.Next(),
        });
    }
}
