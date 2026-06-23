using System;
using WinCraft.Infrastructure.Ipc;
using WinCraft.Infrastructure.RegistryAccess;

namespace WinCraft.Infrastructure.Security
{
    /// <summary>
    /// Dispatches one privileged operation to the appropriate execution path.
    /// </summary>
    internal static class ElevatedOperationExecutor
    {
        public static CommandResult Execute(ElevatedCommandRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.OperationName))
            {
                return CommandResult.Failure(
                    PrivilegeErrorCodes.InvalidRequest,
                    "The elevated request is missing an operation name.",
                    request?.RequestId);
            }

            return request.PrivilegeLevel switch
            {
                PrivilegeLevel.Administrator => ExecuteLocal(request),
                PrivilegeLevel.TrustedInstaller => TrustedInstallerBridge.Execute(request),
                _ => CommandResult.Failure(
                    PrivilegeErrorCodes.PrivilegeLevelRequired,
                    "The privileged host cannot execute a standard-level request.",
                    request.RequestId)
            };
        }

        public static CommandResult ExecuteLocal(ElevatedCommandRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.OperationName))
            {
                return CommandResult.Failure(
                    PrivilegeErrorCodes.InvalidRequest,
                    "The elevated request is missing an operation name.",
                    request?.RequestId);
            }

            if (string.Equals(request.OperationName, ElevatedOperations.Ping, StringComparison.OrdinalIgnoreCase))
                return CommandResult.Success(request.RequestId);

            if (string.Equals(request.OperationName, ElevatedOperations.RegistryWrite, StringComparison.OrdinalIgnoreCase))
                return ExecuteRegistryOperation(request, WindowsRegistryWriter.WriteValue, PrivilegeErrorCodes.RegistryWriteFailed);

            if (string.Equals(request.OperationName, ElevatedOperations.RegistryDelete, StringComparison.OrdinalIgnoreCase))
                return ExecuteRegistryOperation(request, WindowsRegistryWriter.DeleteValue, PrivilegeErrorCodes.RegistryDeleteFailed);

            return CommandResult.Failure(
                PrivilegeErrorCodes.UnsupportedOperation,
                "The elevated operation is not implemented yet.",
                request.RequestId);
        }

        private static CommandResult ExecuteRegistryOperation(
            ElevatedCommandRequest command,
            Action<RegistryValueWriteRequest> operation,
            string errorCode)
        {
            try
            {
                var request = DataContractPayloadSerializer.Deserialize<RegistryValueWriteRequest>(command.Payload);
                operation(request);
                return CommandResult.Success(command.RequestId);
            }
            catch (Exception exception)
            {
                return CommandResult.Failure(errorCode, exception.Message, command.RequestId);
            }
        }
    }
}
