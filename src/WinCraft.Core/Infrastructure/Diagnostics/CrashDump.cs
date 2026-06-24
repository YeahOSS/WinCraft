using System;
using System.IO;
using System.Runtime.InteropServices;
using Windows.Win32;

namespace WinCraft.Infrastructure.Diagnostics
{
    /// <summary>
    /// Writes a minidump of the current process.
    /// </summary>
    internal static class CrashDump
    {
        // Uses a manual P/Invoke for MiniDumpWriteDump rather than CsWin32.
        // The parameter structures (e.g. MINIDUMP_EXCEPTION_INFORMATION)
        // contain pointer-sized fields whose binary layout differs between
        // x86 and x64.  CsWin32 cannot emit the binding under AnyCPU
        // (PInvoke005) because it cannot know which layout to use at
        // generation time.  IntPtr-based marshalling avoids the issue.
        [DllImport("dbghelp.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool MiniDumpWriteDump(
            IntPtr hProcess,
            uint processId,
            IntPtr hFile,
            MiniDumpType dumpType,
            IntPtr exceptionParam,
            IntPtr userStreamParam,
            IntPtr callbackParam);

        [Flags]
        private enum MiniDumpType : uint
        {
            Normal = 0x00000000,
            WithDataSegs = 0x00000001,
            WithFullMemory = 0x00000002,
            WithHandleData = 0x00000004,
            WithUnloadedModules = 0x00000020,
            WithIndirectlyReferencedMemory = 0x00000040,
            WithFullMemoryInfo = 0x00000800,
            WithThreadInfo = 0x00008000,
            WithCodeSegs = 0x00010000,
        }

        private const MiniDumpType DefaultDumpType =
            MiniDumpType.WithDataSegs |
            MiniDumpType.WithHandleData |
            MiniDumpType.WithUnloadedModules |
            MiniDumpType.WithThreadInfo;

        /// <summary>
        /// Writes a minidump of the current process to <paramref name="outputPath"/>.
        /// Intermediate directories are created automatically.
        /// </summary>
        /// <returns><c>true</c> when the dump was written successfully; otherwise <c>false</c>.</returns>
        public static bool TryWrite(string outputPath)
        {
            if (outputPath == null)
                return false;

            try
            {
                var directory = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                using var processHandle = PInvoke.GetCurrentProcess_SafeHandle();
                using var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
                return MiniDumpWriteDump(
                    processHandle.DangerousGetHandle(),
                    PInvoke.GetCurrentProcessId(),
                    fs.SafeFileHandle.DangerousGetHandle(),
                    DefaultDumpType,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero);
            }
            catch
            {
                // Must not throw — the caller is already inside an
                // unhandled-exception handler.
                return false;
            }
        }
    }
}
