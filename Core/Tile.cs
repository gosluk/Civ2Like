namespace Civ2Like
{
    public sealed class Tile
    {
        public Terrain Terrain { get; }
        public Tile(Terrain terrain) { Terrain = terrain; }
    }
}
