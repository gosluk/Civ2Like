using Civ2Like.Core;

namespace Civ2Like.Events.Items;

public sealed class CityProductionProgressedEvent : IGameEvent
{
    public Guid CityId { get; init; }

    public void Apply(Game game)
    {
        City city = game.Cities[CityId];

        if (city.Production > 1)
        {
            city.Production--;
        }
        else
        {
            city.SetProduction();
            game.Events.Process(game, new UnitCreatedEvent { Pos = city.Pos, PlayerId = city.Player.Id });
        }
    }
}
