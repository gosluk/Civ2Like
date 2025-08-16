using Civ2Like.Core;

namespace Civ2Like.Events.Items;

public sealed class CityPopulationUpdateEvent : IGameEvent
{
    public Guid CityId { get; init; }

    public int PopulationChange { get; init; }

    public void Apply(Game game)
    {
        City city = game.Cities[CityId];
    }
}
