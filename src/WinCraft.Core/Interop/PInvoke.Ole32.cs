using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace Windows.Win32
{
    internal static partial class PInvoke
    {
        private const string Ole32 = "ole32.dll";

        /// <summary>
        /// Creates a COM IStream over a global memory block.
        /// CsWin32 generates IStream/ISequentialStream COM wrappers that fail on net30.
        /// </summary>
        [DllImport(Ole32)]
        internal static extern int CreateStreamOnHGlobal(
            IntPtr hGlobal,
            [MarshalAs(UnmanagedType.Bool)] bool fDeleteOnRelease,
            out IStream ppstm);

        /// <summary>
        /// Frees the storage medium and releases associated resources.
        /// </summary>
        [DllImport(Ole32)]
        internal static extern void ReleaseStgMedium(ref STGMEDIUM pmedium);
    }
}
