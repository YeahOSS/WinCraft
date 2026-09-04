using System;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Windows.Win32
{
    internal static partial class PInvoke
    {
        private const string User32 = "user32.dll";

        // CsWin32 cannot generate GetClassLongPtr for AnyCPU — LONG_PTR is pointer-sized.
        // GetClassLongPtrW only exists on 64-bit; 32-bit uses GetClassLongW.
        [DllImport(User32, ExactSpelling = true, EntryPoint = "GetClassLongPtrW", SetLastError = true)]
        private static extern IntPtr GetClassLongPtr64(HWND hWnd, GET_CLASS_LONG_INDEX nIndex);

        [DllImport(User32, ExactSpelling = true, EntryPoint = "GetClassLongW", SetLastError = true)]
        private static extern IntPtr GetClassLongPtr32(HWND hWnd, GET_CLASS_LONG_INDEX nIndex);

        internal static IntPtr GetClassLongPtr(HWND hWnd, GET_CLASS_LONG_INDEX nIndex)
            => IntPtr.Size == 8
                ? GetClassLongPtr64(hWnd, nIndex)
                : GetClassLongPtr32(hWnd, nIndex);

        // CsWin32 does not expose this user32 export or its associated data
        // structures (ACCENT_POLICY, ACCENT_STATE, WINDOWCOMPOSITIONATTRIBDATA).
        internal const int WCA_ACCENT_POLICY = 19;

        [DllImport(User32, ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetWindowCompositionAttribute(
            IntPtr hwnd,
            ref WINDOWCOMPOSITIONATTRIBDATA data);

        // CsWin32 cannot generate TrackMouseEvent for AnyCPU — TRACKMOUSEEVENT
        // contains a pointer-sized HWND field (same reason as GetClassLongPtr).
        [DllImport(User32, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool TrackMouseEvent(ref TRACKMOUSEEVENT lpEventTrack);

        // CsWin32 cannot generate GetWindowLongPtr/SetWindowLongPtr for AnyCPU
        // because LONG_PTR is pointer-sized.  The …PtrW exports only exist on
        // 64-bit Windows; 32-bit uses …LongW.
        [DllImport(User32, ExactSpelling = true, EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
        private static extern IntPtr GetWindowLongPtr64(HWND hWnd, WINDOW_LONG_PTR_INDEX nIndex);

        [DllImport(User32, ExactSpelling = true, EntryPoint = "GetWindowLongW", SetLastError = true)]
        private static extern IntPtr GetWindowLongPtr32(HWND hWnd, WINDOW_LONG_PTR_INDEX nIndex);

        internal static IntPtr GetWindowLongPtr(HWND hWnd, WINDOW_LONG_PTR_INDEX nIndex)
            => IntPtr.Size == 8
                ? GetWindowLongPtr64(hWnd, nIndex)
                : GetWindowLongPtr32(hWnd, nIndex);

        [DllImport(User32, ExactSpelling = true, EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr64(HWND hWnd, WINDOW_LONG_PTR_INDEX nIndex, IntPtr dwNewLong);

        [DllImport(User32, ExactSpelling = true, EntryPoint = "SetWindowLongW", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr32(HWND hWnd, WINDOW_LONG_PTR_INDEX nIndex, IntPtr dwNewLong);

        internal static IntPtr SetWindowLongPtr(HWND hWnd, WINDOW_LONG_PTR_INDEX nIndex, IntPtr dwNewLong)
            => IntPtr.Size == 8
                ? SetWindowLongPtr64(hWnd, nIndex, dwNewLong)
                : SetWindowLongPtr32(hWnd, nIndex, dwNewLong);
    }
}
