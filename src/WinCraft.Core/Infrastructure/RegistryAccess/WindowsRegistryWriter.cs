using System;
using Microsoft.Win32;

namespace WinCraft.Infrastructure.RegistryAccess
{
    /// <summary>
    /// Writes registry values in the current process context.
    /// </summary>
    internal static class WindowsRegistryWriter
    {
        public static void WriteValue(RegistryValueWriteRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrEmpty(request.SubKeyPath))
                throw new ArgumentException("The registry subkey path is required.", nameof(request));

            using var baseKey = OpenBaseKey(request.Location);
            using var subKey = baseKey.CreateSubKey(request.SubKeyPath)
                ?? throw new InvalidOperationException("The registry subkey could not be created.");
            subKey.SetValue(request.ValueName ?? string.Empty, request.ValueData ?? string.Empty, request.ValueKind);
        }

        /// <summary>
        /// Deletes a registry value in the current process context.
        /// Returns silently when the target key or value does not exist.
        /// </summary>
        public static void DeleteValue(RegistryValueWriteRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrEmpty(request.SubKeyPath))
                throw new ArgumentException("The registry subkey path is required.", nameof(request));

            using var baseKey = OpenBaseKey(request.Location);
            using var subKey = baseKey.OpenSubKey(request.SubKeyPath, writable: true);
            if (subKey == null)
                return;

            var valueName = request.ValueName ?? string.Empty;
            if (subKey.GetValue(valueName) == null)
                return;

            subKey.DeleteValue(valueName, throwOnMissingValue: false);
        }

        private static RegistryKey OpenBaseKey(RegistryValueLocation location)
        {
            return location switch
            {
                RegistryValueLocation.CurrentUser => Registry.CurrentUser,
                RegistryValueLocation.LocalMachine => Registry.LocalMachine,
                RegistryValueLocation.ClassesRoot => Registry.ClassesRoot,
                _ => throw new ArgumentOutOfRangeException(nameof(location)),
            };
        }
    }
}
