using System;
using WinCraft.Compatibility;
using WinCraft.Infrastructure.RegistryAccess;

namespace WinCraft.Infrastructure.Shell
{
    /// <summary>
    /// Resolves CLSID GUIDs to display names, icons, and server paths
    /// from the HKCR\CLSID registry tree.
    /// </summary>
    internal sealed class ClsidResolver(IRegistryReader registryReader)
    {
        internal const string ClsidKeyName = "CLSID";
        internal const string Wow6432NodeClsidKeyName = @"WOW6432Node\CLSID";

        internal const string InprocServer32 = nameof(InprocServer32);
        internal const string LocalServer32 = nameof(LocalServer32);
        internal const string DefaultIcon = nameof(DefaultIcon);
        internal const string ServerExecutable = nameof(ServerExecutable);
        internal const string CodeBase = nameof(CodeBase);
        internal const string LocalizedString = nameof(LocalizedString);
        internal const string InfoTip = nameof(InfoTip);

        private static readonly string[] ClsidRoots = [ClsidKeyName, Wow6432NodeClsidKeyName];

        private readonly IRegistryReader _registryReader = ThrowCompat.IfNull(registryReader, nameof(registryReader));

        /// <summary>
        /// Resolves full metadata for a CLSID.  Returns null if the CLSID is
        /// not found or cannot be normalized.
        /// </summary>
        public ClsidInfo? Resolve(string clsid)
        {
            string normalized = Normalize(clsid);
            if (normalized == null)
                return null;

            if (!ClsidKeyExists(normalized))
                return null;

            return new ClsidInfo
            {
                Clsid = normalized,
                Text = ResolveDisplayName(normalized),
                Icon = ResolveIcon(normalized),
                FilePath = ResolveServerPath(normalized)
            };
        }

        /// <summary>
        /// Normalizes a raw CLSID string to the standard {GUID} format,
        /// or null if the value is not a valid GUID.
        /// </summary>
        public static string Normalize(string value)
        {
            if (string.IsNullOrEmpty(value))
                return null;

            string trimmed = value.Trim();
            if (trimmed.Length == 39
                && trimmed.StartsWith("{", StringComparison.Ordinal)
                && trimmed.EndsWith("}-", StringComparison.Ordinal))
            {
                trimmed = trimmed.Substring(0, 38);
            }

            return GuidCompat.TryParse(trimmed, out Guid clsid) ? clsid.ToString("B") : null;
        }

        private bool ClsidKeyExists(string clsid)
        {
            return Array.Exists(
                ClsidRoots,
                clsidRoot => _registryReader.KeyExists(RegistryPath.FromClassesRoot(clsidRoot).Combine(clsid)));
        }

        private string ResolveDisplayName(string clsid)
        {
            if (string.IsNullOrEmpty(clsid))
                return null;

            var clsidPath = RegistryPath.FromClassesRoot(ClsidKeyName).Combine(clsid);
            return StringResolver.Resolve(GetString(clsidPath, LocalizedString))
                ?? GetString(clsidPath, InfoTip)
                ?? GetString(clsidPath, null);
        }

        private IconLocation ResolveIcon(string clsid)
        {
            if (string.IsNullOrEmpty(clsid))
                return default;

            string value = GetString(
                RegistryPath.FromClassesRoot(ClsidKeyName).Combine(clsid).Combine(DefaultIcon),
                null);
            return !string.IsNullOrEmpty(value) ? new IconLocation(value) : new IconLocation(string.Empty);
        }

        private string ResolveServerPath(string clsid)
        {
            if (string.IsNullOrEmpty(clsid))
                return null;

            foreach (string clsidRoot in ClsidRoots)
            {
                var clsidPath = RegistryPath.FromClassesRoot(clsidRoot).Combine(clsid);
                string inprocServer = GetString(clsidPath.Combine(InprocServer32), null);
                if (!string.IsNullOrEmpty(inprocServer))
                    return ResolveInprocPath(clsidPath, inprocServer);

                string localServer = GetString(clsidPath.Combine(LocalServer32), ServerExecutable);
                if (!string.IsNullOrEmpty(localServer))
                    return localServer;

                localServer = GetString(clsidPath.Combine(LocalServer32), null);
                if (!string.IsNullOrEmpty(localServer))
                    return CommandLineParser.ResolveExecutablePath(localServer) ?? localServer;
            }

            return null;
        }

        private string ResolveInprocPath(RegistryPath clsidPath, string inprocServer)
        {
            if (inprocServer.EndsWith("mscoree.dll", StringComparison.OrdinalIgnoreCase))
            {
                string codeBase = GetString(clsidPath.Combine(InprocServer32), CodeBase);
                if (!string.IsNullOrEmpty(codeBase) && Uri.TryCreate(codeBase, UriKind.Absolute, out Uri uri) && uri.IsFile)
                    return uri.LocalPath;
            }

            return inprocServer;
        }

        private string GetString(RegistryPath path, string valueName)
        {
            return _registryReader.GetValue(path, valueName)?.ToString();
        }
    }
}
