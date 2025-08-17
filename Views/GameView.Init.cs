using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Civ2Like.Core;
using System.Collections.Immutable;

namespace Civ2Like.Views;

public sealed partial class GameView
{
    private IImage _unitIcon;
    private IImage _cityIcon;

    private void LoadUnitsGraphics()
    {
        using var stream = AssetLoader.Open(new Uri("avares://Civ2Like/Resources/Units/Unit.png"));
        _unitIcon = new Bitmap(stream);
    }

    private void LoadCitiesGraphics()
    {
        using var stream = AssetLoader.Open(new Uri("avares://Civ2Like/Resources/Board/City.png"));
        _cityIcon = new Bitmap(stream);
    }

    private void LoadTerrain()
    {
        Dictionary<Terrain, TerrainData> data = new();

        void AddBrush(Terrain t, IBrush brush)
        {
            data[t] = new TerrainData
            {
                Brush = brush,
                Image = null
            };
        }

        void AddImage(Terrain t, IBrush brush, string path)
        {
            var stream = AssetLoader.Open(new Uri($"avares://Civ2Like/Resources/Terrain/{path}"));
            var bmp = new Bitmap(stream);
            data[t] = new TerrainData
            {
                Brush = new ImageBrush(bmp)
                {
                    Stretch = Stretch.UniformToFill,
                    AlignmentX = AlignmentX.Center,
                    AlignmentY = AlignmentY.Center,
                    TileMode = TileMode.Tile,
                    DestinationRect = new RelativeRect(new Rect(0, 0, 1.01, 1.0), RelativeUnit.Relative),
                    SourceRect = new RelativeRect(new Rect(0, 0, bmp.Size.Width, bmp.Size.Height), RelativeUnit.Absolute),
                },
                Image = bmp
            };
        }

        //AddImage(Terrain.Grassland, new SolidColorBrush(Color.FromArgb(255, 80, 160, 80)), "Savanna.png");
        //AddImage(Terrain.Plains, new SolidColorBrush(Color.FromArgb(255, 170, 170, 90)), "Plains.png");

        AddBrush(Terrain.Grassland, new SolidColorBrush(Color.FromArgb(255, 80, 160, 80)));
        AddBrush(Terrain.Plains, new SolidColorBrush(Color.FromArgb(255, 170, 170, 90)));

        AddBrush(Terrain.Ocean, new SolidColorBrush(Color.FromArgb(255, 50, 90, 180)));
        AddBrush(Terrain.Coast, new SolidColorBrush(Color.FromArgb(255, 80, 130, 210)));
        AddBrush(Terrain.Forest, new SolidColorBrush(Color.FromArgb(255, 40, 120, 40)));
        AddBrush(Terrain.Hills, new SolidColorBrush(Color.FromArgb(255, 130, 110, 80)));
        AddBrush(Terrain.Mountains, new SolidColorBrush(Color.FromArgb(255, 110, 100, 110)));
        AddBrush(Terrain.Desert, new SolidColorBrush(Color.FromArgb(255, 220, 200, 120)));
        AddBrush(Terrain.Tundra, new SolidColorBrush(Color.FromArgb(255, 200, 220, 240)));

        TerrainBrushes = data.ToImmutableDictionary();
    }

    private sealed class TerrainData
    {
        public IBrush Brush { get; init; }

        public IImage? Image { get; init; }
    }

    private static ImmutableDictionary<Terrain, TerrainData> TerrainBrushes;

    private static IBrush TerrainBrush(Terrain t) => TerrainBrushes[t].Brush;
}
