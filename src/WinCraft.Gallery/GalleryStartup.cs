using System.Windows;
using WinCraft.Gallery.Views;
using WinCraft.Startup;

namespace WinCraft.Gallery;

internal static class GalleryStartup
{
    public static void Run(string[] args)
    {
        WpfApplicationHost.Run(args, CreateApplication, CreateMainWindow);
    }

    private static Application CreateApplication()
    {
        var app = new App();
        app.InitializeComponent();
        return app;
    }

    private static Window CreateMainWindow() => new MainWindow();
}
