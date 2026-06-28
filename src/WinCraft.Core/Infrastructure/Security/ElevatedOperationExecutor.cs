using System;
using System.ComponentModel;
using System.IO;
using System.Security;
using WinCraft.Infrastructure.Ipc;
using WinCraft.Infrastructure.RegistryAccess;
using Windows.Win32.Foundation;

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

            return Execute(
                request,
                SystemPrivilegeBridge.Execute,
                TrustedInstallerBridge.Execute);
        }

        internal static CommandResult Execute(
            ElevatedCommandRequest request,
            Func<ElevatedCommandRequest, CommandResult> systemExecutor,
            Func<ElevatedCommandRequest, CommandResult> trustedInstallerExecutor)
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
                PrivilegeLevel.System => systemExecutor(request),
                PrivilegeLevel.TrustedInstaller => trustedInstallerExecutor(request),
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

            if (string.Equals(request.OperationName, ElevatedOperations.FileWrite, StringComparison.OrdinalIgnoreCase))
                return ExecuteFileWrite(request);

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
            catch (Exception exception) when (IsPermissionFailure(exception))
            {
                return CommandResult.Failure(PrivilegeErrorCodes.RegistryAccessDenied, exception.Message, command.RequestId);
            }
            catch (Exception exception)
            {
                return CommandResult.Failure(errorCode, exception.Message, command.RequestId);
            }
        }

        private static CommandResult ExecuteFileWrite(ElevatedCommandRequest command)
        {
            try
            {
                var request = DataContractPayloadSerializer.Deserialize<FileWriteRequest>(command.Payload);
                string destDir = Path.GetDirectoryName(request.Path);
                if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                    Directory.CreateDirectory(destDir);
                using (var stream = new FileStream(request.Path, FileMode.Create, FileAccess.Write))
                    stream.Write(request.Content, 0, request.Content.Length);
                return CommandResult.Success(command.RequestId);
            }
            catch (Exception exception) when (IsPermissionFailure(exception))
            {
                return CommandResult.Failure(PrivilegeErrorCodes.FileAccessDenied, exception.Message, command.RequestId);
            }
            catch (Exception exception)
            {
                return CommandResult.Failure(
                    PrivilegeErrorCodes.FileWriteFailed,
                    exception.Message,
                    command.RequestId);
            }
        }

        internal static bool IsPermissionFailure(Exception exception)
        {
            return exception is UnauthorizedAccessException
                || exception is SecurityException
                || exception is Win32Exception win32Exception
                    && (win32Exception.NativeErrorCode 
                        is (int)WIN32_ERROR.ERROR_ACCESS_DENIED
                        or (int)WIN32_ERROR.ERROR_PRIVILEGE_NOT_HELD);
        }
    }
}
