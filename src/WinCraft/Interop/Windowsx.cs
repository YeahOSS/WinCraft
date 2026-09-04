using System;

namespace Windows.Win32
{
    internal static class Windowsx
    {
        internal static int GetXLParam(IntPtr lParam)
        {
            return unchecked((short)(lParam.ToInt64() & 0xFFFF));
        }

        internal static int GetYLParam(IntPtr lParam)
        {
            return unchecked((short)((lParam.ToInt64() >> 16) & 0xFFFF));
        }

        internal static uint GetSystemCommand(IntPtr wParam)
        {
            const uint SystemCommandMask = 0xFFF0;
            return unchecked((uint)wParam.ToInt64()) & SystemCommandMask;
        }
    }
}
