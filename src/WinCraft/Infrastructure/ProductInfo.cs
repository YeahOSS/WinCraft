using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace WinCraft.Infrastructure
{
    /// <summary>
    /// Product identity (name, version, publisher) and well-known
    /// application data paths under <c>%LocalAppData%\</c>.
    /// </summary>
    public static class ProductInfo
    {
        public static readonly string ExecutablePath = Assembly.GetEntryAssembly().Location;

        public static readonly string StartupPath = AppDomain.CurrentDomain.BaseDirectory;

        private static readonly FileVersionInfo VersionInfo = FileVersionInfo.GetVersionInfo(ExecutablePath);

        public static string ProductName => VersionInfo.ProductName;

        public static string Publisher => VersionInfo.CompanyName;

        public static readonly Version ProductVersion = new(VersionInfo.ProductVersion);

        /// <summary>
        /// Application data root directory.  Falls back to the application
        /// base directory when <c>LocalApplicationData</c> is unavailable
        /// (e.g., certain service-account or Server Core configurations).
        /// </summary>
        public static readonly string AppDataDir = ResolveAppDataDir();

        /// <summary>
        /// Log file output directory.
        /// </summary>
        public static readonly string LogsDir = Path.Combine(AppDataDir, "Logs");

        /// <summary>
        /// Crash dump output directory.
        /// </summary>
        public static readonly string DumpsDir = Path.Combine(AppDataDir, "Dumps");

        /// <summary>
        /// Registry backup (.reg) files saved before destructive operations.
        /// </summary>
        public static readonly string BackupDir = Path.Combine(AppDataDir, "Backup");

        private static string ResolveAppDataDir()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrEmpty(appData))
            {
                appData = StartupPath;
            }

            return Path.Combine(appData, ProductName);
        }
    }
}
