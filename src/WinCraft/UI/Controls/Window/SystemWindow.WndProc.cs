using System;
using WinCraft.Infrastructure;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace WinCraft.UI
{
    public partial class SystemWindow
    {
        private protected override IntPtr OnWndProc(int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            return (uint)msg switch
            {
                PInvoke.WM_NCACTIVATE => HandleNcActivateForActiveVisuals(lParam, ref handled),
                PInvoke.WM_SETICON => HandleSetIcon(wParam, lParam, ref handled),
                _ => IntPtr.Zero,
            };
        }

        private IntPtr HandleSetIcon(IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (_isUpdatingIconVisibility || ShowIcon)
                return IntPtr.Zero;

            if (wParam.ToInt32() == (int)PInvoke.ICON_SMALL)
                _hiddenSmallIcon = lParam;
            else if (wParam.ToInt32() == (int)PInvoke.ICON_BIG)
                _hiddenBigIcon = lParam;
            else
                return IntPtr.Zero;

            // The actual icon is null while hidden, which is also the correct
            // WM_SETICON return value for callers updating Window.Icon.
            handled = true;
            return IntPtr.Zero;
        }

        private void UpdateCloseMenuItem()
        {
            var menu = PInvoke.GetSystemMenu((HWND)_hwnd, false);
            if (!menu.IsNull)
                PInvoke.EnableMenuItem(menu, PInvoke.SC_CLOSE,
                    ShowClose ? MENU_ITEM_FLAGS.MF_ENABLED : MENU_ITEM_FLAGS.MF_GRAYED);
        }

        private IntPtr HandleNcActivateForActiveVisuals(IntPtr lParam, ref bool handled)
        {
            // KeepActiveVisuals makes DWM retain the entire active visual
            // state, including the native caption, shadow, and Mica intensity.
            if (!KeepActiveVisuals
                || !WindowsVersion.IsAtLeast(WindowsRelease.Win11_21H2))
                return IntPtr.Zero;

            handled = true;
            return PInvoke.DefWindowProc(
                (HWND)_hwnd,
                PInvoke.WM_NCACTIVATE,
                new WPARAM(1u),
                new LPARAM(lParam));
        }
    }
}
