using Civ2Like.Core.Nations;
using Civ2Like.Core.Players;

namespace Civ2Like.Core.World;

public sealed class Tile
{
    public Terrain Terrain { get; }

    public Tile(Terrain terrain) { Terrain = terrain; }

    public Player? Owner { get; set; }

    public Dictionary<Nation, PopulationUnit> Populations { get; private set; } = new();

    public Tile Clone() => new Tile(Terrain)
    {
        Owner = Owner,
        Populations = Populations.ToDictionary(kv => kv.Key, kv => kv.Value.Clone()),
    };
}
