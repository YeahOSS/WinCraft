using System;
using System.Collections.Generic;
using Microsoft.Win32;

namespace WinCraft.Infrastructure.RegistryAccess
{
    /// <summary>
    /// Represents a registry base hive and subkey path.
    /// </summary>
    internal sealed class RegistryPath(RegistryValueLocation location, string subKeyPath)
    {
        private const char KeySeparator = '\\';

        public RegistryValueLocation Location { get; } = location;

        public string SubKeyPath { get; } = subKeyPath ?? string.Empty;

        public static RegistryPath ClassesRoot(string subKeyPath)
        {
            return new RegistryPath(RegistryValueLocation.ClassesRoot, subKeyPath);
        }

        /// <summary>
        /// Appends a child segment to this registry path and returns a new <see cref="RegistryPath"/>.
        /// </summary>
        public RegistryPath Append(string childPath)
        {
            if (string.IsNullOrEmpty(SubKeyPath))
                return string.IsNullOrEmpty(childPath) ? this : new RegistryPath(Location, childPath);
            if (string.IsNullOrEmpty(childPath))
                return this;
            return new RegistryPath(Location, string.Concat(SubKeyPath, KeySeparator, childPath));
        }

        /// <summary>
        /// Returns the parent registry path, or a root-level path if already at the hive root.
        /// </summary>
        public RegistryPath GetParent()
        {
            int separatorIndex = SubKeyPath.LastIndexOf(KeySeparator);
            return separatorIndex < 0
                ? new RegistryPath(Location, string.Empty)
                : new RegistryPath(Location, SubKeyPath.Substring(0, separatorIndex));
        }

        /// <summary>
        /// Returns the leaf segment of this path.
        /// </summary>
        public string GetName()
        {
            int separatorIndex = SubKeyPath.LastIndexOf(KeySeparator);
            return separatorIndex < 0 ? SubKeyPath : SubKeyPath.Substring(separatorIndex + 1);
        }

        public override string ToString()
        {
            return string.Concat(GetHiveName(Location), KeySeparator, SubKeyPath);
        }

        public static bool TryParse(string fullPath, out RegistryPath path)
        {
            path = null;
            if (string.IsNullOrEmpty(fullPath))
                return false;

            string normalizedPath = fullPath.Trim(KeySeparator);
            int separatorIndex = normalizedPath.IndexOf(KeySeparator);
            string hiveName = separatorIndex < 0 ? normalizedPath : normalizedPath.Substring(0, separatorIndex);
            string subKeyPath = separatorIndex < 0 ? string.Empty : normalizedPath.Substring(separatorIndex + 1);

            if (!TryParseHiveName(hiveName, out RegistryValueLocation location))
                return false;

            path = new RegistryPath(location, subKeyPath);
            return true;
        }

        internal static RegistryKey OpenBaseKey(RegistryValueLocation location)
        {
            return location switch
            {
                RegistryValueLocation.CurrentUser => Registry.CurrentUser,
                RegistryValueLocation.LocalMachine => Registry.LocalMachine,
                RegistryValueLocation.ClassesRoot => Registry.ClassesRoot,
                _ => throw new ArgumentOutOfRangeException(nameof(location)),
            };
        }

        private static readonly Dictionary<string, RegistryValueLocation> HiveNameMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { "HKEY_CURRENT_USER", RegistryValueLocation.CurrentUser },
            { "HKCU", RegistryValueLocation.CurrentUser },
            { "HKEY_LOCAL_MACHINE", RegistryValueLocation.LocalMachine },
            { "HKLM", RegistryValueLocation.LocalMachine },
            { "HKEY_CLASSES_ROOT", RegistryValueLocation.ClassesRoot },
            { "HKCR", RegistryValueLocation.ClassesRoot },
        };

        private static bool TryParseHiveName(string hiveName, out RegistryValueLocation location)
        {
            return HiveNameMap.TryGetValue(hiveName, out location);
        }

        private static string GetHiveName(RegistryValueLocation location)
        {
            return location switch
            {
                RegistryValueLocation.CurrentUser => "HKEY_CURRENT_USER",
                RegistryValueLocation.LocalMachine => "HKEY_LOCAL_MACHINE",
                RegistryValueLocation.ClassesRoot => "HKEY_CLASSES_ROOT",
                _ => throw new ArgumentOutOfRangeException(nameof(location)),
            };
        }
    }
}
