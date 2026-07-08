using System;
using Microsoft.Win32;

namespace WinCraft.Compatibility
{
    /// <summary>
    /// Provides <see cref="RegistryKey"/> helpers that bridge gaps between framework variants.
    /// </summary>
    public static class RegistryKeyCompat
    {
        /// <summary>
        /// Deletes a subkey and any children recursively, returning silently when the
        /// target subkey does not exist. net30 lacks the
        /// <see cref="RegistryKey.DeleteSubKeyTree(string, bool)"/> overload.
        /// </summary>
        public static void DeleteSubKeyTree(RegistryKey parentKey, string keyName)
        {
#if NET45
            parentKey.DeleteSubKeyTree(keyName, throwOnMissingSubKey: false);
#else
            try
            {
                parentKey.DeleteSubKeyTree(keyName);
            }
            catch (ArgumentException)
            {
                // The key does not exist — intentionally ignored.
            }
#endif
        }

        /// <summary>
        /// Deletes a subkey, returning silently when the target subkey does not exist.
        /// net30 lacks the <see cref="RegistryKey.DeleteSubKey(string, bool)"/> overload.
        /// </summary>
        public static void DeleteSubKey(RegistryKey parentKey, string keyName)
        {
#if NET45
            parentKey.DeleteSubKey(keyName, throwOnMissingSubKey: false);
#else
            try
            {
                parentKey.DeleteSubKey(keyName);
            }
            catch (ArgumentException)
            {
                // The key does not exist — intentionally ignored.
            }
#endif
        }
    }
}
