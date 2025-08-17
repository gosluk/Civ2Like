using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Civ2Like.Views;

public partial class PlayerFlag : UserControl
{
    public static readonly DirectProperty<PlayerFlag, IBrush?> PrimaryBrushProperty =
        AvaloniaProperty.RegisterDirect<PlayerFlag, IBrush?>(
            nameof(PrimaryBrush),
            o => o.PrimaryBrush,
            (o, v) => o.PrimaryBrush = v);

    public static readonly DirectProperty<PlayerFlag, IBrush?> SecondaryBrushProperty =
        AvaloniaProperty.RegisterDirect<PlayerFlag, IBrush?>(
            nameof(SecondaryBrush),
            o => o.SecondaryBrush,
            (o, v) => o.SecondaryBrush = v);

    private IBrush? _primaryBrush = Brushes.DodgerBlue;
    public IBrush? PrimaryBrush
    {
        get => _primaryBrush;
        set => SetAndRaise(PrimaryBrushProperty, ref _primaryBrush, value);
    }

    private IBrush? _secondaryBrush = Brushes.White;
    public IBrush? SecondaryBrush
    {
        get => _secondaryBrush;
        set => SetAndRaise(SecondaryBrushProperty, ref _secondaryBrush, value);
    }

    static PlayerFlag()
    {
        AffectsRender<PlayerFlag>(PrimaryBrushProperty, SecondaryBrushProperty);
    }

    public PlayerFlag()
    {
        InitializeComponent();
    }
}
