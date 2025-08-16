using Avalonia.Media;
using Civ2Like.Hexagon;
using Civ2Like.View;
using Civ2Like.View.Views.Events;
using CommunityToolkit.Mvvm.Messaging;
using System.ComponentModel;
using System.Runtime.CompilerServices;

internal class ActiveUnitControlModel
    : IRecipient<UnitSelectionChangedEvent>, INotifyPropertyChanged, IDisposable
{
    public ActiveUnitControlModel()
    {
        WeakReferenceMessenger.Default.Register(this);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(name);
        return true;
    }

    private Guid? _unitId;
    public Guid? UnitId
    {
        get => _unitId;
        set => SetProperty(ref _unitId, value);
    }

    private Guid? _playerId;
    public Guid? PlayerId
    {
        get => _playerId;
        set => SetProperty(ref _playerId, value);
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
        PlayerId = message.Player!.Id;
        Pos = message.Pos;
        MovesLeft = message.MovesLeft;
        Icon = message.Icon;

        ColorA = new SolidColorBrush(message.Player.ColorA);
        ColorB = new SolidColorBrush(message.Player.ColorB);
    }

    public void Dispose()
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }
}
