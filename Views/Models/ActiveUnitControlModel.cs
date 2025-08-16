using Avalonia.Media;
using Civ2Like.Hexagon;
using Civ2Like.View.Views.Events;
using CommunityToolkit.Mvvm.Messaging;

namespace Civ2Like.Views.Models;

internal class ActiveUnitControlModel
    : BaseModel, IRecipient<UnitSelectionChangedEvent>
{
    public ActiveUnitControlModel()
    {
        WeakReferenceMessenger.Default.Register(this);
    }

    private Guid? _unitId;
    public Guid? UnitId
    {
        get => _unitId;
        set => SetProperty(ref _unitId, value);
    }

    private string? _unitName;
    public string? UnitName
    {
        get => _unitName;
        set => SetProperty(ref _unitName, value);
    }   

    private Guid? _playerId;
    public Guid? PlayerId
    {
        get => _playerId;
        set => SetProperty(ref _playerId, value);
    }

    private string? _playerName;
    public string? PlayerName
    {
        get => _playerName;
        set => SetProperty(ref _playerName, value);
    }

    private Hex? _pos;
    public Hex? Pos
    {
        get => _pos;
        set => SetProperty(ref _pos, value);
    }

    private int? _movesLeft;
    public int? MovesLeft
    {
        get => _movesLeft;
        set => SetProperty(ref _movesLeft, value);
    }

    private IImage? _icon;
    public IImage? Icon
    {
        get => _icon;
        set => SetProperty(ref _icon, value);
    }

    private SolidColorBrush? _colorA;
    public SolidColorBrush? ColorA
    {
        get => _colorA;
        set => SetProperty(ref _colorA, value);
    }

    private SolidColorBrush? _colorB;
    public SolidColorBrush? ColorB
    {
        get => _colorB;
        set => SetProperty(ref _colorB, value);
    }

    public void Receive(UnitSelectionChangedEvent message)
    {
        UnitId = message.UnitId;
        UnitName = message.UnitName;
        PlayerId = message.Player?.Id ?? Guid.Empty;
        PlayerName = message.Player?.Name ?? string.Empty;
        Pos = message.Pos;
        MovesLeft = message.MovesLeft;
        Icon = message.Icon;

        if (message.IsSelected)
        {
            ColorA = new SolidColorBrush(message.Player!.ColorA);
            ColorB = new SolidColorBrush(message.Player!.ColorB);
        }
        else
        {
            ColorA = new SolidColorBrush(Colors.Black);
            ColorB = ColorA;
        }
    }
}
