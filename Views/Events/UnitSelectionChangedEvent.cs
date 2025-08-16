using Avalonia.Media;
using Civ2Like.Core;
using Civ2Like.Hexagon;

namespace Civ2Like.View.Views.Events;

public sealed class UnitSelectionChangedEvent
{
    public UnitSelectionChangedEvent()
    {
        IsSelected = false;
    }

    public UnitSelectionChangedEvent(Unit unit, IImage icon)
    {
        UnitId = unit.Id;
        PlayerId = unit.Owner.Id;
        Pos = unit.Pos;
        MovesLeft = unit.MovesLeft;
        Icon = icon;
        IsSelected = true;
    }

    public Guid? UnitId { get; }

    public Guid? PlayerId { get; }

    public Hex? Pos { get; }

    public int? MovesLeft { get; }

    public IImage? Icon { get; }

    public bool IsSelected { get; }
}
