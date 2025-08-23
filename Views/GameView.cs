using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
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

    private void RefreshTile(Hex? tile = null)
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
        {
            for (int c = 0; c < GameConfig.Width; c++)
            {
                var screenHex = _game.Map.FromColRow(c, r);
                var (x, y) = HexLayout.HexToPixel(screenHex, _sizeX, _sizeY);
                if (x < minX)
                {
                    minX = x;
                }

                if (y < minY)
                {
                    minY = y;
                }
            }
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
}
