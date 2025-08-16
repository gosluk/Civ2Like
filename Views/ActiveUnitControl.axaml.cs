using Avalonia.Controls;
using Civ2Like.Views.Models;

namespace Civ2Like.Views;

public partial class ActiveUnitControl : UserControl
{
    public ActiveUnitControl()
    {
        InitializeComponent();
        DataContext = new ActiveUnitControlModel();
    }
}