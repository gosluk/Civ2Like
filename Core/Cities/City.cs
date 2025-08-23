using Civ2Like.Core.Economy;
using Civ2Like.Core.Players;
using Civ2Like.Core.Units;
using Civ2Like.Core.World;
using Civ2Like.Events;
using Civ2Like.Events.Items;
using Civ2Like.Hexagon;
using Civ2Like.Extensions;

namespace Civ2Like.Core.Cities;

public sealed class City : UuidObject, IEquatable<City>
{
    // Stockpile only for storable resources
    private readonly Dictionary<ResourceType, decimal> _stock = new()
    {
        { ResourceType.Food, 0 }, { ResourceType.Timber, 0 }, { ResourceType.Stone, 0 }, { ResourceType.Iron, 0 }
    };

    public IReadOnlyDictionary<ResourceType, decimal> Stock => _stock;

    public Player Player { get; }

    public string Name { get; }

    public Hex Pos { get; }

    public decimal Production { get; set; }

    public decimal Growth { get; set; }

    public required Game Game { get; init; }

    public City(Player owner, string name, Hex pos)
    {
        Player = owner; Name = name; Pos = pos;

        Growth = 3;

        SetProduction();
    }

    public void SetProduction() => Production = 5;

    public decimal Population
    {
        get
        {
            Tile tile = Game.Map[Pos];

            return tile.Populations.Sum(i => i.Value.Value);
        }
    }

    public IBuildable? CurrentBuild { get; set; }

    public decimal CurrentBuildProgress { get; set; }

    public decimal GetStockpile(ResourceType r) => _stock.TryGetValue(r, out var v) ? v : 0;

    public void SetStockpile(ResourceType r, decimal amount) => _stock[r] = System.Math.Max(0, amount);

    public void AddToStockpile(ResourceType r, decimal delta) => SetStockpile(r, GetStockpile(r) + delta);

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

    public bool Equals(City? other) => Id.Equals(other?.Id);
}
