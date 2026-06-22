using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Storage.FileSystem;

namespace WinCraft.Infrastructure.Ipc
{
    /// <summary>
    /// Pipe client that runs inside the elevated agent process.
    /// Connects to the UI-owned named pipe, reads a request, dispatches it,
    /// writes the result, and repeats until a shutdown operation arrives.
    /// </summary>
    internal static class ElevatedAgentPipeClient
    {
        private const string BrokenPipeMessage = "The UI process closed the pipe unexpectedly.";

        /// <summary>
        /// Per-attempt timeout in milliseconds for <see cref="PInvoke.WaitNamedPipe"/>.
        /// A finite value gives the agent a periodic chance to check whether the
        /// UI process is still alive between waits.
        /// </summary>
        private const int WaitTimeoutMilliseconds = 30000;

        /// <summary>
        /// Connects to the UI-owned named pipe and processes requests
        /// until a shutdown operation arrives or the UI process exits.
        /// This call blocks for the lifetime of the elevated agent.
        /// </summary>
        public static void Run(string pipeName, Func<ElevatedCommandRequest, CommandResult> dispatch)
        {
            var fullPipeName = PipeBufferIO.BuildFullPipeName(pipeName);

            // The pipe name embeds the UI process ID — parse it so we can
            // detect when the UI exits without a clean shutdown.
            var parentPid = ParseParentPid(pipeName);

            while (true)
            {
                if (parentPid.HasValue && !ProcessExists(parentPid.Value))
                    break;

                if (!PInvoke.WaitNamedPipe(fullPipeName, (uint)WaitTimeoutMilliseconds))
                {
                    // Timed out or the pipe is unavailable.  If the UI is
                    // still alive this is just an idle interval — retry.
                    // If the UI is gone, exit.
                    if (parentPid.HasValue && !ProcessExists(parentPid.Value))
                        break;
                    continue;
                }

                using var pipeHandle = PInvoke.CreateFile(
                    fullPipeName,
                    (uint)(GENERIC_ACCESS_RIGHTS.GENERIC_READ | GENERIC_ACCESS_RIGHTS.GENERIC_WRITE),
                    0,
                    null,
                    FILE_CREATION_DISPOSITION.OPEN_EXISTING,
                    0,
                    null);

                if (pipeHandle.IsInvalid)
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                try
                {
                    var request = PipeMessageIO.ReadMessage<ElevatedCommandRequest>(pipeHandle, BrokenPipeMessage);

                    var isShutdown = string.Equals(
                        request.OperationName,
                        ElevatedOperations.Shutdown,
                        StringComparison.OrdinalIgnoreCase);

                    var result = isShutdown
                        ? CommandResult.Success()
                        : dispatch(request);

                    PipeMessageIO.WriteMessage(pipeHandle, result);

                    if (isShutdown)
                        break;
                }
                catch (Win32Exception)
                {
                    // The pipe was closed or the connection was lost.
                    // Loop back and wait for the next connection.
                }
                catch (InvalidOperationException)
                {
                    // Protocol error (broken pipe, invalid payload).
                    // Loop back and wait for the next connection.
                }
            }
        }

        /// <summary>
        /// Extracts the UI process ID from the pipe name.
        /// The format is "WinCraft.ElevatedAgent.&lt;pid&gt;".
        /// </summary>
        private static int? ParseParentPid(string pipeName)
        {
            var lastDot = pipeName.LastIndexOf('.');
            if (lastDot < 0)
                return null;

            var pidPart = pipeName.Substring(lastDot + 1);
            return int.TryParse(pidPart, out int pid) ? pid : null;
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
