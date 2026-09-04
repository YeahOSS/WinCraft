using System;
using System.Runtime.InteropServices;

namespace Windows.Win32
{
    /// <summary>
    /// Data structure for <c>SetWindowCompositionAttribute</c>.
    /// CsWin32 cannot generate this API or its associated types.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct WINDOWCOMPOSITIONATTRIBDATA
    {
        public int Attribute;
        public IntPtr Data;
        public int SizeOfData;
    }
}
