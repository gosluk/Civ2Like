using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Civ2Like.Views;

public partial class PlayerFlag : UserControl
{
    public static readonly DirectProperty<PlayerFlag, IImmutableSolidColorBrush> PrimaryBrushProperty =
        AvaloniaProperty.RegisterDirect<PlayerFlag, IImmutableSolidColorBrush>(
            nameof(PrimaryBrush),
            o => o.PrimaryBrush,
            (o, v) => o.PrimaryBrush = v);

    public static readonly DirectProperty<PlayerFlag, IImmutableSolidColorBrush> SecondaryBrushProperty =
        AvaloniaProperty.RegisterDirect<PlayerFlag, IImmutableSolidColorBrush>(
            nameof(SecondaryBrush),
            o => o.SecondaryBrush,
            (o, v) => o.SecondaryBrush = v);

    private IImmutableSolidColorBrush _primaryBrush = Brushes.White;
    public IImmutableSolidColorBrush PrimaryBrush
    {
        get => _primaryBrush;
        set => SetAndRaise(PrimaryBrushProperty, ref _primaryBrush, value);
    }

    private IImmutableSolidColorBrush _secondaryBrush = Brushes.Red;
    public IImmutableSolidColorBrush SecondaryBrush
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