using Civ2Like.Core.Nations;
using Civ2Like.Core.World;
using Civ2Like.Hexagon;

namespace Civ2Like.Core.Mechanics;

public static class Nations
{
    private static readonly Random _rand = new();

    private const double GROWTH_NOISE_MAX = 0.05;   // was 0.2 → ±0.5% in the final rate
    private const double GROWTH_TERRAIN_SCALE = 4.0;
    private const double GROWTH_RATE_SCALE = 0.10; // your original 0.1

    private const double MIGRATION_BASE = 0.02; // was 0.05 → ~2% baseline
    private const double MIGRATION_NOISE_MAX = 0.05; // was 0.1
    private const uint MIGRATION_MIN_MOVE = 1;   // don’t bother if < 10 people

    // weights for attractiveness
    private const double ATTR_TERRAIN_W = 1.0;


    public static void ApplyGrowth(Nation nation, IReadOnlyDictionary<Hex, Tile> map)
    {
        foreach (var pos in map.Where(i => i.Value.Populations.ContainsKey(nation)))
        {
            PopulationUnit pu = pos.Value.Populations[nation];

            double coefficient = TerrainFertility(pos.Value.Terrain) + _rand.NextDouble() * GROWTH_NOISE_MAX;
            coefficient += nation.Bonuses.Sum(i => i.GrowthMultiplier);

            ulong growth = (ulong)(pu.Value * coefficient * GROWTH_RATE_SCALE);

            pu.Value += growth;
        }
    }

    private static double TerrainFertility(Terrain terrain) => (terrain switch
    {
        Terrain.Ocean => 0.00,
        Terrain.Mountains => 0.00,
        Terrain.Desert => 0.01,
        Terrain.Snow => 0.005,
        Terrain.Tundra => 0.01,
        Terrain.Swamp => 0.012,
        Terrain.Hills => 0.012,
        Terrain.Forest => 0.015,
        Terrain.Jungle => 0.015,
        Terrain.Plains => 0.02,
        Terrain.Grassland => 0.022,
        Terrain.Coast => 0.0,
        _ => throw new NotImplementedException(),
    }) * GROWTH_TERRAIN_SCALE;

    public static void ApplyMigration(Nation nation, Map map)
    {
        var originalState = map.MapData.ToList();

        foreach (var pos in originalState.Where(i => i.Value.Populations.ContainsKey(nation)))
        {
            PopulationUnit pu = pos.Value.Populations[nation];
            double coefficient = 1.0 + TerrainMigrationability(pos.Value.Terrain) + _rand.NextDouble() * MIGRATION_NOISE_MAX;

            coefficient += nation.Bonuses.Sum(i => i.MigrationMultiplier);

            ulong allMigratingPeople = (ulong)(pu.Value * coefficient * MIGRATION_BASE);

            var neighbours = map.Neighbors(pos.Key).
                Select(i => map[i]).
                Where(i => TerrainFertility(i.Terrain) > 0.0).
                OrderBy(_ => _rand.Next());

            foreach (var tile in neighbours)
            {
                if (allMigratingPeople < MIGRATION_MIN_MOVE)
                {
                    break;
                }

                double desirability = TerrainFertility(tile.Terrain); 
                uint migratingPeople = (uint)(allMigratingPeople * desirability * 10);

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

    private static double TerrainMigrationability(Terrain terrain) => (terrain switch
    {
        Terrain.Ocean => 0.00,
        Terrain.Mountains => 0.00,
        Terrain.Desert => 0.01,
        Terrain.Snow => 0.005,
        Terrain.Tundra => 0.01,
        Terrain.Swamp => 0.012,
        Terrain.Hills => 0.012,
        Terrain.Forest => 0.015,
        Terrain.Jungle => 0.015,
        Terrain.Plains => 0.02,
        Terrain.Grassland => 0.022,
        Terrain.Coast => 0.0,
        _ => throw new NotImplementedException(),
    }) * ATTR_TERRAIN_W;
}
