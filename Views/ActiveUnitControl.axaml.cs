using Avalonia.Controls;

namespace Civ2Like.View;

public partial class ActiveUnitControl : UserControl
{
    public ActiveUnitControl()
    {
        InitializeComponent();
        this.DataContext = new ActiveUnitControlModel();
    }
}