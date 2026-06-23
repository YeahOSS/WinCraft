using System;
using Microsoft.Win32;
using WinCraft.Infrastructure.Ipc;
using WinCraft.Infrastructure.Security;

namespace WinCraft.Infrastructure.RegistryAccess
{
    /// <summary>
    /// Routes registry writes to either the current user context or the privileged host.
    /// </summary>
    internal sealed class PrivilegedRegistryWriter(IPrivilegeBroker privilegeBroker)
    {
        private readonly IPrivilegeBroker _privilegeBroker = privilegeBroker;

        public PrivilegeExecutionResult WriteString(
            RegistryValueLocation location,
            string subKeyPath,
            string valueName,
            string valueData,
            PrivilegeLevel privilegeLevel)
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
                : ExecutePrivileged(request, ElevatedOperations.RegistryWrite, privilegeLevel);
        }

        public PrivilegeExecutionResult DeleteString(
            RegistryValueLocation location,
            string subKeyPath,
            string valueName,
            PrivilegeLevel privilegeLevel)
        {
            var request = new RegistryValueWriteRequest
            {
                Location = location,
                SubKeyPath = subKeyPath,
                ValueName = valueName
            };

            return location == RegistryValueLocation.CurrentUser
                ? ExecuteLocal(request, WindowsRegistryWriter.DeleteValue, PrivilegeErrorCodes.RegistryDeleteFailed)
                : ExecutePrivileged(request, ElevatedOperations.RegistryDelete, privilegeLevel);
        }

        private static PrivilegeExecutionResult ExecuteLocal(
            RegistryValueWriteRequest request,
            Action<RegistryValueWriteRequest> operation,
            string errorCode)
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

        private PrivilegeExecutionResult ExecutePrivileged(
            RegistryValueWriteRequest request,
            string operationName,
            PrivilegeLevel privilegeLevel)
        {
            if (privilegeLevel == PrivilegeLevel.Standard)
            {
                return PrivilegeExecutionResult.Failure(
                    PrivilegeErrorCodes.PrivilegeLevelRequired,
                    "A machine-level registry write must declare an elevated privilege level.");
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
                Payload = payload,
                PrivilegeLevel = privilegeLevel,
                RequestId = Guid.NewGuid().ToString("N")
            };

            return _privilegeBroker.Execute(command);
        }
    }
}
