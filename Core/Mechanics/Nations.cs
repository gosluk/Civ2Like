using Civ2Like.Core.Nations;
using Civ2Like.Core.World;
using Civ2Like.Extensions;
using Civ2Like.Hexagon;

namespace Civ2Like.Core.Mechanics;

public static class Nations
{
    private static readonly Random _rand = new();

    private const decimal GROWTH_NOISE_MAX = 0.05m;   // was 0.2 → ±0.5% in the final rate
    private const decimal GROWTH_TERRAIN_SCALE = 4.0m;
    private const decimal GROWTH_RATE_SCALE = 0.10m; // your original 0.1

    private const decimal MIGRATION_BASE = 0.02m; // was 0.05 → ~2% baseline
    private const decimal MIGRATION_NOISE_MAX = 0.05m; // was 0.1
    private const decimal MIGRATION_MIN_MOVE = 1m;   // don’t bother if < 10 people

    // weights for attractiveness
    private const decimal ATTR_TERRAIN_W = 1.0m;


    public static void ApplyGrowth(Nation nation, IReadOnlyDictionary<Hex, Tile> map)
    {
        foreach (var pos in map.Where(i => i.Value.Populations.ContainsKey(nation)))
        {
            PopulationUnit pu = pos.Value.Populations[nation];

            decimal coefficient = TerrainFertility(pos.Value.Terrain) + _rand.NextDecimal() * GROWTH_NOISE_MAX;
            coefficient += nation.Bonuses.Sum(i => i.GrowthMultiplier);

            long growth = (long)(pu.Value * coefficient * GROWTH_RATE_SCALE);

            pu.Value += growth;
        }
    }

    private static decimal TerrainFertility(Terrain terrain) => (terrain switch
    {
        Terrain.Ocean => 0.00m,
        Terrain.Mountains => 0.00m,
        Terrain.Desert => 0.01m,
        Terrain.Snow => 0.005m,
        Terrain.Tundra => 0.01m,
        Terrain.Swamp => 0.012m,
        Terrain.Hills => 0.012m,
        Terrain.Forest => 0.015m,
        Terrain.Jungle => 0.015m,
        Terrain.Plains => 0.02m,
        Terrain.Grassland => 0.022m,
        Terrain.Coast => 0.0m,
        _ => throw new NotImplementedException(),
    }) * GROWTH_TERRAIN_SCALE;

    public static void ApplyMigration(Nation nation, Map map)
    {
        var originalState = map.MapData.ToList();

        foreach (var pos in originalState.Where(i => i.Value.Populations.ContainsKey(nation)))
        {
            PopulationUnit pu = pos.Value.Populations[nation];
            decimal coefficient = 1.0m + TerrainMigrationability(pos.Value.Terrain) + _rand.NextDecimal() * MIGRATION_NOISE_MAX;

            coefficient += nation.Bonuses.Sum(i => i.MigrationMultiplier);

            decimal allMigratingPeople = pu.Value * coefficient * MIGRATION_BASE;

            var neighbours = map.Neighbors(pos.Key).
                Select(i => map[i]).
                Where(i => TerrainFertility(i.Terrain) > 0.0m).
                OrderBy(_ => _rand.Next());

            foreach (var tile in neighbours)
            {
                if (allMigratingPeople < MIGRATION_MIN_MOVE)
                {
                    break;
                }

                decimal desirability = TerrainFertility(tile.Terrain);
                decimal migratingPeople = allMigratingPeople * desirability * 10;

                if (migratingPeople > 0)
                {
                    if (tile.Populations.ContainsKey(nation))
                    {
                        var puToModify = tile.Populations[nation];
                        if (puToModify.Value > pu.Value)
                        {
                            migratingPeople = 0;
                        }
                        puToModify.Value += migratingPeople;
                    }
                    else
                    {
                        tile.Populations[nation] = new PopulationUnit { Nation = nation, Value = migratingPeople, };
                    }
                }

                allMigratingPeople -= migratingPeople;
            }

            pu.Value -= allMigratingPeople;
        }
    }

    private static decimal TerrainMigrationability(Terrain terrain) => (terrain switch
    {
        Terrain.Ocean => 0.00m,
        Terrain.Mountains => 0.00m,
        Terrain.Desert => 0.01m,
        Terrain.Snow => 0.005m,
        Terrain.Tundra => 0.01m,
        Terrain.Swamp => 0.012m,
        Terrain.Hills => 0.012m,
        Terrain.Forest => 0.015m,
        Terrain.Jungle => 0.015m,
        Terrain.Plains => 0.02m,
        Terrain.Grassland => 0.022m,
        Terrain.Coast => 0.0m,
        _ => throw new NotImplementedException(),
    }) * ATTR_TERRAIN_W;
}
