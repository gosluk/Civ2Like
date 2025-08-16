namespace Civ2Like.Core;

public sealed class Tile
{
    public Terrain Terrain { get; }

    public Tile(Terrain terrain) { Terrain = terrain; }
}
