using Avalonia.Input;
using Civ2Like.Config;
using Civ2Like.Core.Units;
using Civ2Like.Events.Items;
using Civ2Like.View.Views.Events;
using CommunityToolkit.Mvvm.Messaging;
using System.Threading.Tasks.Dataflow;

namespace Civ2Like.Views;

public sealed partial class GameView
{
    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        var p = e.GetPosition(this);
        var world = PixelToWorldHex(p);
        var screen = ScreenHexFromWorld(world);
        _hoverWorldHex = world;
        _hoverScreenHex = screen;

        _hoverNotifier.Post(string.Empty);
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
}
