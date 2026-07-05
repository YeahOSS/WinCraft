using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using WinCraft.Compatibility;

namespace WinCraft.Infrastructure.RegistryAccess
{
    /// <summary>
    /// Exports registry keys to .reg files for backup before destructive operations.
    /// Delegates to <c>regedit.exe /e</c> for reliable, standards-compliant output.
    /// </summary>
    internal static class RegistryExporter
    {
        /// <summary>
        /// Exports a registry key tree to a .reg file at the specified path.
        /// Returns true if the export succeeded, false if the key does not exist.
        /// </summary>
        public static bool ExportToFile(RegistryPath path, string filePath)
        {
            ThrowCompat.IfNull(path, nameof(path));
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException(Errors.NullOrEmpty);

            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "regedit.exe",
                    Arguments = "/e \"" + filePath + "\" \"" + path + "\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.WaitForExit();
            return process.ExitCode == 0 && File.Exists(filePath);
        }

        /// <summary>
        /// Generates a default backup file path for the given registry path.
        /// </summary>
        public static string GenerateBackupFilePath(RegistryPath path)
        {
            string safeName = path.ToString()
                .Replace('\\', '_')
                .Replace('/', '_')
                .Replace(':', '_');
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            return Path.Combine(ProductInfo.BackupDir, safeName + "_" + timestamp + ".reg");
        }
    }
}
