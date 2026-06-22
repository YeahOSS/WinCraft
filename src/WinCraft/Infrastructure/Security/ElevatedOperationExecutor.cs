using System;
using WinCraft.Infrastructure.Ipc;
using WinCraft.Infrastructure.RegistryAccess;

namespace WinCraft.Infrastructure.Security
{
    /// <summary>
    /// Dispatches one elevated operation to the appropriate handler.
    /// Used as the request handler for the persistent elevated agent loop.
    /// </summary>
    internal static class ElevatedOperationExecutor
    {
        public static CommandResult Execute(ElevatedCommandRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.OperationName))
            {
                return CommandResult.Failure(
                    PrivilegeErrorCodes.InvalidRequest,
                    "The elevated request is missing an operation name.");
            }

            if (string.Equals(request.OperationName, ElevatedOperations.RegistryWrite, StringComparison.OrdinalIgnoreCase))
                return ExecuteRegistryOperation(request.Payload, WindowsRegistryWriter.WriteValue, PrivilegeErrorCodes.RegistryWriteFailed);

            if (string.Equals(request.OperationName, ElevatedOperations.RegistryDelete, StringComparison.OrdinalIgnoreCase))
                return ExecuteRegistryOperation(request.Payload, WindowsRegistryWriter.DeleteValue, PrivilegeErrorCodes.RegistryDeleteFailed);

            return CommandResult.Failure(
                PrivilegeErrorCodes.UnsupportedOperation,
                "The elevated operation is not implemented yet.");
        }

        private static CommandResult ExecuteRegistryOperation(
            string payload, Action<RegistryValueWriteRequest> operation, string errorCode)
        {
            try
            {
                var request = DataContractPayloadSerializer.Deserialize<RegistryValueWriteRequest>(payload);
                operation(request);
                return CommandResult.Success();
            }
            catch (Exception exception)
            {
                return CommandResult.Failure(errorCode, exception.Message);
            }
        }
    }
}
