using System;
using System.Windows;
using WinCraft.Infrastructure;

namespace WinCraft
{
    internal static class Program
    {
        /// <summary>
        /// Application entry point with single-instance enforcement.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            var app = new App();
            app.InitializeComponent();
            app.MainWindow = new MainWindow();

            var manager = new SingleInstanceApp(app);
            manager.StartupNextInstance += (sender, e) =>
            {
                var window = app.MainWindow;
                if (window != null)
                {
                    if (window.WindowState == WindowState.Minimized)
                        window.WindowState = WindowState.Normal;
                    window.Activate();
                }
            };
            manager.Run(args);
        }
    }
}
