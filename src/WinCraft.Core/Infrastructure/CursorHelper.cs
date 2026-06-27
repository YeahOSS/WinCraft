using System.Drawing;
using System.Windows.Input;
using System.Windows.Interop;
using Windows.Win32;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;

namespace WinCraft.Infrastructure
{
    /// <summary>
    /// Helpers for creating WPF cursors from bitmaps and icons.
    /// </summary>
    internal static class CursorHelper
    {
        /// <summary>
        /// Creates a WPF <see cref="Cursor"/> from a bitmap with the given hotspot.
        /// </summary>
        public static Cursor CreateCursorFromBitmap(Bitmap bitmap, Point hotspot)
        {
            using var icon = Icon.FromHandle(bitmap.GetHicon());
            unsafe
            {
                var hIcon = new HICON(icon.Handle);
                ICONINFO info;
                PInvoke.GetIconInfo(hIcon, &info);
                info.xHotspot = (uint)hotspot.X;
                info.yHotspot = (uint)hotspot.Y;
                info.fIcon = false;

                var hCursor = PInvoke.CreateIconIndirect(info);

                // Free the color and mask bitmaps created by GetIconInfo.
                if (info.hbmColor != default)
                    PInvoke.DeleteObject((HGDIOBJ)info.hbmColor);
                if (info.hbmMask != default)
                    PInvoke.DeleteObject((HGDIOBJ)info.hbmMask);

                return CursorInteropHelper.Create(hCursor);
            }
        }
    }
}
