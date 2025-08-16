using Avalonia.Controls;
using Civ2Like.Views.Models;

namespace Civ2Like.Views;

public partial class ActivePlayerControl : UserControl
{
    public ActivePlayerControl()
    {
        InitializeComponent();
        DataContext = new ActivePlayerControlModel();
    }
}