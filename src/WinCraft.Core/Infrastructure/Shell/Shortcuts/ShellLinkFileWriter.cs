using System;
using WinCraft.Infrastructure.Ipc;
using WinCraft.Infrastructure.Security;

namespace WinCraft.Infrastructure.Shell.Shortcuts
{
    /// <summary>
    /// Saves a <see cref="ShellLink"/> to disk.
    /// Call <see cref="SaveWithElevation"/> when the target path requires Administrator privileges.
    /// </summary>
    internal static class ShellLinkFileWriter
    {
        /// <summary>
        /// Saves the shortcut to the specified path without elevation.
        /// </summary>
        public static void Save(ShellLink link, string lnkPath)
        {
            link.SaveAs(lnkPath);
        }

        /// <summary>
        /// Saves the shortcut through the elevated agent at <see cref="PrivilegeLevel.Administrator"/>.
        /// </summary>
        public static void SaveWithElevation(ShellLink link, string lnkPath, IPrivilegeBroker broker)
        {
            if (broker == null)
                throw new ArgumentNullException(nameof(broker));

            var request = new ElevatedCommandRequest
            {
                OperationName = ElevatedOperations.FileWrite,
                Payload = DataContractPayloadSerializer.Serialize(
                    new FileWriteRequest { Path = lnkPath, Content = link.SaveToBytes() }),
                PrivilegeLevel = PrivilegeLevel.Administrator,
            };

            var result = broker.Execute(request);
            if (!result.Succeeded)
                throw new UnauthorizedAccessException(
                    string.Format(
                        "Failed to save '{0}' through the elevated agent. Error: {1}",
                        lnkPath, result.ErrorMessage));
        }
    }
}
