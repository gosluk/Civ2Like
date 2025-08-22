using Civ2Like.Core.Nations;
using Civ2Like.Core.World;
using Civ2Like.Hexagon;

namespace Civ2Like.Core.Mechanics;

public static class Nations
{
    public static void ApplyGrowth(Nation nation, IReadOnlyCollection<KeyValuePair<Hex, Tile>> map)
    {
        var originalState = map.ToList();
    }

    public static void ApplyMigration(Nation nation, IReadOnlyCollection<KeyValuePair<Hex, Tile>> map)
    {
        var originalState = map.ToList();
    }
}
