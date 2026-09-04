using System;
using System.Windows;
using System.Windows.Controls;
using WinCraft.UI;

namespace WinCraft.Startup
{
    internal static class UserInterfaceStartup
    {
        public static void Run(string[] args)
        {
            WpfApplicationHost.Run(
                args,
                createApplication: CreateApplication,
                createMainWindow: CreateMainWindow,
                onReady: SetupTrayIcon);
        }

        private static Application CreateApplication()
        {
            var app = new App();
            app.InitializeComponent();
            return app;
        }

        private static Window CreateMainWindow() => new MainWindow();

        private static void SetupTrayIcon(Application app, Window mainWindow)
        {
            var trayIcon = new TrayIcon { ToolTipText = mainWindow.Title };
            trayIcon.ContextMenu = CreateTrayIconContextMenu(app, mainWindow);
            trayIcon.Click += (_, _) => ActivateMainWindow(mainWindow);

            app.Exit += (_, _) => trayIcon.Dispose();
        }

        private static void ActivateMainWindow(Window window)
        {
            if (window == null)
                return;

            if (!window.IsVisible)
                window.Show();

            if (window.WindowState == WindowState.Minimized)
                window.WindowState = WindowState.Normal;
            window.Activate();
        }

        private static ContextMenu CreateTrayIconContextMenu(Application app, Window mainWindow)
        {
            var menu = new ContextMenu();

            var showItem = new MenuItem { Header = "Show WinCraft" };
            showItem.Click += (_, _) => ActivateMainWindow(mainWindow);
            menu.Items.Add(showItem);

            menu.Items.Add(new Separator());

            var exitItem = new MenuItem { Header = "Exit" };
            exitItem.Click += (_, _) => app.Shutdown();
            menu.Items.Add(exitItem);
            return menu;
        }
    }
}
