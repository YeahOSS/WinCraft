using System.Windows;
using System.Windows.Controls;

namespace WinCraft.Gallery.Views;

public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void ThemeButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.ContextMenu is ContextMenu menu)
        {
            menu.PlacementTarget = element;
            menu.IsOpen = true;
        }
    }
}
