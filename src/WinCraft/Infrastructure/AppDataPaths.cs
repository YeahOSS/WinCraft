using System;
using System.IO;
using WinCraft.Compatibility;

namespace WinCraft.Infrastructure
{
    /// <summary>
    /// Provides well-known application data paths under <c>%LocalAppData%\WinCraft\</c>.
    /// </summary>
    public static class AppDataPaths
    {
        /// <summary>
        /// Application data root directory.  Falls back to the application
        /// base directory when <c>LocalApplicationData</c> is unavailable
        /// (e.g., certain service-account or Server Core configurations).
        /// </summary>
        public static readonly string Root = ResolveRoot();

        private static string ResolveRoot()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrEmpty(appData))
            {
                appData = AppDomain.CurrentDomain.BaseDirectory;
            }

            return PathCompat.Combine(appData, nameof(WinCraft));
        }

        /// <summary>
        /// Log file output directory.
        /// </summary>
        public static readonly string Logs = Path.Combine(Root, "Logs");
    }
}
