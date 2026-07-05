using Microsoft.Win32;
using WinCraft.Infrastructure.Ipc;
using WinCraft.Infrastructure.Security;

namespace WinCraft.Infrastructure.RegistryAccess
{
    /// <summary>
    /// Shared registry write primitives used by shell registration editor classes.
    /// </summary>
    internal sealed class RegistryEditor(IPrivilegeBroker privilegeBroker)
    {
        private readonly IPrivilegeBroker _privilegeBroker = privilegeBroker;

        public PrivilegeExecutionResult WriteValue(
            RegistryPath path,
            string valueName,
            string valueData,
            RegistryValueKind valueKind)
        {
            var request = new RegistryValueWriteRequest
            {
                Location = path.Location,
                SubKeyPath = path.SubKeyPath,
                ValueName = valueName,
                ValueData = valueData,
                ValueKind = valueKind
            };

            if (path.Location == RegistryValueLocation.CurrentUser)
            {
                RegistryWriter.WriteValue(request);
                return PrivilegeExecutionResult.Success();
            }

            return ExecuteElevated(ElevatedOperations.RegistryWrite, request);
        }

        public PrivilegeExecutionResult DeleteValue(RegistryPath path, string valueName)
        {
            var request = new RegistryValueWriteRequest
            {
                Location = path.Location,
                SubKeyPath = path.SubKeyPath,
                ValueName = valueName,
                ValueKind = RegistryValueKind.String
            };

            if (path.Location == RegistryValueLocation.CurrentUser)
            {
                RegistryWriter.DeleteValue(request);
                return PrivilegeExecutionResult.Success();
            }

            return ExecuteElevated(ElevatedOperations.RegistryDelete, request);
        }

        public PrivilegeExecutionResult MoveKey(RegistryPath sourcePath, RegistryPath destinationPath)
        {
            var request = new RegistryKeyOperationRequest
            {
                Location = sourcePath.Location,
                SourceSubKeyPath = sourcePath.SubKeyPath,
                DestinationSubKeyPath = destinationPath.SubKeyPath,
                Recursive = true
            };

            if (sourcePath.Location == RegistryValueLocation.CurrentUser)
            {
                RegistryWriter.MoveKey(request);
                return PrivilegeExecutionResult.Success();
            }

            return ExecuteElevated(ElevatedOperations.RegistryMoveKey, request);
        }

        public PrivilegeExecutionResult DeleteKey(RegistryPath path)
        {
            var request = new RegistryKeyOperationRequest
            {
                Location = path.Location,
                SourceSubKeyPath = path.SubKeyPath,
                Recursive = true
            };

            if (path.Location == RegistryValueLocation.CurrentUser)
            {
                RegistryWriter.DeleteKey(request);
                return PrivilegeExecutionResult.Success();
            }

            return ExecuteElevated(ElevatedOperations.RegistryDeleteKey, request);
        }

        /// <summary>
        /// Exports the key to a .reg backup file (best-effort), then deletes it.
        /// </summary>
        public PrivilegeExecutionResult DeleteKeyWithBackup(RegistryPath path)
        {
            try
            {
                string backupPath = RegistryExporter.GenerateBackupFilePath(path);
                RegistryExporter.ExportToFile(path, backupPath);
            }
            catch
            {
                // Backup is best-effort; never block deletion on backup failure.
            }

            return DeleteKey(path);
        }

        private PrivilegeExecutionResult ExecuteElevated<T>(string operationName, T payload)
        {
            if (_privilegeBroker == null)
                return PrivilegeExecutionResult.Unavailable(
                    PrivilegeErrorCodes.ElevatedAgentUnavailable,
                    "The elevated agent controller is not available.");

            var request = new ElevatedCommandRequest
            {
                OperationName = operationName,
                Payload = DataContractPayloadSerializer.Serialize(payload),
                PrivilegeLevel = PrivilegeLevel.Administrator
            };
            return _privilegeBroker.Execute(request);
        }
    }
}
