using Civ2Like.Core;

namespace Civ2Like.Events.Items;

public sealed class UnitKilledEvent : IGameEvent
{
    public required Guid UnitId { get; init; }
    public required Guid KillerId { get; init; }

    public void Apply(Game game)
    {
        var unit = game.Units[UnitId];
        
        // Remove the unit from the game
        game.Units.Remove(unit);

        // Increment the killer's kill count
        var killer = game.Units[KillerId];
        killer.Kills++;
    }
}
