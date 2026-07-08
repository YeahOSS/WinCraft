using Microsoft.Win32;

namespace WinCraft.Infrastructure.RegistryAccess
{
    /// <summary>
    /// Reads registry data using read-only key handles.
    /// </summary>
    internal sealed class RegistryReader : IRegistryReader
    {
        public bool KeyExists(RegistryPath path)
        {
            using var key = OpenSubKey(path);
            return key != null;
        }

        public string[] GetSubKeyNames(RegistryPath path)
        {
            using var key = OpenSubKey(path);
            return key?.GetSubKeyNames() ?? [];
        }

        public string[] GetValueNames(RegistryPath path)
        {
            using var key = OpenSubKey(path);
            return key?.GetValueNames() ?? [];
        }

        public object GetValue(RegistryPath path, string valueName)
        {
            using var key = OpenSubKey(path);
            return key?.GetValue(valueName ?? string.Empty);
        }

        private static RegistryKey OpenSubKey(RegistryPath path)
        {
            if (string.IsNullOrEmpty(path.SubKeyPath))
                return RegistryPath.OpenBaseKey(path.Location);

            using var baseKey = RegistryPath.OpenBaseKey(path.Location);
            return baseKey.OpenSubKey(path.SubKeyPath, writable: false);
        }
    }
}
