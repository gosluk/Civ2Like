using Civ2Like.Core.Cities;
using Civ2Like.Core.Economy;
using Civ2Like.Core.World;
using Civ2Like.Hexagon;

namespace Civ2Like.Core;

/// <summary>
/// Helpers to compute yields, storage, growth and production for cities.
/// </summary>
public static class CityEconomy
{
    // --- CONFIG (tune to your rulebook) ---
    public const decimal FoodPerPopConsumption = 2;
    public static decimal FoodToGrow(int population) => 20 + population * 4; // stockpiled food threshold
    public static decimal BaseStorageFood = 100;
    public static decimal BaseStorageMaterial = 80;

    // Terrain → base yields (no buildings/improvements, kept simple and safe)
    public static Yield TerrainYield(Terrain t) => t switch
    {
        Terrain.Grassland => new(food: 2, timber: 0, stone: 0, gold: 1, science: 0, culture: 0),
        Terrain.Plains => new(food: 1, timber: 1, stone: 0, gold: 1),
        Terrain.Forest => new(food: 1, timber: 2, stone: 0),
        Terrain.Hills => new(food: 0, timber: 0, stone: 2, gold: 1),
        Terrain.Mountains => new(food: 0, stone: 2, iron: 1),
        Terrain.Desert => new(food: 0, gold: 1),
        Terrain.Tundra => new(food: 0, timber: 1),
        Terrain.Snow => new(food: 0),
        Terrain.Jungle => new(food: 1, timber: 1),
        Terrain.Swamp => new(food: 1, timber: 1),
        Terrain.Coast => new(food: 1, gold: 2),
        Terrain.Ocean => new(gold: 1),
        _ => new()
    };

    public static Yield BuildingsYield(City c) => new(); // hook: sum building modifiers if/when you add them

    /// <summary>Compute per-turn yields for a city by scanning worked tiles in a hex radius.</summary>
    public static Yield ComputeCityYield(Game game, City c)
    {
        var tiles = EnumerateCityTiles(game, c);
        var fromTiles = tiles.Select(t => TerrainYield(GetTerrain(t))).Aggregate(new Yield(), (acc, y) => acc + y);
        var fromBuildings = BuildingsYield(c);
        // Abstract yields from population (taxes / literacy):
        var perPop = new Yield(gold: 1, science: 1, culture: 1);
        var fromPop = Multiply(perPop, c.Population);

        return fromTiles + fromBuildings + fromPop;
    }

    /// <summary>Apply consumption & growth using city stockpile.</summary>
    public static void ApplyConsumptionAndGrowth(City c, Yield yields)
    {
        // 1) Stockpile physical goods
        c.AddToStockpile(ResourceType.Food, yields.Food);
        c.AddToStockpile(ResourceType.Timber, yields.Timber);
        c.AddToStockpile(ResourceType.Stone, yields.Stone);
        c.AddToStockpile(ResourceType.Iron, yields.Iron);

        // 2) Consume food
        var need = c.Population * FoodPerPopConsumption;
        c.AddToStockpile(ResourceType.Food, -need);

        // TODO 3) Starvation check
        //if (c.GetStockpile(ResourceType.Food) < 0)
        //{
        //    // lose pop until non-negative
        //    while (c.Population > 1 && c.GetStockpile(ResourceType.Food) < 0)
        //    {
        //        c.Population--;
        //        c.AddToStockpile(ResourceType.Food, FoodPerPopConsumption); // refund one pop's need
        //    }
        //    // clamp
        //    c.SetStockpile(ResourceType.Food, Math.Max(0, c.GetStockpile(ResourceType.Food)));
        //}

        // TODO 4) Growth check
        //var threshold = FoodToGrow(c.Population);
        //while (c.GetStockpile(ResourceType.Food) >= threshold)
        //{
        //    c.AddToStockpile(ResourceType.Food, -threshold);
        //    c.Population++;
        //    threshold = FoodToGrow(c.Population);
        //}

        // 5) Enforce storage caps
        ClampStorage(c);
    }

    /// <summary>Apply production progress to current build queue head.</summary>
    public static void ApplyProduction(City c, decimal production /* “hammers” from Timber+Stone simplification */)
    {
        if (c.CurrentBuild is null) return;

        c.CurrentBuildProgress += production;
        if (c.CurrentBuildProgress >= c.CurrentBuild.ProductionCost)
        {
            c.CurrentBuild.OnCompleted(c.Game, c);
            c.CurrentBuild = null;
            c.CurrentBuildProgress = 0;
        }
    }

    /// <summary>Very simple: treat (Timber+Stone)/2 as production points.</summary>
    public static decimal ComputeProductionPoints(Yield yields)
        => (yields.Timber + yields.Stone) / 2;

    public static void ClampStorage(City c)
    {
        var capFood = c.StorageCapacity(ResourceType.Food);
        var capTim = c.StorageCapacity(ResourceType.Timber);
        var capStn = c.StorageCapacity(ResourceType.Stone);
        var capFe = c.StorageCapacity(ResourceType.Iron);

        c.SetStockpile(ResourceType.Food, Math.Min(capFood, c.GetStockpile(ResourceType.Food)));
        c.SetStockpile(ResourceType.Timber, Math.Min(capTim, c.GetStockpile(ResourceType.Timber)));
        c.SetStockpile(ResourceType.Stone, Math.Min(capStn, c.GetStockpile(ResourceType.Stone)));
        c.SetStockpile(ResourceType.Iron, Math.Min(capFe, c.GetStockpile(ResourceType.Iron)));
    }

    public static decimal StorageCapacity(this City c, ResourceType r)
    {
        decimal baseCap = r == ResourceType.Food ? BaseStorageFood : BaseStorageMaterial;
        // Hook: buildings could add capacity here, e.g. +50 granary, +50 warehouse, etc.
        return baseCap + c.Population * 10;
    }

    // --- helpers ---
    private static Yield Multiply(Yield y, decimal k) => new(
        gold: y.Gold*k, science: y.Science*k, culture: y.Culture*k,
        food: y.Food*k, timber: y.Timber*k, stone: y.Stone*k, iron: y.Iron*k);

    private static IEnumerable<object> EnumerateCityTiles(Game game, City c)
    {
        // If your City owns tiles explicitly, prefer those. Otherwise use a radius.
        var ownProp = c.GetType().GetProperty("OwnedTiles");
        if (ownProp?.GetValue(c) is IEnumerable<object> owned) return owned;

        var radiusProp = c.GetType().GetProperty("WorkRadius");
        int radius = radiusProp is null ? 2 : (int)radiusProp.GetValue(c)!;

        var center = (Hex)c.GetType().GetProperty("Position")!.GetValue(c)!;
        return HexSpiral(game.Map, center, radius);
    }

    private static IEnumerable<object> HexSpiral(Map map, Hex center, int radius)
    {
        // Ring 0 .. radius
        yield return GetTile(map, center)!;
        throw new NotImplementedException();
        //for (int r = 1; r <= radius; r++)
        //{
        //    foreach (var h in HexRing(center, r))
        //    {
        //        var t = GetTile(map, h);
        //        if (t != null) yield return t;
        //    }
        //}
    }

    private static object? GetTile(Map map, Hex pos)
    {
        var m = map.GetType().GetMethod("GetTile", new[] { typeof(Hex) });
        return m?.Invoke(map, [pos]);
    }

    private static Terrain GetTerrain(object tile)
    {
        var p = tile.GetType().GetProperty("Terrain") ?? tile.GetType().GetProperty("Terrain");
        return p != null ? (Terrain)p.GetValue(tile)! : Terrain.Plains;
    }
}
