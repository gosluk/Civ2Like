using Avalonia.Media;
using Civ2Like.Core;
using Civ2Like.Core.Units;
using Civ2Like.Events;
using Civ2Like.Hexagon;

namespace Civ2Like.View.Views.Events;

public sealed class UnitSelectionChangedEvent
{
    public UnitSelectionChangedEvent()
    {
        IsSelected = false;
    }

    public UnitSelectionChangedEvent(Unit unit, Player player, IImage icon)
    {
        UnitId = unit.Id;
        UnitName = unit.Name;
        Player = player;
        Pos = unit.Pos;
        MovesLeft = unit.MovesLeft;
        Icon = icon;
        IsSelected = true;
        State = unit.State;
        Health = unit.Health;
    }

    public Guid? UnitId { get; }

    public string? UnitName { get; }

    public Player? Player { get; }

    public Hex? Pos { get; }

    public int? MovesLeft { get; }

    public IImage? Icon { get; }

    public UnitState? State { get; }

    public uint Health { get; }

    public bool IsSelected { get; }
}
