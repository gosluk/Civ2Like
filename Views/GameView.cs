using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Media.TextFormatting;
using Avalonia.Platform;
using Avalonia.Threading;
using Civ2Like.Config;
using Civ2Like.Core;
using Civ2Like.Core.Players;
using Civ2Like.Core.Units;
using Civ2Like.Core.World;
using Civ2Like.Hexagon;
using Civ2Like.Views.Events;
using CommunityToolkit.Mvvm.Messaging;
using System.Threading.Tasks.Dataflow;

namespace Civ2Like.Views;

public sealed partial class GameView : Control, IDisposable
{
    private readonly Game _game;
    private readonly Typeface _typeface = new("Segoe UI", weight: FontWeight.Bold);
    private readonly double _sizeX = GameConfig.HexSizeX;
    private readonly double _sizeY = GameConfig.HexSizeY;
    private readonly double _pad = GameConfig.HexSizeX + GameConfig.HexSizeY;

    private Hex? _hoverScreenHex;
    private Hex? _hoverWorldHex;
    private readonly ITargetBlock<Hex> _hoverNotifier;
    private IReadOnlyList<Hex>? _currentPath;
    private Point _origin;

    private int _viewColOffset = 0;
    private int _viewRowOffset = 0;

    private bool _attackMode = false;
    private bool _attackRanged = false;
    private Unit? _attackHoverUnit; // enemy under cursor when in attack mode

    public GameView()
    {
        _game = new Game(GameConfig.Width, GameConfig.Height, GameConfig.Seed);
        Focusable = true;

        PointerMoved += OnPointerMoved;
        PointerPressed += OnPointerPressed;
        KeyDown += OnKeyDown;

        _origin = ComputeOrigin();

        _hoverNotifier = new ActionBlock<Hex>(async msg =>
            {
                await Task.Delay(300);
                await Dispatcher.UIThread.InvokeAsync(InvalidateVisual);
                RefreshTile(msg);
            },
            new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = 1,
                MaxDegreeOfParallelism = 1,
            });

        LoadTerrain();

        LoadUnitsGraphics();

        LoadCitiesGraphics();

        Width = GameConfig.Width * _sizeX * 1.8 + _pad + _origin.X;
        Height = GameConfig.Height * _sizeY * 1.5 + _pad + _origin.Y;
        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top;

        WeakReferenceMessenger.Default.Register(this);
    }

    private void RefreshTile(Hex? tile)
    {
        WeakReferenceMessenger.Default.Send(new TileSelectionChangedEvent(_game, tile ?? _hoverWorldHex ?? null));
    }

    public void Dispose()
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }

    private static int Mod(int a, int m) { int r = a % m; return r < 0 ? r + m : r; }

    private Unit? GetUnitAt(Hex world)
    {
        world = _game.Map.Canonical(world);
        return _game.Units.FirstOrDefault(u => u.Pos == world);
    }

    private Point ComputeOrigin()
    {
        double minX = double.MaxValue, minY = double.MaxValue;
        for (int r = 0; r < GameConfig.Height; r++)
            for (int c = 0; c < GameConfig.Width; c++)
            {
                var screenHex = _game.Map.FromColRow(c, r);
                var (x, y) = HexLayout.HexToPixel(screenHex, _sizeX, _sizeY);
                if (x < minX) minX = x;
                if (y < minY) minY = y;
            }
        return new Point(_pad - minX, _pad - minY);
    }

    private Point ToPixelScreen(Hex screenHex)
    {
        var (x, y) = HexLayout.HexToPixel(_game.Map.Canonical(screenHex), _sizeX, _sizeY);
        return new Point(x + _origin.X, y + _origin.Y);
    }

    private (int col, int row) ScreenColRowFromAxial(Hex screenHex)
    {
        int row = screenHex.R;
        int col = screenHex.Q - Map.QStart(row);
        return (col, row);
    }

    private Hex WorldHexAtScreen(int screenCol, int screenRow)
        => _game.Map.FromColRow(screenCol + _viewColOffset, screenRow + _viewRowOffset);

    private Hex ScreenHexFromWorld(Hex world)
    {
        int row = Mod(world.R - _viewRowOffset, GameConfig.Height);
        int colWorld = world.Q - Map.QStart(world.R);
        int col = Mod(colWorld - _viewColOffset, GameConfig.Width);
        return _game.Map.FromColRow(col, row);
    }

    private Hex PixelToWorldHex(Point p)
    {
        var approxScreen = HexLayout.PixelToHex(p.X - _origin.X, p.Y - _origin.Y, _sizeX, _sizeY);
        var (sc, sr) = ScreenColRowFromAxial(approxScreen);
        return WorldHexAtScreen(Mod(sc, GameConfig.Width), Mod(sr, GameConfig.Height));
    }

    private void SelectPlayer(Player player)
    {
        WeakReferenceMessenger.Default.Send(new PlayerSelectionChangedEvent(_game, player));
    }

    private void DrawBoard(DrawingContext ctx)
    {
        for (int r = 0; r < GameConfig.Height; r++)
        {
            for (int c = 0; c < GameConfig.Width; c++)
            {
                var screenHex = _game.Map.FromColRow(c, r);
                var worldHex = WorldHexAtScreen(c, r);
                var t = _game.Map[worldHex].Terrain;
                DrawHexAt(ctx, screenHex, TerrainBrush(t), outline: Brushes.Black, thickness: 1.3);
            }
        }
    }

    private void DrawHoveredHex(DrawingContext ctx, Hex sh, double thickness, double fillOpacity)
    {
        DrawHexAt(ctx, sh, fill: new SolidColorBrush(Colors.White, fillOpacity), outline: Brushes.White, thickness: thickness, dashed: true);
    }

    private readonly static Pen PathPenNormal = new Pen(Brushes.White, 2);
    private readonly static Pen PathPenDashed = new Pen(Brushes.White, 2, dashStyle: new ImmutableDashStyle([1, 2], 0));
    private readonly static Pen PathPenBlack = new Pen(Brushes.Black, 2);

    private void DrawCurrentPath(DrawingContext ctx)
    {
        if (_currentPath is { Count: > 1 })
        {
            for (int i = 0; i < _currentPath.Count - 1; i++)
            {
                var hexA = ScreenHexFromWorld(_currentPath[i]);
                var hexB = ScreenHexFromWorld(_currentPath[i + 1]);
                var a = ToPixelScreen(hexA);
                var b = ToPixelScreen(hexB);
                ctx.DrawLine(hexA.Distance(hexB) > 2 ? PathPenDashed : PathPenNormal, a, b);

                if (i == _currentPath.Count - 2)
                {
                    ctx.DrawEllipse(Avalonia.Media.Brushes.Black, PathPenBlack, b, 7, 7);
                    ctx.DrawEllipse(Avalonia.Media.Brushes.White, PathPenNormal, b, 5, 5);
                }
            }
        }
    }

    private void DrawCities(DrawingContext ctx)
    {
        foreach (var city in _game.Cities)
        {
            var screenHex = ScreenHexFromWorld(city.Pos);
            var center = ToPixelScreen(screenHex);
            var rect = new Rect(center.X - 10, center.Y - 10, 20, 20);

            ctx.DrawImage(_cityIcon, new Rect(center.X - _cityIcon.Size.Width / 2, center.Y - _cityIcon.Size.Height / 2, _cityIcon.Size.Width, _cityIcon.Size.Height));
            DrawFlag(ctx, center, city.Player);

            string cityName = $"{city.Name} [{city.Production}, {city.Growth}]";
            using var nameLayout0 = new TextLayout(cityName, _typeface, 12, Brushes.White, TextAlignment.Center);
            using var nameLayout1 = new TextLayout(cityName, _typeface, 12, Brushes.Black, TextAlignment.Center);

            nameLayout1.Draw(ctx, new Point(center.X - nameLayout0.Width / 2 + 1, center.Y + _cityIcon.Size.Height / 2 + 1));
            nameLayout1.Draw(ctx, new Point(center.X - nameLayout0.Width / 2 + 1, center.Y + _cityIcon.Size.Height / 2 - 1));
            nameLayout1.Draw(ctx, new Point(center.X - nameLayout0.Width / 2 - 1, center.Y + _cityIcon.Size.Height / 2 - 1));
            nameLayout1.Draw(ctx, new Point(center.X - nameLayout0.Width / 2 - 1, center.Y + _cityIcon.Size.Height / 2 + 1));
            nameLayout0.Draw(ctx, new Point(center.X - nameLayout0.Width / 2, center.Y + _cityIcon.Size.Height / 2));
        }
    }

    private void DrawFlag(DrawingContext ctx, Point center, Player player)
    {
        Point pos = new Point(center.X + 10, center.Y - 10);
        var flagBrush0 = new SolidColorBrush(player.ColorA);
        var flagBrush1 = new SolidColorBrush(player.ColorB);

        const int margin = 1;
        const int size = 16;
        ctx.DrawRectangle(Brushes.Black, null, new Rect(pos.X, pos.Y, size, size), new BoxShadows(new BoxShadow() { Blur = 0.8 }));

        ctx.DrawRectangle(flagBrush0, null, new Rect(pos.X + margin, pos.Y + margin, size - margin * 2, size / 2));
        ctx.DrawRectangle(flagBrush1, null, new Rect(pos.X + margin, pos.Y + size / 2, size - margin * 2, size / 2 - margin));
    }

    private void DrawUnits(DrawingContext ctx)
    {
        foreach (var unit in _game.Units)
        {
            var screenHex = ScreenHexFromWorld(unit.Pos);
            var center = ToPixelScreen(screenHex);

            if (unit == _game.SelectedUnit)
            {
                DrawHoveredHex(ctx, ScreenHexFromWorld(unit.Pos), 4.0, 0.8);
            }

            DrawFlag(ctx, center, unit.Player);
            ctx.DrawImage(_unitIcon, new Rect(center.X - _unitIcon.Size.Width / 2, center.Y - _unitIcon.Size.Height / 2, _unitIcon.Size.Width, _unitIcon.Size.Height));
        }
    }

    private IBrush CreateBorderFillBrush(Color c1, Color c2)
    {
        const int tileSize = 2;
        var tile = new Grid
        {
            Width = tileSize * 4,
            Height = tileSize * 4,
            RowDefinitions = new RowDefinitions("*,*"),
            ColumnDefinitions = new ColumnDefinitions("* ,*")
        };
        tile.RowDefinitions.Add(new RowDefinition());
        tile.RowDefinitions.Add(new RowDefinition());
        tile.ColumnDefinitions.Add(new ColumnDefinition());
        tile.ColumnDefinitions.Add(new ColumnDefinition());

        SolidColorBrush b1 = new SolidColorBrush(c2);
        SolidColorBrush b2 = new SolidColorBrush(c1);
        Thickness t = new Thickness(1);

        tile.Children.Add(new Border { Background = b1, Width = tileSize, Height = tileSize, Margin = t });                 // (0,0)

        tile.Children.Add(new Border { Background = b2, Width = tileSize, Height = tileSize, Margin = t }); // (0,1)
        Grid.SetColumn(tile.Children[1], tileSize);

        tile.Children.Add(new Border { Background = b2, Width = tileSize, Height = tileSize, Margin = t });    // (1,0)
        Grid.SetRow(tile.Children[2], tileSize);

        tile.Children.Add(new Border { Background = b1, Width = tileSize, Height = tileSize, Margin = t }); // (1,1)
        Grid.SetColumn(tile.Children[3], tileSize);
        Grid.SetRow(tile.Children[3], tileSize);

        return new VisualBrush
        {
            Visual = tile,
            Stretch = Stretch.None,           // use exact tile size
            TileMode = TileMode.Tile,         // repeat
            Opacity = 0.6,
            DestinationRect = new RelativeRect(new Rect(0, 0, tileSize * 4, tileSize * 4), RelativeUnit.Absolute)
        };
    }

    private void DrawBorders(DrawingContext ctx)
    {
        foreach (var player in _game.Players)
        {
            IBrush fill = CreateBorderFillBrush(player.ColorA, player.ColorB);
            IBrush outline = new SolidColorBrush(player.ColorA, opacity: 0.78);
            foreach (var tile in _game.Map.MapData.Where(i => i.Value.Owner == player))
            {
                var screenHex = ScreenHexFromWorld(tile.Key);
                DrawHexAt(ctx, screenHex, fill: fill, outline: outline, thickness: 4, dashed: true, dashes: [1, 0.3]);
            }
        }
    }

    public override void Render(DrawingContext ctx)
    {
        base.Render(ctx);

        DrawBoard(ctx);

        DrawBorders(ctx);

        if (_hoverScreenHex is Hex sh && _hoverWorldHex is Hex wh)
        {
            DrawHoveredHex(ctx, sh, 2.5, 0.4);
        }

        DrawCurrentPath(ctx);

        DrawUnits(ctx);

        DrawCities(ctx);

        DrawHexAt(ctx, new Hex(5, 0), TerrainBrush(Terrain.Plains), outline: Brushes.Black, thickness: 1);
    }

    private void DrawHexAt(
        DrawingContext ctx,
        Hex screenHex,
        IBrush? fill,
        IBrush? outline,
        double thickness = 1,
        bool dashed = false,
        IEnumerable<double>? dashes = null)
    {
        const int pointSize = 6;
        var (cx, cy) = HexLayout.HexToPixel(_game.Map.Canonical(screenHex), _sizeX, _sizeY);
        cx += _origin.X;
        cy += _origin.Y;

        Span<Point> pts = stackalloc Point[pointSize];
        for (int i = 0; i < pointSize; i++)
        {
            var (ox, oy) = HexLayout.CornerOffset(i, _sizeX, _sizeY);
            pts[i] = new Point(cx + ox, cy + oy);
        }

        var geo = new StreamGeometry();
        using (var gc = geo.Open())
        {
            gc.BeginFigure(pts[0], isFilled: true);
            for (int i = 1; i < pointSize; i++)
            {
                gc.LineTo(pts[i]);
            }
            gc.EndFigure(true);
        }

        Pen? pen = outline is not null ? new Pen(outline, thickness) : null;

        if (dashed && pen is not null)
        {
            pen.DashStyle = new DashStyle(dashes ?? [1, 2], 0);
        }

        ctx.DrawGeometry(fill, pen, geo);
    }
}
