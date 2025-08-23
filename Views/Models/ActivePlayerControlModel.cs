using Avalonia.Media;
using Civ2Like.Views.Events;
using CommunityToolkit.Mvvm.Messaging;

namespace Civ2Like.Views.Models;
internal class ActivePlayerControlModel
    : BaseModel, IRecipient<PlayerSelectionChangedEvent>
{
    public ActivePlayerControlModel()
    {
        WeakReferenceMessenger.Default.Register(this);
    }

    private string? _playerName;
    public string? PlayerName
    {
        get => _playerName;
        set => SetProperty(ref _playerName, value);
    }

    private long _gold;
    public long Gold
    {
        get => _gold;
        set => SetProperty(ref _gold, value);
    }

    private uint _numberOfUnits;
    public uint NumberOfUnits
    {
        get => _numberOfUnits;
        set => SetProperty(ref _numberOfUnits, value);
    }

    private uint _numberOfCities;
    public uint NumberOfCities
    {
        get => _numberOfCities;
        set => SetProperty(ref _numberOfCities, value);
    }

    private uint _turn;
    public uint Turn
    {
        get => _turn;
        set => SetProperty(ref _turn, value);
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

    private string? _nationName;
    public string? NationName
    {
        get => _nationName;
        set => SetProperty(ref _nationName, value);
    }

    public void Receive(PlayerSelectionChangedEvent message)
    {
        PlayerName = message.Player.Name;
        Gold = (long)message.Player.Gold;
        NumberOfUnits = message.NumberOfUnits;
        NumberOfCities = message.NumberOfCities;
        Turn = message.Turn;
        ColorA = new SolidColorBrush(message.Player!.ColorA);
        ColorB = new SolidColorBrush(message.Player!.ColorB);
        NationName = message.Player?.Founder.Name;
    }
}
