using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Security;
using System.Threading;
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
        private static readonly Dictionary<string, Func<ElevatedCommandRequest, CommandResult>> OperationHandlers =
            new(StringComparer.OrdinalIgnoreCase)
        {
            { ElevatedOperations.Ping, cmd => CommandResult.Success(cmd.RequestId) },
            { ElevatedOperations.RegistryWrite, cmd => ExecuteRegistryOperation<RegistryValueWriteRequest>(cmd, RegistryWriter.WriteValue, PrivilegeErrorCodes.RegistryWriteFailed) },
            { ElevatedOperations.RegistryDelete, cmd => ExecuteRegistryOperation<RegistryValueWriteRequest>(cmd, RegistryWriter.DeleteValue, PrivilegeErrorCodes.RegistryDeleteFailed) },
            { ElevatedOperations.RegistryDeleteKey, cmd => ExecuteRegistryOperation<RegistryKeyOperationRequest>(cmd, RegistryWriter.DeleteKey, PrivilegeErrorCodes.RegistryKeyDeleteFailed) },
            { ElevatedOperations.RegistryMoveKey, cmd => ExecuteRegistryOperation<RegistryKeyOperationRequest>(cmd, RegistryWriter.MoveKey, PrivilegeErrorCodes.RegistryKeyMoveFailed) },
            { ElevatedOperations.FileWrite, ExecuteFileWrite },
            { ElevatedOperations.FileDelete, ExecuteFileDelete },
            { ElevatedOperations.FileRename, ExecuteFileRename },
            { ElevatedOperations.FileSetAttributes, ExecuteFileSetAttributes },
        };
        public static CommandResult Execute(ElevatedCommandRequest request)
        {
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
            if (string.IsNullOrEmpty(request?.OperationName))
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
            if (string.IsNullOrEmpty(request?.OperationName))
            {
                return CommandResult.Failure(
                    PrivilegeErrorCodes.InvalidRequest,
                    "The elevated request is missing an operation name.",
                    request?.RequestId);
            }

            return OperationHandlers.TryGetValue(request.OperationName, out var handler)
                ? handler(request)
                : CommandResult.Failure(
                    PrivilegeErrorCodes.UnsupportedOperation,
                    "The elevated operation is not implemented yet.",
                    request.RequestId);
        }

        private static CommandResult ExecuteRegistryOperation<T>(
            ElevatedCommandRequest command,
            Action<T> operation,
            string errorCode)
        {
            try
            {
                var request = DataContractPayloadSerializer.Deserialize<T>(command.Payload);
                operation(request);
                return CommandResult.Success(command.RequestId);
            }
            catch (Exception exception) when (IsPermissionFailure(exception))
            {
                return CommandResult.Failure(PrivilegeErrorCodes.RegistryAccessDenied, exception.Message, command.RequestId);
            }
            catch (Exception exception) when (!IsCriticalException(exception))
            {
                return CommandResult.Failure(errorCode, exception.Message, command.RequestId);
            }
        }

        private static CommandResult ExecuteFileOperation(
            ElevatedCommandRequest command,
            string errorCode,
            Func<CommandResult> operation)
        {
            try
            {
                return operation();
            }
            catch (Exception exception) when (IsPermissionFailure(exception))
            {
                return CommandResult.Failure(PrivilegeErrorCodes.FileAccessDenied, exception.Message, command.RequestId);
            }
            catch (Exception exception) when (!IsCriticalException(exception))
            {
                return CommandResult.Failure(errorCode, exception.Message, command.RequestId);
            }
        }

        private static CommandResult ExecuteFileWrite(ElevatedCommandRequest command)
        {
            return ExecuteFileOperation(command, PrivilegeErrorCodes.FileWriteFailed, () =>
            {
                var request = DataContractPayloadSerializer.Deserialize<FileWriteRequest>(command.Payload);
                string destDir = Path.GetDirectoryName(request.Path);
                if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                    Directory.CreateDirectory(destDir);
                using (var stream = new FileStream(request.Path, FileMode.Create, FileAccess.Write))
                    stream.Write(request.Content, 0, request.Content.Length);
                return CommandResult.Success(command.RequestId);
            });
        }

        private static CommandResult ExecuteFileDelete(ElevatedCommandRequest command)
        {
            return ExecuteFileOperation(command, PrivilegeErrorCodes.FileDeleteFailed, () =>
            {
                var request = DataContractPayloadSerializer.Deserialize<FileOperationRequest>(command.Payload);
                try
                {
                    FileAttributes attrs = File.GetAttributes(request.SourcePath);
                    if ((attrs & FileAttributes.Directory) == FileAttributes.Directory)
                        Directory.Delete(request.SourcePath, request.Recursive);
                    else
                        File.Delete(request.SourcePath);
                }
                catch (FileNotFoundException)
                {
                    // Path does not exist — idempotent delete succeeds.
                }
                catch (DirectoryNotFoundException)
                {
                    // Path does not exist — idempotent delete succeeds.
                }

                return CommandResult.Success(command.RequestId);
            });
        }

        private static CommandResult ExecuteFileRename(ElevatedCommandRequest command)
        {
            return ExecuteFileOperation(command, PrivilegeErrorCodes.FileRenameFailed, () =>
            {
                var request = DataContractPayloadSerializer.Deserialize<FileOperationRequest>(command.Payload);
                try
                {
                    FileAttributes attrs = File.GetAttributes(request.SourcePath);
                    if ((attrs & FileAttributes.Directory) == FileAttributes.Directory)
                        Directory.Move(request.SourcePath, request.DestinationPath);
                    else
                        File.Move(request.SourcePath, request.DestinationPath);
                }
                catch (DirectoryNotFoundException)
                {
                    throw new FileNotFoundException("The source path does not exist.", request.SourcePath);
                }

                return CommandResult.Success(command.RequestId);
            });
        }

        private static CommandResult ExecuteFileSetAttributes(ElevatedCommandRequest command)
        {
            return ExecuteFileOperation(command, PrivilegeErrorCodes.FileSetAttributesFailed, () =>
            {
                var request = DataContractPayloadSerializer.Deserialize<FileOperationRequest>(command.Payload);
                File.SetAttributes(request.SourcePath, request.Attributes);
                return CommandResult.Success(command.RequestId);
            });
        }

        internal static bool IsPermissionFailure(Exception exception)
        {
            return exception is UnauthorizedAccessException
                || exception is SecurityException
                || (exception is Win32Exception win32Exception
                    && (win32Exception.NativeErrorCode
                        is (int)WIN32_ERROR.ERROR_ACCESS_DENIED
                        or (int)WIN32_ERROR.ERROR_PRIVILEGE_NOT_HELD));
        }

        private static bool IsCriticalException(Exception exception)
        {
            return exception is OutOfMemoryException
                || exception is StackOverflowException
                || exception is ThreadAbortException;
        }
    }
}
