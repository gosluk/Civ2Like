using Civ2Like.Core;


namespace Civ2Like.Events.Items;

internal class UnitStateChangedEvent : IGameEvent
{
    public required Guid UnitId { get; init; }

    public required UnitState NewState { get; init; }

    public void Apply(Game game)
    {
        game.Units[UnitId].State = NewState;
    }
}
