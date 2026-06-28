using System;
using WinCraft.Startup;
using WinCraft.Views;

namespace WinCraft
{
    internal static class Program
    {
#if !DEBUG && !INSTALLER

        static Program()
        {
            Overlay.AssemblyResolver.Register();
        }
#endif

        [STAThread]
        private static void Main(string[] args)
        {
            ProgramHost.Run(
                args,
                () => new App(),
                app => ((App)app).InitializeComponent(),
                () => new MainWindow());
        }
    }
}
