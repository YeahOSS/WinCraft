using System;
using System.Runtime.InteropServices;

namespace Windows.Win32
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct TRACKMOUSEEVENT
    {
        public uint cbSize;
        public TRACKMOUSEEVENT_FLAGS dwFlags;
        public IntPtr hwndTrack;
        public uint dwHoverTime;
    }

    /// <summary>Non-client mouse tracking flags.</summary>
    [Flags]
    internal enum TRACKMOUSEEVENT_FLAGS : uint
    {
        TME_HOVER = 0x00000001,
        TME_LEAVE = 0x00000002,
        TME_NONCLIENT = 0x00000010,
        TME_QUERY = 0x40000000,
        TME_CANCEL = 0x80000000,
    }
}
