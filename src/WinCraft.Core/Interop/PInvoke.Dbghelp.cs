using System;
using System.Runtime.InteropServices;

namespace Windows.Win32
{
    internal static partial class PInvoke
    {
        private const string Dbghelp = "dbghelp.dll";

        /// <summary>
        /// Writes a user-mode minidump to a file.
        /// </summary>
        /// <remarks>
        /// The parameter structures contain pointer-sized fields whose binary layout
        /// differs between x86 and x64.  CsWin32 cannot emit the binding under AnyCPU.
        /// </remarks>
        [DllImport(Dbghelp, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool MiniDumpWriteDump(
            IntPtr hProcess,
            uint processId,
            IntPtr hFile,
            MiniDumpType dumpType,
            IntPtr exceptionParam,
            IntPtr userStreamParam,
            IntPtr callbackParam);

        [Flags]
        internal enum MiniDumpType : uint
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
    }
}
