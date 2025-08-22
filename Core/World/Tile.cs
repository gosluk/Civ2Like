using Civ2Like.Core.Nations;
using Civ2Like.Core.Players;

namespace Civ2Like.Core.World;

public sealed class Tile
{
    public Terrain Terrain { get; }

    public Tile(Terrain terrain) { Terrain = terrain; }

    public Player? Owner { get; set; }

    public Dictionary<Nation, PopulationUnit> Populations { get; } = new();
}
