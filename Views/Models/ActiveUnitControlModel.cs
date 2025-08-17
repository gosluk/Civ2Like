using Avalonia.Media;
using Civ2Like.Core.Units;
using Civ2Like.Hexagon;
using Civ2Like.View.Views.Events;
using Civ2Like.Views.Events;
using CommunityToolkit.Mvvm.Input;
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

    private uint? _movesLeft;
    public uint? MovesLeft
    {
        get => _movesLeft;
        set => SetProperty(ref _movesLeft, value);
    }

    private uint? _movesLeftEvaluated;
    public uint? MovesLeftEvaluated
    {
        get => _movesLeftEvaluated;
        set => SetProperty(ref _movesLeftEvaluated, value);
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

    private RelayCommand? _centerOnUnitCommand;
    public RelayCommand CenterOnUnitCommand
    {
        get
        {
            return _centerOnUnitCommand ??= new RelayCommand(CenterOnUnit);
        }
    }

    public bool IsAutoCenterOnUnit { get; set; }

    private uint _health;
    public uint Health
    {
        get => _health;
        set => SetProperty(ref _health, value);
    }

    private UnitState? _state;
    public UnitState? State
    {
        get => _state;
        set => SetProperty(ref _state, value);
    }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    private void CenterOnUnit()
    {
        if (UnitId is not null)
        {
            WeakReferenceMessenger.Default.Send(new CenterOnUnitEvent() { UnitId = UnitId.Value });
        }
    }

    private string? _unitTypeName;
    public string? UnitTypeName
    {
        get => _unitTypeName;
        set => SetProperty(ref _unitTypeName, value);
    }

    private uint? _maxHealth;
    public uint? MaxHealth
    {
        get => _maxHealth;
        set => SetProperty(ref _maxHealth, value);
    }

    private uint? _moveAllowance;
    public uint? MoveAllowance
    {
        get => _moveAllowance;
        set => SetProperty(ref _moveAllowance, value);
    }

    private uint? _tileVisibility;
    public uint? TileVisibility
    {
        get => _tileVisibility;
        set => SetProperty(ref _tileVisibility, value);
    }

    private uint? _attackRange;
    public uint? AttackRange
    {
        get => _attackRange;
        set => SetProperty(ref _attackRange, value);
    }

    private uint? _attackRanged;
    public uint? AttackRanged
    {
        get => _attackRanged;
        set => SetProperty(ref _attackRanged, value);
    }

    private uint? _attackMelee;
    public uint? AttackMelee
    {
        get => _attackMelee;
        set => SetProperty(ref _attackMelee, value);
    }

    private uint? _defenseRanged;
    public uint? DefenseRanged
    {
        get => _defenseRanged;
        set => SetProperty(ref _defenseRanged, value);
    }

    private uint? _defenseMelee;
    public uint? DefenseMelee
    {
        get => _defenseMelee;
        set => SetProperty(ref _defenseMelee, value);
    }

    private uint? _bonusMaxHealth;
    public uint? BonusMaxHealth
    {
        get => _bonusMaxHealth;
        set => SetProperty(ref _bonusMaxHealth, value);
    }

    private uint? _bonusMoveAllowance;
    public uint? BonusMoveAllowance
    {
        get => _bonusMoveAllowance;
        set => SetProperty(ref _bonusMoveAllowance, value);
    }

    private uint? _bonusTileVisibility;
    public uint? BonusTileVisibility
    {
        get => _bonusTileVisibility;
        set => SetProperty(ref _bonusTileVisibility, value);
    }

    private uint? _bonusAttackRange;
    public uint? BonusAttackRange
    {
        get => _bonusAttackRange;
        set => SetProperty(ref _bonusAttackRange, value);
    }

    private uint? _bonusAttackRanged;
    public uint? BonusAttackRanged
    {
        get => _bonusAttackRanged;
        set => SetProperty(ref _bonusAttackRanged, value);
    }

    private uint? _bonusAttackMelee;
    public uint? BonusAttackMelee
    {
        get => _bonusAttackMelee;
        set => SetProperty(ref _bonusAttackMelee, value);
    }

    private uint? _bonusDefenseRanged;
    public uint? BonusDefenseRanged
    {
        get => _bonusDefenseRanged;
        set => SetProperty(ref _bonusDefenseRanged, value);
    }

    private uint? _bonusDefenseMelee;
    public uint? BonusDefenseMelee
    {
        get => _bonusDefenseMelee;
        set => SetProperty(ref _bonusDefenseMelee, value);
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
        Health = message.Health;
        State = message.State;

        IsSelected = message.IsSelected;

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

        UnitTypeName = message.UnitType?.Name;
        MaxHealth = message.UnitType?.MaxHealth;
        MoveAllowance = message.UnitType?.MoveAllowance;
        TileVisibility = message.UnitType?.TileVisibility;
        AttackRange = message.UnitType?.AttackRange;
        AttackRanged = message.UnitType?.AttackRanged;
        AttackMelee = message.UnitType?.AttackMelee;
        DefenseRanged = message.UnitType?.DefenseRanged;
        DefenseMelee = message.UnitType?.DefenseMelee;

        BonusMaxHealth = message.Bonuses?.MaxHealth ?? 0;
        BonusMoveAllowance = message.Bonuses?.MoveAllowance ?? 0;
        BonusTileVisibility = message.Bonuses?.TileVisibility ?? 0;
        BonusAttackRange = message.Bonuses?.AttackRange ?? 0;
        BonusAttackRanged = message.Bonuses?.AttackRanged ?? 0;
        BonusAttackMelee = message.Bonuses?.AttackMelee ?? 0;
        BonusDefenseRanged = message.Bonuses?.DefenseRanged ?? 0;
        BonusDefenseMelee = message.Bonuses?.DefenseMelee ?? 0;

         MovesLeftEvaluated = message.UnitType?.MoveAllowance + (message.Bonuses?.MoveAllowance ?? 0);

        if (IsAutoCenterOnUnit && message.IsSelected)
        {
            CenterOnUnit();
        }
    }
}
