using System;
using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.Win32;

namespace WinCraft.Infrastructure.RegistryAccess
{
    /// <summary>
    /// Applies and removes deny ACL rules on registry keys, e.g. to protect
    /// ShellNew sort order from being reverted by Windows.
    /// </summary>
    internal static class RegistrySecurityHelper
    {
        public static bool DenyAccess(
            RegistryValueLocation location,
            string subKeyPath,
            string account,
            RegistryRights rights)
        {
            using var key = OpenForSecurity(location, subKeyPath);
            if (key == null)
                return false;

            var security = key.GetAccessControl();
            security.AddAccessRule(new RegistryAccessRule(
                account,
                rights,
                InheritanceFlags.None,
                PropagationFlags.None,
                AccessControlType.Deny));
            key.SetAccessControl(security);
            return true;
        }

        public static bool RemoveDenyAccess(
            RegistryValueLocation location,
            string subKeyPath,
            string account,
            RegistryRights rights)
        {
            using var key = OpenForSecurity(location, subKeyPath);
            if (key == null)
                return false;

            var security = key.GetAccessControl();
            foreach (RegistryAccessRule rule in security.GetAccessRules(
                true, true, typeof(NTAccount)))
            {
                if (rule.AccessControlType == AccessControlType.Deny
                    && rule.IdentityReference.Value.Equals(
                        account, StringComparison.OrdinalIgnoreCase)
                    && (rule.RegistryRights & rights) != 0)
                {
                    security.RemoveAccessRuleSpecific(rule);
                }
            }

            key.SetAccessControl(security);
            return true;
        }

        private static RegistryKey OpenForSecurity(
            RegistryValueLocation location, string subKeyPath)
        {
            var hive = location switch
            {
                RegistryValueLocation.CurrentUser => Registry.CurrentUser,
                RegistryValueLocation.LocalMachine => Registry.LocalMachine,
                _ => null
            };

            return hive?.OpenSubKey(subKeyPath,
                RegistryKeyPermissionCheck.ReadWriteSubTree,
                RegistryRights.ChangePermissions);
        }
    }
}
