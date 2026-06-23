using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Storage.FileSystem;

namespace WinCraft.Infrastructure.Ipc
{
    /// <summary>
    /// Pipe client that runs inside the privileged host process.
    /// </summary>
    internal static class ElevatedAgentPipeClient
    {
        private const string BrokenPipeMessage = "The UI process closed the pipe unexpectedly.";
        private const int WaitTimeoutMilliseconds = 30000;

        public static void Run(
            string pipeName,
            int? expectedServerProcessId,
            Func<ElevatedCommandRequest, CommandResult> dispatch)
        {
            var fullPipeName = PipeBufferIO.BuildFullPipeName(pipeName);
            var connectedServerProcessId = expectedServerProcessId;
            var hasAcceptedServerConnection = false;

            while (true)
            {
                if (ShouldStopWaitingForServer(connectedServerProcessId))
                {
                    break;
                }

                if (!PInvoke.WaitNamedPipe(fullPipeName, (uint)WaitTimeoutMilliseconds))
                {
                    if (ShouldStopWaitingForServer(connectedServerProcessId))
                    {
                        break;
                    }

                    continue;
                }

                using var pipeHandle = PInvoke.CreateFile(
                    fullPipeName,
                    (uint)(GENERIC_ACCESS_RIGHTS.GENERIC_READ | GENERIC_ACCESS_RIGHTS.GENERIC_WRITE),
                    0,
                    null,
                    FILE_CREATION_DISPOSITION.OPEN_EXISTING,
                    FILE_FLAGS_AND_ATTRIBUTES.FILE_FLAG_OVERLAPPED,
                    null);

                if (pipeHandle.IsInvalid)
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                try
                {
                    var actualServerProcessId = GetServerProcessId(pipeHandle);
                    if (!IsExpectedServerConnection(
                        expectedServerProcessId,
                        hasAcceptedServerConnection,
                        connectedServerProcessId,
                        actualServerProcessId))
                    {
                        continue;
                    }

                    connectedServerProcessId = actualServerProcessId;
                    hasAcceptedServerConnection = true;
                    var request = PipeMessageIO.ReadMessage<ElevatedCommandRequest>(pipeHandle, BrokenPipeMessage);
                    if (request == null)
                        throw new InvalidOperationException("The privileged host received an empty request.");

                    var isShutdown = string.Equals(
                        request.OperationName,
                        ElevatedOperations.Shutdown,
                        StringComparison.OrdinalIgnoreCase);

                    var result = isShutdown
                        ? CommandResult.Success(request.RequestId)
                        : dispatch(request);

                    PipeMessageIO.WriteMessage(pipeHandle, result);

                    if (isShutdown)
                        break;
                }
                catch (Win32Exception)
                {
                    // A transient disconnection is expected between requests.
                }
                catch (InvalidOperationException)
                {
                    // Ignore malformed or torn-down connections and wait again.
                }
                catch (TimeoutException)
                {
                    // Timed I/O should reset this connection, not terminate the host.
                }
            }
        }

        private static bool ShouldStopWaitingForServer(int? connectedServerProcessId)
        {
            if (!connectedServerProcessId.HasValue)
                return false;

            return !ProcessExists(connectedServerProcessId.Value);
        }

        private static bool IsExpectedServerConnection(
            int? expectedServerProcessId,
            bool hasAcceptedServerConnection,
            int? connectedServerProcessId,
            int actualServerProcessId)
        {
            if (!hasAcceptedServerConnection && expectedServerProcessId.HasValue)
                return actualServerProcessId == expectedServerProcessId.Value;

            if (hasAcceptedServerConnection && connectedServerProcessId.HasValue)
                return actualServerProcessId == connectedServerProcessId.Value;

            return true;
        }

        private static int GetServerProcessId(SafeFileHandle pipeHandle)
        {
            if (!PInvoke.GetNamedPipeServerProcessId(pipeHandle, out uint serverProcessId))
                throw new Win32Exception(Marshal.GetLastWin32Error());

            return (int)serverProcessId;
        }

        private static bool ProcessExists(int pid)
        {
            try
            {
                using var process = Process.GetProcessById(pid);
                return !process.HasExited;
            }
            catch
            {
                return false;
            }
        }
    }
}
