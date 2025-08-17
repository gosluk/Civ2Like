using Civ2Like.Core;
using Civ2Like.Core.Cities;

namespace Civ2Like.Events.Items;

public sealed class CityProductionProgressedEvent : IGameEvent
{
    public required Guid CityId { get; init; }

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
            game.ProcessEvent(new UnitCreatedEvent
            {
                Pos = city.Pos,
                PlayerId = city.Player.Id,
                UnitTypeId = game.UnitTypes.First().Id
            });
        }
    }
}
