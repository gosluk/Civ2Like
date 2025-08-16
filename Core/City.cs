using Civ2Like.Events;
using Civ2Like.Hexagon;
using Civ2Like.View.Core.Interfaces;

namespace Civ2Like.Core;

public sealed class City : IIdObject
{
    public Guid Id { get; internal set; }
    
    public Player Player { get; }
    
    public string Name { get; }
    
    public Hex Pos { get; }

    public uint Production { get; set; }

    public uint Growth { get; set; }

    public uint Population { get; set; } = 1;

    public City(Player owner, string name, Hex pos)
    {
        Player = owner; Name = name; Pos = pos;

        Growth = 3;

        SetProduction();
    }

    public void SetProduction() => Production = 5;

    public void SetGrowth() => Growth = 3 + Population / 2;

    public IEnumerable<IGameEvent> EndOfTurnEvents()
    {
        yield return new CityProductionProgressedEvent
        {
            CityId = Id,
        };

        yield return new CityGrowthProgressedEvent
        {
            CityId = Id,
        };
    }
}
