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

        _attackHoverUnit = null;
        if (_attackMode)
        {
            var unitToAttack = GetUnitAt(world);

            if (unitToAttack is not null && _game.SelectedUnit is not null && unitToAttack.Player != _game.SelectedUnit.Player)
            {
                _attackHoverUnit = unitToAttack;
            }
        }

        _hoverNotifier.Post(string.Empty);
    }
    
    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var p = e.GetPosition(this);
        var world = PixelToWorldHex(p);

        var click = e.GetCurrentPoint(this).Properties;

        if (click.IsLeftButtonPressed)
        {
            /// If in attack mode the assumption is the unit is already selected
            /// When no attacking checking if we are selecting a unit
            if (_attackMode)
            {
                var attacker = _game.SelectedUnit;
                var defender = GetUnitAt(world);

                if (attacker is not null && defender is not null && defender.Player != attacker.Player)
                {
                    var evt = new UnitAttacksUnitEvent()
                    {
                        AttackerUnitId = attacker.Id,
                        DefenderUnitId = defender.Id,
                        IsRanged = _attackRanged
                    };

                    _game.ProcessEvent(evt);   // Apply via event pipeline
                    _attackMode = false;                // exit attack mode after a shot/swing
                    _currentPath = null;                // clear any path preview
                }
                // clicked empty tile or friendly: just exit attack mode
                _attackMode = false;
            }
            else if (_game.SelectedUnit is null)
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

            SelectPlayer(_game.ActivePlayer);
        }
        else if (click.IsRightButtonPressed)
        {
            // Right click: exit attack mode, clear path, deselect unit
            _attackMode = false;
            SelectUnit(null);

            Cursor = null;
        }

        InvalidateVisual();
        Focus();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            // -- View navigation keys
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

            case Key.C: _game.TryFoundCity(); break;

            // -- Movement & Unit keys
            case Key.F:
                if (_game.SelectedUnit is not null)
                {
                    _game.ProcessEvent(new UnitStateChangedEvent() { UnitId = _game.SelectedUnit.Id, NewState = UnitState.Fortified });
                    WeakReferenceMessenger.Default.Send(new UnitSelectionChangedEvent(_game.SelectedUnit, _game.SelectedUnit.Player, _unitIcon));
                }
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

            // -- Attack mode keys
            case Key.A: // melee attack
                if (_game.SelectedUnit is not null)
                {
                    _attackMode = true;
                    _attackRanged = false;
                    _attackHoverUnit = null;
                    Cursor = new Cursor(StandardCursorType.Hand);
                }
                break;

            case Key.R: // ranged attack
                if (_game.SelectedUnit is not null && _game.SelectedUnit.EffectiveStats().AttackRange > 0)
                {
                    _attackMode = true;
                    _attackRanged = true;
                    _attackHoverUnit = null;
                    Cursor = new Cursor(StandardCursorType.Hand);
                }
                break;

            case Key.Escape:
                if (_attackMode)
                {
                    _attackMode = false;
                    _attackHoverUnit = null;
                    Cursor = null;
                }
                break;
        }

        SelectPlayer(_game.ActivePlayer);
        InvalidateVisual();
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
}
