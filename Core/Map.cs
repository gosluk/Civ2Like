using Civ2Like.Hexagon;
using Civ2Like.View;

namespace Civ2Like.Core;

public sealed class Map
{
    private readonly Dictionary<Hex, Tile> _tiles;
    public int Width  { get; }
    public int Height { get; }

    public Map(int width, int height)
    {
        Width = width; Height = height;
        _tiles = new Dictionary<Hex, Tile>(width * height);

        for (int r = 0; r < Height; r++)
        {
            int qStart = QStart(r);
            for (int c = 0; c < Width; c++)
                _tiles[new Hex(qStart + c, r)] = new Tile(Terrain.Ocean);
        }
    }

    public static int QStart(int r) => -(int)Math.Floor(r / 2.0);

    private static int Mod(int a, int m)
    {
        int r = a % m;
        return r < 0 ? r + m : r;
    }

    public Hex FromColRow(int col, int row)
    {
        int rr = Mod(row, Height);
        int qStart = QStart(rr);
        int cc = Mod(col, Width);
        return new Hex(qStart + cc, rr);
    }

    public Hex Canonical(Hex h)
    {
        int rr = Mod(h.R, Height);
        int c  = h.Q - QStart(h.R);
        int cc = Mod(c, Width);
        int q  = QStart(rr) + cc;
        return new Hex(q, rr);
    }

    public Tile this[Hex h]
    {
        get => _tiles[Canonical(h)];
        set => _tiles[Canonical(h)] = value;
    }

    public IEnumerable<Hex> AllHexes()
    {
        for (int r = 0; r < Height; r++)
        for (int c = 0; c < Width; c++)
            yield return FromColRow(c, r);
    }

    public IEnumerable<Hex> Neighbors(Hex h)
    {
        var ch = Canonical(h);
        foreach (var d in Hex.NeighborDirs)
            yield return Canonical(ch.Add(d));
    }
}
