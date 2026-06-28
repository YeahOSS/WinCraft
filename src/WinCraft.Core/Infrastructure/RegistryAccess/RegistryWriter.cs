using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Win32;
using WinCraft.Compatibility;

namespace WinCraft.Infrastructure.RegistryAccess
{
    /// <summary>
    /// Writes registry values in the current process context.
    /// </summary>
    internal static class RegistryWriter
    {
        public static void WriteValue(RegistryValueWriteRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrEmpty(request.SubKeyPath))
                throw new ArgumentException("The registry subkey path is required.", nameof(request));

            using var baseKey = RegistryPath.OpenBaseKey(request.Location);
            using var subKey = baseKey.CreateSubKey(request.SubKeyPath)
                ?? throw new InvalidOperationException("The registry subkey could not be created.");
            subKey.SetValue(request.ValueName ?? string.Empty, ConvertValueData(request), request.ValueKind);
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

            using var baseKey = RegistryPath.OpenBaseKey(request.Location);
            using var subKey = baseKey.OpenSubKey(request.SubKeyPath, writable: true);
            if (subKey == null)
                return;

            var valueName = request.ValueName ?? string.Empty;
            if (subKey.GetValue(valueName) == null)
                return;

            subKey.DeleteValue(valueName, throwOnMissingValue: false);
        }

        /// <summary>
        /// Deletes a registry key in the current process context.
        /// Returns silently when the target key does not exist.
        /// </summary>
        public static void DeleteKey(RegistryKeyOperationRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrEmpty(request.SourceSubKeyPath))
                throw new ArgumentException("The registry source subkey path is required.", nameof(request));

            var (parentPath, keyName) = SplitParentPath(request.SourceSubKeyPath);

            using var baseKey = RegistryPath.OpenBaseKey(request.Location);
            using var parentKey = string.IsNullOrEmpty(parentPath)
                ? baseKey
                : baseKey.OpenSubKey(parentPath, writable: true);
            if (parentKey == null)
                return;

            if (request.Recursive)
                RegistryKeyCompat.DeleteSubKeyTree(parentKey, keyName);
            else
                RegistryKeyCompat.DeleteSubKey(parentKey, keyName);
        }

        /// <summary>
        /// Moves a registry key in the current process context.
        /// </summary>
        public static void MoveKey(RegistryKeyOperationRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrEmpty(request.SourceSubKeyPath))
                throw new ArgumentException("The registry source subkey path is required.", nameof(request));
            if (string.IsNullOrEmpty(request.DestinationSubKeyPath))
                throw new ArgumentException("The registry destination subkey path is required.", nameof(request));

            using var baseKey = RegistryPath.OpenBaseKey(request.Location);
            using var sourceKey = baseKey.OpenSubKey(request.SourceSubKeyPath);
            if (sourceKey == null)
                return;

            using var destinationKey = baseKey.CreateSubKey(request.DestinationSubKeyPath)
                ?? throw new InvalidOperationException("The registry destination subkey could not be created.");

            try
            {
                CopyKey(sourceKey, destinationKey, overwrite: true, recursive: request.Recursive);
            }
            catch
            {
                // Roll back the partial destination key tree.
                var (destParent, destKeyName) = SplitParentPath(request.DestinationSubKeyPath);
                try
                {
                    using var destParentKey = string.IsNullOrEmpty(destParent)
                        ? baseKey
                        : baseKey.OpenSubKey(destParent, writable: true);
                    if (destParentKey != null)
                        RegistryKeyCompat.DeleteSubKeyTree(destParentKey, destKeyName);
                }
                catch
                {
                    // Best-effort cleanup; re-throw the original failure.
                }

                throw;
            }

            DeleteKey(new RegistryKeyOperationRequest
            {
                Location = request.Location,
                SourceSubKeyPath = request.SourceSubKeyPath,
                Recursive = request.Recursive
            });
        }

        private static void CopyKey(RegistryKey sourceKey, RegistryKey destinationKey, bool overwrite, bool recursive)
        {
            CopyValues(sourceKey, destinationKey, overwrite);

            if (!recursive)
                return;

            var pendingSources = new Stack<RegistryKey>();
            var pendingDests = new Stack<RegistryKey>();
            EnqueueSubKeys(sourceKey, destinationKey, pendingSources, pendingDests);

            while (pendingSources.Count > 0)
            {
                using var src = pendingSources.Pop();
                using var dest = pendingDests.Pop();
                CopyValues(src, dest, overwrite);
                EnqueueSubKeys(src, dest, pendingSources, pendingDests);
            }
        }

        private static void CopyValues(RegistryKey source, RegistryKey destination, bool overwrite)
        {
            foreach (string valueName in source.GetValueNames())
            {
                if (!overwrite && destination.GetValue(valueName) != null)
                    continue;
                destination.SetValue(valueName, source.GetValue(valueName), source.GetValueKind(valueName));
            }
        }

        private static void EnqueueSubKeys(RegistryKey source, RegistryKey destination, Stack<RegistryKey> sources, Stack<RegistryKey> dests)
        {
            foreach (string subKeyName in source.GetSubKeyNames())
            {
                var sourceSubKey = source.OpenSubKey(subKeyName);
                if (sourceSubKey == null)
                    continue;
                var destinationSubKey = destination.CreateSubKey(subKeyName)
                    ?? throw new InvalidOperationException("The registry destination subkey could not be created.");
                sources.Push(sourceSubKey);
                dests.Push(destinationSubKey);
            }
        }

        private static object ConvertValueData(RegistryValueWriteRequest request)
        {
            return request.ValueKind switch
            {
                RegistryValueKind.DWord => ParseDWord(request.ValueData, nameof(request)),
                RegistryValueKind.QWord => ParseQWord(request.ValueData, nameof(request)),
                RegistryValueKind.MultiString => ParseMultiString(request.ValueData),
                _ => request.ValueData ?? string.Empty,
            };

            static object ParseDWord(string valueData, string paramName)
            {
                if (long.TryParse(valueData, NumberStyles.Integer, CultureInfo.InvariantCulture, out long longValue)
                    && longValue >= int.MinValue && longValue <= int.MaxValue)
                    return (int)longValue;
                if (int.TryParse(valueData, out int intValue))
                    return intValue;
                throw new ArgumentException("The DWord value data is not a valid 32-bit integer.", paramName);
            }

            static object ParseQWord(string valueData, string paramName)
            {
                if (long.TryParse(valueData, out long qwordValue))
                    return qwordValue;
                throw new ArgumentException("The QWord value data is not a valid 64-bit integer.", paramName);
            }

            static object ParseMultiString(string valueData)
            {
                string data = valueData ?? string.Empty;
                return data.Length == 0
                    ? []
                    : data.Replace("\r\n", "\n").Split(['\n'], StringSplitOptions.None);
            }
        }

        private static (string ParentPath, string KeyName) SplitParentPath(string subKeyPath)
        {
            string trimmed = subKeyPath.TrimEnd('\\');
            int separatorIndex = trimmed.LastIndexOf('\\');
            if (separatorIndex < 0)
                return (string.Empty, trimmed);

            return (trimmed.Substring(0, separatorIndex), trimmed.Substring(separatorIndex + 1));
        }
    }
}
