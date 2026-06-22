using System.Windows;
using Microsoft.VisualBasic.ApplicationServices;

namespace WinCraft.Infrastructure
{
    /// <summary>
    /// Ensures only one instance of the application runs at a time,
    /// using the WPF dispatcher loop for the first instance.
    /// </summary>
    internal sealed class SingleInstanceHost : WindowsFormsApplicationBase
    {
        private readonly Application _app;

        public SingleInstanceHost(Application app)
        {
            IsSingleInstance = true;
            _app = app;
        }

        protected override void OnRun()
        {
            _app.Run(_app.MainWindow);
        }
    }
}
