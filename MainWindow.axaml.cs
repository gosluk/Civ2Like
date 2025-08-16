using Avalonia.Controls;

namespace Civ2Like;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        GameView.Focus();
    }
}
