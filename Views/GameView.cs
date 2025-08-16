using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.Immutable;
using Avalonia.Media.TextFormatting;
using Avalonia.Platform;
using Avalonia.Threading;
using Civ2Like.Config;
using Civ2Like.Core;
using Civ2Like.Core.Units;
using Civ2Like.Events.Items;
using Civ2Like.Hexagon;
using Civ2Like.View.Views.Events;
using Civ2Like.Views.Events;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.Immutable;
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
    private readonly ITargetBlock<string> _hoverNotifier;
    private IReadOnlyList<Hex>? _currentPath;
    private Point _origin;

    private int _viewColOffset = 0;
    private int _viewRowOffset = 0;

    private IImage _unitIcon;
    private IImage _cityIcon;

    public GameView()
    {
        _game = new Game(GameConfig.Width, GameConfig.Height, GameConfig.Seed);
        Focusable = true;

        PointerMoved += OnPointerMoved;
        PointerPressed += OnPointerPressed;
        KeyDown += OnKeyDown;

        _origin = ComputeOrigin();

        _hoverNotifier = new ActionBlock<string>(async msg =>
            {
                await Task.Delay(300);
                await Dispatcher.UIThread.InvokeAsync(InvalidateVisual);
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

    public void Dispose()
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }

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

    private static int Mod(int a, int m) { int r = a % m; return r < 0 ? r + m : r; }

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

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        var p = e.GetPosition(this);
        var world = PixelToWorldHex(p);
        var screen = ScreenHexFromWorld(world);
        _hoverWorldHex = world;
        _hoverScreenHex = screen;

        _hoverNotifier.Post(string.Empty);
    }

    private void SelectUnit(Unit? unit)
    {
        _game.SelectedUnit = unit;

        if (unit is null)
        {
            WeakReferenceMessenger.Default.Send(new UnitSelectionChangedEvent());
        }
        else
        {
            _currentPath = null;
            WeakReferenceMessenger.Default.Send(new UnitSelectionChangedEvent(unit, unit.Player, _unitIcon));
        }
    }

    private void SelectPlayer(Player player)
    {
        WeakReferenceMessenger.Default.Send(new PlayerSelectionChangedEvent(_game, player));
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var p = e.GetPosition(this);
        var world = PixelToWorldHex(p);

        var click = e.GetCurrentPoint(this).Properties;

        if (click.IsLeftButtonPressed)
        {
            if (_game.SelectedUnit is null)
            {
                var selectedUnit = _game.TrySelectUnitAt(world, true);
                SelectUnit(selectedUnit);
            }
            else
            {
                void EvaluatePath() => _currentPath = _game.FindPath(_game.SelectedUnit.Pos, world);

                if (_currentPath is null)
                {
                    EvaluatePath();
                }
                else
                {
                    if (_currentPath.Count > 1 && _currentPath[^1] == world)
                    {
                        _game.FollowPath(_currentPath);
                        WeakReferenceMessenger.Default.Send(new UnitSelectionChangedEvent(_game.SelectedUnit!, _game.SelectedUnit!.Player, _unitIcon));
                        _currentPath = null;
                    }
                    else
                    {
                        EvaluatePath();
                    }
                }
            }
        }

        SelectPlayer(_game.ActivePlayer);

        InvalidateVisual();
        Focus();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Left: _viewColOffset = Mod(_viewColOffset - 1, GameConfig.Width); break;
            case Key.Right: _viewColOffset = Mod(_viewColOffset + 1, GameConfig.Width); break;
            case Key.Up: _viewRowOffset = Mod(_viewRowOffset - 2, GameConfig.Height); break;
            case Key.Down: _viewRowOffset = Mod(_viewRowOffset + 2, GameConfig.Height); break;

            case Key.S:
                //File.WriteAllText("events.json", _game.Events.ToJson());
                break;

            case Key.L:
                if (File.Exists("events.json"))
                {
                    var json = File.ReadAllText("events.json");
                    //_game.Events.LoadFromJson(json);
                    //_game.Events.Replay(_game, _game.Events.Log);
                }
                break;

            case Key.F:
                if (_game.SelectedUnit is not null)
                {
                    _game.ProcessEvent(new UnitStateChangedEvent() { UnitId = _game.SelectedUnit.Id, NewState = UnitState.Fortified });
                    WeakReferenceMessenger.Default.Send(new UnitSelectionChangedEvent(_game.SelectedUnit, _game.SelectedUnit.Player, _unitIcon));
                }
                break;

            case Key.C:
                _game.TryFoundCity();
                break;
            case Key.Space:
                _game.EndTurn();
                SelectUnit(_game.FindNextUnitToMove());
                SelectPlayer(_game.ActivePlayer);
                break;
            case Key.N:
                SelectUnit(_game.FindNextUnitToMove());
                break;
            case Key.M:
                if (_currentPath is not null && _game.SelectedUnit is not null)
                {
                    _game.FollowPath(_currentPath);
                }
                break;
        }

        SelectPlayer(_game.ActivePlayer);
        InvalidateVisual();
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
