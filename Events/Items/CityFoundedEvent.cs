using Civ2Like.Core;
using Civ2Like.Core.Cities;
using Civ2Like.Core.Nations;
using Civ2Like.Hexagon;

namespace Civ2Like.Events.Items;

public sealed class CityFoundedEvent : IGameEvent
{
    private const ulong AddPopulation = 1000;

    public required Guid PlayerId { get; init; }
    public required Guid CityId { get; init; }
    public required string Name { get; init; } = "City";
    public required int Q { get; init; }
    public required int R { get; init; }

    public void Apply(Game game)
    {
        var owner = game.Players.First(p => p.Id == PlayerId) ?? game.ActivePlayer;

        Hex pos = new(Q, R);

        var city = new City(owner, Name, pos);

        if (!game.Cities.ContainsKey(CityId))
        {
            game.Cities.Add(city);

            var tile = game.Map[pos];

            Nation founder = owner.Founder;

            if (tile.Populations.ContainsKey(founder))
            {
                tile.Populations[founder].Value += AddPopulation;
            }
            else
            {
                tile.Populations.Add(founder, new PopulationUnit() { Nation = founder, Value = AddPopulation });
            }
        }
    }
}
