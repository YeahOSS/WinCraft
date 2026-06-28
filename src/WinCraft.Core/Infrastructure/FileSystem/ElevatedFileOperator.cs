using System;
using System.IO;
using System.Text;
using WinCraft.Infrastructure.Ipc;
using WinCraft.Infrastructure.Security;

namespace WinCraft.Infrastructure.FileSystem
{
    /// <summary>
    /// Executes file-system operations locally or through the elevated agent.
    /// </summary>
    internal sealed class ElevatedFileOperator(IPrivilegeBroker privilegeBroker)
    {
        private readonly IPrivilegeBroker _privilegeBroker = privilegeBroker;

        public PrivilegeExecutionResult Delete(string path, bool recursive, bool elevated)
        {
            return ExecuteWithElevation(
                elevated,
                () => DeleteLocal(path, recursive),
                ElevatedOperations.FileDelete,
                new FileOperationRequest { SourcePath = path, Recursive = recursive });
        }

        public PrivilegeExecutionResult Rename(string sourcePath, string destinationPath, bool elevated)
        {
            return ExecuteWithElevation(
                elevated,
                () => RenameLocal(sourcePath, destinationPath),
                ElevatedOperations.FileRename,
                new FileOperationRequest { SourcePath = sourcePath, DestinationPath = destinationPath });
        }

        public PrivilegeExecutionResult SetAttributes(string path, FileAttributes attributes, bool elevated)
        {
            return ExecuteWithElevation(
                elevated,
                () => File.SetAttributes(path, attributes),
                ElevatedOperations.FileSetAttributes,
                new FileOperationRequest { SourcePath = path, Attributes = attributes });
        }

        public PrivilegeExecutionResult WriteAllText(string path, string content, bool elevated)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(content ?? string.Empty);
            return ExecuteWithElevation(
                elevated,
                () =>
                {
                    string directoryPath = Path.GetDirectoryName(path);
                    if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
                        Directory.CreateDirectory(directoryPath);
                    File.WriteAllBytes(path, bytes);
                },
                ElevatedOperations.FileWrite,
                new FileWriteRequest { Path = path, Content = bytes });
        }

        private PrivilegeExecutionResult ExecuteWithElevation<T>(
            bool elevated,
            Action localAction,
            string operationName,
            T operationRequest)
        {
            if (!elevated)
            {
                localAction();
                return PrivilegeExecutionResult.Success();
            }

            return ExecuteElevated(operationName, operationRequest);
        }

        private PrivilegeExecutionResult ExecuteElevated<T>(string operationName, T operationRequest)
        {
            if (_privilegeBroker == null)
                return PrivilegeExecutionResult.Unavailable(
                    PrivilegeErrorCodes.ElevatedAgentUnavailable,
                    "The elevated agent controller is not available.");

            var request = new ElevatedCommandRequest
            {
                OperationName = operationName,
                Payload = DataContractPayloadSerializer.Serialize(operationRequest),
                PrivilegeLevel = PrivilegeLevel.Administrator
            };
            return _privilegeBroker.Execute(request);
        }

        private static void DeleteLocal(string path, bool recursive)
        {
            try
            {
                FileAttributes attrs = File.GetAttributes(path);
                if ((attrs & FileAttributes.Directory) == FileAttributes.Directory)
                    Directory.Delete(path, recursive);
                else
                    File.Delete(path);
            }
            catch (FileNotFoundException)
            {
                // Path does not exist — idempotent delete succeeds.
            }
            catch (DirectoryNotFoundException)
            {
                // Path does not exist — idempotent delete succeeds.
            }
        }

        private static void RenameLocal(string sourcePath, string destinationPath)
        {
            try
            {
                FileAttributes attrs = File.GetAttributes(sourcePath);
                if ((attrs & FileAttributes.Directory) == FileAttributes.Directory)
                    Directory.Move(sourcePath, destinationPath);
                else
                    File.Move(sourcePath, destinationPath);
            }
            catch (DirectoryNotFoundException)
            {
                throw new FileNotFoundException("The source path does not exist.", sourcePath);
            }
        }
    }
}
