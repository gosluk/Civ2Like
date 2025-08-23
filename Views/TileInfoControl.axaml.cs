using Avalonia.Controls;
using Civ2Like.Views.Models;

namespace Civ2Like.Views;

public partial class TileInfoControl : UserControl
{
    public TileInfoControl()
    {
        InitializeComponent();
        this.DataContext = new TileInfoModel();
    }
}