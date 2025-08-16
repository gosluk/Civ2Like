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

    public City(Player owner, string name, Hex pos)
    {
        Player = owner; Name = name; Pos = pos;

        SetProduction();
    }

    public void SetProduction() => Production = 5;

    public IEnumerable<IGameEvent> EndOfTurnEvents()
    {
        yield return new CityProductionProcessed
        {
            CityId = Id,
        };
    }
}
