using Civ2Like.Core;
using Civ2Like.Hexagon;

namespace Civ2Like.Events.Items;

public sealed class CityFoundedEvent : IGameEvent
{
    public Guid PlayerId { get; set; }
    public Guid CityId { get; set; }
    public string Name { get; set; } = "City";
    public int Q { get; set; }
    public int R { get; set; }

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
