using System;
using System.IO;
using Windows.Win32;

namespace WinCraft.Infrastructure.Diagnostics
{
    /// <summary>
    /// Writes a minidump of the current process.
    /// </summary>
    internal static class CrashDump
    {
        private const PInvoke.MiniDumpType DefaultDumpType =
            PInvoke.MiniDumpType.WithDataSegs |
            PInvoke.MiniDumpType.WithHandleData |
            PInvoke.MiniDumpType.WithUnloadedModules |
            PInvoke.MiniDumpType.WithThreadInfo;

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
                return PInvoke.MiniDumpWriteDump(
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
