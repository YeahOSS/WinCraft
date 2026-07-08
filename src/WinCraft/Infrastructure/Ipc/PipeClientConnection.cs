using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Storage.FileSystem;

namespace WinCraft.Infrastructure.Ipc
{
    /// <summary>
    /// Opens a duplex client connection to an existing named pipe server.
    /// </summary>
    internal static class PipeClientConnection
    {
        private const int DefaultWaitTimeoutMilliseconds = 30000;

        public static SafeFileHandle Open(string pipeName)
        {
            return Open(pipeName, DefaultWaitTimeoutMilliseconds);
        }

        public static SafeFileHandle Open(string pipeName, int waitTimeoutMilliseconds)
        {
            var fullPipeName = PipeBufferIO.BuildFullPipeName(pipeName);

            if (!PInvoke.WaitNamedPipe(fullPipeName, (uint)waitTimeoutMilliseconds))
                throw new Win32Exception(Marshal.GetLastWin32Error());

            var handle = PInvoke.CreateFile(
                fullPipeName,
                (uint)(GENERIC_ACCESS_RIGHTS.GENERIC_READ
                    | GENERIC_ACCESS_RIGHTS.GENERIC_WRITE),
                0,
                null,
                FILE_CREATION_DISPOSITION.OPEN_EXISTING,
                FILE_FLAGS_AND_ATTRIBUTES.FILE_FLAG_OVERLAPPED,
                null);

            if (handle.IsInvalid)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            return handle;
        }
    }
}
