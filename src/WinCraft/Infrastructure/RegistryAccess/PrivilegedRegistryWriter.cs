using System;
using Microsoft.Win32;
using WinCraft.Infrastructure.Ipc;
using WinCraft.Infrastructure.Security;

namespace WinCraft.Infrastructure.RegistryAccess
{
    /// <summary>
    /// Routes registry writes to either the local process or the elevated agent.
    /// </summary>
    internal sealed class PrivilegedRegistryWriter(IPrivilegeBroker privilegeBroker)
    {
        private readonly IPrivilegeBroker _privilegeBroker = privilegeBroker;

        public PrivilegeExecutionResult WriteString(
            RegistryValueLocation location, string subKeyPath, string valueName, string valueData)
        {
            var request = new RegistryValueWriteRequest
            {
                Location = location,
                SubKeyPath = subKeyPath,
                ValueName = valueName,
                ValueData = valueData,
                ValueKind = RegistryValueKind.String
            };

            return location == RegistryValueLocation.CurrentUser
                ? ExecuteLocal(request, WindowsRegistryWriter.WriteValue, PrivilegeErrorCodes.RegistryWriteFailed)
                : ExecuteElevated(request, ElevatedOperations.RegistryWrite);
        }

        public PrivilegeExecutionResult DeleteString(
            RegistryValueLocation location, string subKeyPath, string valueName)
        {
            var request = new RegistryValueWriteRequest
            {
                Location = location,
                SubKeyPath = subKeyPath,
                ValueName = valueName
            };

            return location == RegistryValueLocation.CurrentUser
                ? ExecuteLocal(request, WindowsRegistryWriter.DeleteValue, PrivilegeErrorCodes.RegistryDeleteFailed)
                : ExecuteElevated(request, ElevatedOperations.RegistryDelete);
        }

        private static PrivilegeExecutionResult ExecuteLocal(
            RegistryValueWriteRequest request, Action<RegistryValueWriteRequest> operation, string errorCode)
        {
            try
            {
                operation(request);
                return PrivilegeExecutionResult.Success();
            }
            catch (Exception exception)
            {
                return PrivilegeExecutionResult.Failure(errorCode, exception.Message);
            }
        }

        private PrivilegeExecutionResult ExecuteElevated(
            RegistryValueWriteRequest request, string operationName)
        {
            if (ProcessElevation.IsCurrentProcessElevated())
            {
                var isWrite = operationName == ElevatedOperations.RegistryWrite;
                Action<RegistryValueWriteRequest> action = isWrite
                    ? WindowsRegistryWriter.WriteValue
                    : WindowsRegistryWriter.DeleteValue;
                var errorCode = isWrite
                    ? PrivilegeErrorCodes.RegistryWriteFailed
                    : PrivilegeErrorCodes.RegistryDeleteFailed;
                return ExecuteLocal(request, action, errorCode);
            }

            if (_privilegeBroker == null)
            {
                return PrivilegeExecutionResult.Unavailable(
                    PrivilegeErrorCodes.ElevatedAgentUnavailable,
                    "The elevated agent is not configured.");
            }

            var payload = DataContractPayloadSerializer.Serialize(request);
            var command = new ElevatedCommandRequest
            {
                OperationName = operationName,
                Payload = payload
            };

            return _privilegeBroker.Execute(command);
        }
    }
}
