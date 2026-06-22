using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace WinCraft.Infrastructure.Ipc
{
    /// <summary>
    /// Shared low-level pipe read/write helpers used by both the UI-side
    /// pipe server and the elevated-agent pipe client.
    /// </summary>
    /// <remarks>
    /// All I/O uses overlapped operations because the pipe is created
    /// with <see cref="FILE_FLAGS_AND_ATTRIBUTES.FILE_FLAG_OVERLAPPED"/>.
    /// Each call allocates a short-lived <see cref="NativeOverlapped"/>
    /// with a dedicated event so the buffer and overlapped struct stay
    /// pinned for the duration of the I/O.
    /// </remarks>
    internal static class PipeBufferIO
    {
        public static string BuildFullPipeName(string pipeName)
        {
            return string.Format("\\\\.\\pipe\\{0}", pipeName);
        }

        public static unsafe byte[] ReadExact(SafeFileHandle pipeHandle, int length, string brokenPipeMessage)
        {
            var buffer = new byte[length];
            var offset = 0;

            while (offset < length)
            {
                using var readEvent = new ManualResetEvent(false);
                var eventSafeHandle = readEvent.SafeWaitHandle;
                var mustRelease = false;
                try
                {
                    eventSafeHandle.DangerousAddRef(ref mustRelease);
                    var eventHandle = eventSafeHandle.DangerousGetHandle();

                    var overlapped = new NativeOverlapped { EventHandle = eventHandle };
                    var gcHandle = GCHandle.Alloc(overlapped, GCHandleType.Pinned);
                    try
                    {
                        var overlappedPtr = (NativeOverlapped*)gcHandle.AddrOfPinnedObject();

                        fixed (byte* bufferPointer = &buffer[offset])
                        {
                            if (!PInvoke.ReadFile(pipeHandle, bufferPointer, (uint)(length - offset), null, overlappedPtr))
                            {
                                var errorCode = Marshal.GetLastWin32Error();
                                if (errorCode == (int)WIN32_ERROR.ERROR_IO_PENDING)
                                {
                                    if (!PInvoke.GetOverlappedResult(pipeHandle, in *overlappedPtr, out uint bytesRead, true))
                                        throw new Win32Exception(Marshal.GetLastWin32Error());
                                    if (bytesRead == 0)
                                        throw new InvalidOperationException("The pipe peer returned an incomplete payload.");

                                    offset += (int)bytesRead;
                                }
                                else if (errorCode == (int)WIN32_ERROR.ERROR_BROKEN_PIPE)
                                {
                                    throw new InvalidOperationException(brokenPipeMessage);
                                }
                                else
                                {
                                    throw new Win32Exception(errorCode);
                                }
                            }
                            else
                            {
                                // Completed synchronously — get the byte count
                                // from GetOverlappedResult (lpNumberOfBytesRead
                                // is null, so it is not available from ReadFile).
                                if (!PInvoke.GetOverlappedResult(pipeHandle, in *overlappedPtr, out uint bytesRead, false))
                                    throw new Win32Exception(Marshal.GetLastWin32Error());
                                if (bytesRead == 0)
                                    throw new InvalidOperationException("The pipe peer returned an incomplete payload.");

                                offset += (int)bytesRead;
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

            return buffer;
        }

        /// <summary>
        /// Writes an entire buffer to the pipe, looping until all bytes are sent.
        /// </summary>
        /// <remarks>
        /// Each <c>WriteFile</c> call is followed by <c>GetOverlappedResult</c>
        /// with <c>bWait=true</c>, so the data is committed to the kernel pipe
        /// buffer when this method returns. An explicit flush is not needed.
        /// </remarks>
        public static unsafe void WriteBuffer(SafeFileHandle pipeHandle, byte[] buffer)
        {
            var offset = 0;

            while (offset < buffer.Length)
            {
                using var writeEvent = new ManualResetEvent(false);
                var eventSafeHandle = writeEvent.SafeWaitHandle;
                var mustRelease = false;
                try
                {
                    eventSafeHandle.DangerousAddRef(ref mustRelease);
                    var eventHandle = eventSafeHandle.DangerousGetHandle();

                    var overlapped = new NativeOverlapped { EventHandle = eventHandle };
                    var gcHandle = GCHandle.Alloc(overlapped, GCHandleType.Pinned);
                    try
                    {
                        var overlappedPtr = (NativeOverlapped*)gcHandle.AddrOfPinnedObject();

                        fixed (byte* bufferPointer = &buffer[offset])
                        {
                            if (!PInvoke.WriteFile(pipeHandle, bufferPointer, (uint)(buffer.Length - offset), null, overlappedPtr))
                            {
                                var errorCode = Marshal.GetLastWin32Error();
                                if (errorCode == (int)WIN32_ERROR.ERROR_IO_PENDING)
                                {
                                    if (!PInvoke.GetOverlappedResult(pipeHandle, in *overlappedPtr, out uint bytesWritten, true))
                                        throw new Win32Exception(Marshal.GetLastWin32Error());
                                    if (bytesWritten == 0)
                                        throw new InvalidOperationException("The pipe did not accept the payload.");

                                    offset += (int)bytesWritten;
                                }
                                else if (errorCode == (int)WIN32_ERROR.ERROR_BROKEN_PIPE)
                                {
                                    throw new InvalidOperationException("The pipe peer closed the connection during write.");
                                }
                                else
                                {
                                    throw new Win32Exception(errorCode);
                                }
                            }
                            else
                            {
                                if (!PInvoke.GetOverlappedResult(pipeHandle, in *overlappedPtr, out uint bytesWritten, false))
                                    throw new Win32Exception(Marshal.GetLastWin32Error());
                                if (bytesWritten == 0)
                                    throw new InvalidOperationException("The pipe did not accept the payload.");

                                offset += (int)bytesWritten;
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
}
