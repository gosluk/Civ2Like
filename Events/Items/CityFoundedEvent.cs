using Civ2Like.Core;
using Civ2Like.Core.Cities;
using Civ2Like.Hexagon;

namespace Civ2Like.Events.Items;

public sealed class CityFoundedEvent : IGameEvent
{
    public required Guid PlayerId { get; init; }
    public required Guid CityId { get; init; }
    public required string Name { get; init; } = "City";
    public required int Q { get; init; }
    public required int R { get; init; }

    public void Apply(Game game)
    {
        var owner = game.Players.First(p => p.Id == PlayerId) ?? game.ActivePlayer;

        var city = new City(owner, Name, new Hex(Q, R));

        if (!game.Cities.ContainsKey(CityId))
        {
            game.Cities.Add(city);
        }
    }
}
