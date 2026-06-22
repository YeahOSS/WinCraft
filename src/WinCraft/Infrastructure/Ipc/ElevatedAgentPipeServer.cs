using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Storage.FileSystem;

namespace WinCraft.Infrastructure.Ipc
{
    /// <summary>
    /// UI-side named pipe server. Creates the pipe that the elevated
    /// agent connects to, accepts connections, and delegates typed
    /// message I/O to <see cref="PipeMessageIO"/>.
    /// </summary>
    /// <remarks>
    /// The pipe is created in the unelevated UI process with default
    /// security. The elevated agent (same user, full Administrators token)
    /// can connect without hitting the DACL or integrity-level barriers.
    /// </remarks>
    internal static class ElevatedAgentPipeServer
    {
        /// <summary>
        /// Timeout in milliseconds for the elevated agent to connect.
        /// </summary>
        private const int ConnectTimeoutMilliseconds = 30000;

        /// <summary>
        /// Creates a named pipe server instance that the elevated agent will connect to.
        /// </summary>
        public static SafeFileHandle Create(string pipeName)
        {
            var fullPipeName = PipeBufferIO.BuildFullPipeName(pipeName);

            var pipeHandle = PInvoke.CreateNamedPipe(
                fullPipeName,
                FILE_FLAGS_AND_ATTRIBUTES.PIPE_ACCESS_DUPLEX
                    | FILE_FLAGS_AND_ATTRIBUTES.FILE_FLAG_OVERLAPPED,
                0,
                1,
                8 * 1024,
                8 * 1024,
                0,
                null);

            if (pipeHandle.IsInvalid)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            return pipeHandle;
        }

        /// <summary>
        /// Blocks until the elevated agent connects to the server pipe
        /// or the timeout expires.
        /// </summary>
        /// <remarks>
        /// Uses overlapped I/O so that <see cref="ConnectNamedPipe"/> can
        /// be cancelled after <see cref="ConnectTimeoutMilliseconds"/>.
        /// Without a timeout a dead agent would leave the UI thread
        /// blocked indefinitely.
        /// </remarks>
        public static unsafe void WaitForConnection(SafeFileHandle pipeHandle)
        {
            using var connectEvent = new ManualResetEvent(false);
            var eventSafeHandle = connectEvent.SafeWaitHandle;
            var mustRelease = false;
            try
            {
                eventSafeHandle.DangerousAddRef(ref mustRelease);
                var eventHandle = eventSafeHandle.DangerousGetHandle();

                var overlapped = new NativeOverlapped { EventHandle = eventHandle };

                // Pin the overlapped struct so its address stays valid for the
                // duration of the overlapped ConnectNamedPipe call.
                var gcHandle = GCHandle.Alloc(overlapped, GCHandleType.Pinned);
                try
                {
                    var overlappedPtr = (NativeOverlapped*)gcHandle.AddrOfPinnedObject();

                    if (!PInvoke.ConnectNamedPipe(pipeHandle, overlappedPtr))
                    {
                        var errorCode = Marshal.GetLastWin32Error();
                        if (errorCode == (int)WIN32_ERROR.ERROR_IO_PENDING)
                        {
                            if (!connectEvent.WaitOne(ConnectTimeoutMilliseconds))
                            {
                                // Cancel all pending I/O on the pipe and drain
                                // the completion so the overlapped struct can
                                // be safely freed.
                                PInvoke.CancelIoEx(pipeHandle, (NativeOverlapped*)null);
                                PInvoke.GetOverlappedResult(
                                    pipeHandle, in *overlappedPtr, out _, true);
                                throw new TimeoutException(
                                    "The elevated agent did not connect to the named pipe within the timeout period.");
                            }
                        }
                        else if (errorCode != (int)WIN32_ERROR.ERROR_PIPE_CONNECTED)
                        {
                            throw new Win32Exception(errorCode);
                        }
                    }
                }
                finally
                {
                    gcHandle.Free();
                }
            }
            finally
            {
                if (mustRelease)
                    eventSafeHandle.DangerousRelease();
            }
        }
    }
}
