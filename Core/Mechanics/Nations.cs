using Civ2Like.Core.Nations;
using Civ2Like.Core.World;
using Civ2Like.Hexagon;

namespace Civ2Like.Core.Mechanics;

public static class Nations
{
    private static readonly Random _rand = new();

    public static void ApplyGrowth(Nation nation, IReadOnlyDictionary<Hex, Tile> map)
    {
        foreach (var pos in map.Where(i => i.Value.Populations.ContainsKey(nation)))
        {
            PopulationUnit pu = pos.Value.Populations[nation];

            double coefficient = TerrainFertility(pos.Value.Terrain) * 4 + _rand.NextDouble() * 0.2;
            coefficient += nation.Bonuses.Sum(i => i.GrowthMultiplier);

            ulong growth = (ulong)(pu.Value * coefficient * 0.1);

            pu.Value += growth;
        }
    }

    private static double TerrainFertility(Terrain terrain) => terrain switch
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
    };

    public static void ApplyMigration(Nation nation, Map map)
    {
        var originalState = map.MapData.ToList();

        foreach (var pos in originalState.Where(i => i.Value.Populations.ContainsKey(nation)))
        {
            PopulationUnit pu = pos.Value.Populations[nation];
            double coefficient = 1.0 + TerrainMigrationability(pos.Value.Terrain) + _rand.NextDouble() * 0.1;

            coefficient += nation.Bonuses.Sum(i => i.MigrationMultiplier);

            ulong allMigratingPeople = (ulong)(pu.Value * coefficient * 0.05);

            if (allMigratingPeople < 3)
            {
                continue;
            }

            var neighbours = map.Neighbors(pos.Key).OrderBy(_ => _rand.Next());

            foreach (var neighbour in neighbours)
            {
                if (allMigratingPeople < 10)
                {
                    break;
                }

                var tile = map[neighbour];

                double desirability = TerrainFertility(tile.Terrain);

                if (desirability < 0.01)
                {
                    continue;
                }

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

    private static double TerrainMigrationability(Terrain terrain) => terrain switch
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
    };
}
