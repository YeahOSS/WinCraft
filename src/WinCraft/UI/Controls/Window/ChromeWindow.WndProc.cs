using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using WinCraft.Compatibility;
using WinCraft.Infrastructure;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;

namespace WinCraft.UI
{
    public partial class ChromeWindow
    {
        // Undocumented messages sent by the DWM theme engine to draw themed
        // window borders.  Block them to prevent drawing over the client area.
        // These are NOT defined by CsWin32 — use raw values.
        private const uint WM_NCUAHDRAWCAPTION = 0x00AE;
        private const uint WM_NCUAHDRAWFRAME = 0x00AF;

        private bool _themeEnabled;
        private RECT _rgn;
        private bool _rgnValid;

        private protected override IntPtr OnWndProc(int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch ((uint)msg)
            {
                case PInvoke.WM_GETMINMAXINFO:
                    HandleGetMinMaxInfo(lParam);
                    break;

                case PInvoke.WM_NCCALCSIZE:
                    return HandleNcCalcSize(wParam, lParam, ref handled);

                case PInvoke.WM_NCPAINT:
                    HandleNcPaint(ref handled);
                    break;

                case PInvoke.WM_NCACTIVATE:
                    return HandleNcActivate(wParam, ref handled);

                case PInvoke.WM_NCHITTEST:
                    return HandleNcHitTest(_hwnd, lParam, ref handled);

                case PInvoke.WM_NCLBUTTONDOWN:
                    return HandleNcLButtonDown(wParam, ref handled);

                case PInvoke.WM_NCLBUTTONUP:
                    return HandleNcLButtonUp(wParam, ref handled);

                case PInvoke.WM_NCLBUTTONDBLCLK:
                    return HandleNcLButtonDblClk(wParam, ref handled);

                case PInvoke.WM_NCMOUSELEAVE:
                    return HandleNcMouseLeave();

                case PInvoke.WM_GETTITLEBARINFOEX:
                    return HandleGetTitleBarInfoEx(lParam, ref handled);

                case WM_NCUAHDRAWCAPTION:
                case WM_NCUAHDRAWFRAME:
                    // Block theme-drawn borders unconditionally — the native
                    // caption is already suppressed via SetWindowThemeAttribute,
                    // and when the frame overlaps the client area these would
                    // otherwise paint over WPF content.
                    handled = true;
                    return IntPtr.Zero;

                case PInvoke.WM_SETICON:
                case PInvoke.WM_SETTEXT:
                    return HandleSetIconOrText((uint)msg, wParam, lParam, ref handled);

                case PInvoke.WM_THEMECHANGED:
                    return HandleThemeChanged();

                case PInvoke.WM_DWMCOMPOSITIONCHANGED:
                    return HandleDwmCompositionChanged(msg, wParam, lParam, ref handled);

                case PInvoke.WM_WINDOWPOSCHANGED:
                    return HandleWindowPosChanged(lParam);

                case PInvoke.WM_LBUTTONDOWN:
                    return HandleLButtonDownForDrag(lParam, ref handled);

                default:
                    return base.OnWndProc(msg, wParam, lParam, ref handled);
            }
            return IntPtr.Zero;
        }

        // ── GETMINMAXINFO ──

        private void HandleGetMinMaxInfo(IntPtr lParam)
        {
            var mmi = MarshalCompat.PtrToStructure<MINMAXINFO>(lParam);
            var monitor = PInvoke.MonitorFromWindow(
                (HWND)_hwnd,
                MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);
            if (!monitor.IsNull)
            {
                var mi = new MONITORINFO
                {
                    cbSize = (uint)Marshal.SizeOf(typeof(MONITORINFO)),
                };
                if (PInvoke.GetMonitorInfo(monitor, ref mi))
                {
                    mmi.ptMaxPosition.X = mi.rcWork.left - mi.rcMonitor.left;
                    mmi.ptMaxPosition.Y = mi.rcWork.top - mi.rcMonitor.top;
                    mmi.ptMaxSize.X = mi.rcWork.right - mi.rcWork.left;
                    mmi.ptMaxSize.Y = mi.rcWork.bottom - mi.rcWork.top;
                }
            }

            MarshalCompat.StructureToPtr(mmi, lParam, false);
        }

        // ═════════════════════════════════════════════════════════════
        //  NCCALCSIZE — two strategies, switched by IsResizeFrameInClient
        // ═════════════════════════════════════════════════════════════

        private IntPtr HandleNcCalcSize(IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (IsResizeFrameInClient)
                return HandleNcCalcSizeEdgeToEdge(wParam, lParam, ref handled);

            // System mode: reserve the native resize frame.
            var clientRect = MarshalCompat.PtrToStructure<RECT>(lParam);
            ApplyNonClientInsets(ref clientRect);
            MarshalCompat.StructureToPtr(clientRect, lParam, false);
            handled = true;
            return wParam == IntPtr.Zero
                ? IntPtr.Zero
                : (IntPtr)(int)(PInvoke.WVR_HREDRAW | PInvoke.WVR_VREDRAW);
        }

        /// <summary>System mode frame insets — client shrinks to make room for invisible borders.</summary>
        private void ApplyNonClientInsets(ref RECT clientRect)
        {
            if (WindowsVersion.IsBelow(WindowsRelease.Win8)) return;

            if (IsResizable(ResizeMode))
            {
                GetEffectiveResizeFrameThickness(
                    out int left, out int top, out int right, out int bottom);
                clientRect.left += left;
                clientRect.top += top;
                clientRect.right -= right;
                clientRect.bottom -= bottom;
            }
            else
            {
                clientRect.top += 1;
            }
        }

        /// <summary>
        /// Client-mode NCCALCSIZE: the client area covers the full window
        /// rect in the normal state (minus a 1 px top anchor to prevent drag
        /// jitter).  Maximized windows use the monitor work area directly
        /// (simpler than the auto-hide appbar hack — see rossy/borderless-window#3).
        /// </summary>
        private IntPtr HandleNcCalcSizeEdgeToEdge(IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (wParam != IntPtr.Zero)
            {
                var nccsp = MarshalCompat.PtrToStructure<NCCALCSIZE_PARAMS>(lParam);
                RECT originalRect = nccsp.rgrc._0;

                PInvoke.DefWindowProc(
                    (HWND)_hwnd,
                    PInvoke.WM_NCCALCSIZE,
                    new WPARAM((nuint)(int)wParam),
                    new LPARAM(lParam));

                if (!WindowsVersion.IsBelow(WindowsRelease.Win8) && IsZoomed)
                {
                    var monitor = PInvoke.MonitorFromRect(
                        nccsp.rgrc._0,
                        MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);
                    if (!monitor.IsNull)
                    {
                        var mi = new MONITORINFO
                        {
                            cbSize = (uint)Marshal.SizeOf(typeof(MONITORINFO)),
                        };
                        if (PInvoke.GetMonitorInfo(monitor, ref mi))
                        {
                            nccsp.rgrc._0 = mi.rcWork;
                        }
                    }
                }
                else
                {
                    // Normal state: client area = window rect (zero inset),
                    // but reserve a 1 px band at the top so the system
                    // never misclassifies the title bar during a drag.
                    nccsp.rgrc._0 = originalRect;
                    if (!WindowsVersion.IsBelow(WindowsRelease.Win8))
                        nccsp.rgrc._0.top += 1;
                }

                MarshalCompat.StructureToPtr(nccsp, lParam, false);
            }
            else
            {
                var original = MarshalCompat.PtrToStructure<RECT>(lParam);
                PInvoke.DefWindowProc(
                    (HWND)_hwnd,
                    PInvoke.WM_NCCALCSIZE,
                    new WPARAM((nuint)(int)wParam),
                    new LPARAM(lParam));
                if (!WindowsVersion.IsBelow(WindowsRelease.Win8))
                    original.top += 1;
                MarshalCompat.StructureToPtr(original, lParam, false);
            }

            handled = true;
            return wParam != IntPtr.Zero
                ? (IntPtr)(int)(PInvoke.WVR_HREDRAW | PInvoke.WVR_VREDRAW)
                : IntPtr.Zero;
        }

        // ── NCPAINT ──

        private void HandleNcPaint(ref bool handled)
        {
            if (IsResizeFrameInClient)
            {
                if (!_isDwmEnabled)
                    handled = true;
            }
            else if (WindowsVersion.IsBelow(WindowsRelease.Win8))
            {
                handled = true;
            }
        }

        // ── NCACTIVATE ──

        private IntPtr HandleNcActivate(IntPtr wParam, ref bool handled)
        {
            if (WindowState == WindowState.Minimized)
                return IntPtr.Zero;

            // KeepActiveVisuals makes DWM render the non-client area as active
            // after deactivation. This is required to preserve Mica intensity;
            // older backdrops do not react to activation state.
            if (KeepActiveVisuals
                && WindowsVersion.IsAtLeast(WindowsRelease.Win11_21H2))
            {
                handled = true;
                return PInvoke.DefWindowProc(
                    (HWND)_hwnd,
                    PInvoke.WM_NCACTIVATE,
                    new WPARAM(1u),
                    new LPARAM(-1));
            }

            if (IsResizeFrameInClient)
            {
                handled = true;
                return PInvoke.DefWindowProc(
                    (HWND)_hwnd,
                    PInvoke.WM_NCACTIVATE,
                    new WPARAM((nuint)(int)wParam),
                    new LPARAM(-1));
            }

            if (WindowsVersion.IsBelow(WindowsRelease.Win8))
            {
                handled = true;
                return (IntPtr)1;
            }
            return IntPtr.Zero;
        }

        // ═════════════════════════════════════════════════════════════
        //  NCHITTEST — two strategies, switched by IsResizeFrameInClient
        // ═════════════════════════════════════════════════════════════

        private IntPtr HandleNcHitTest(IntPtr hwnd, IntPtr lParam, ref bool handled)
        {
            int x = Windowsx.GetXLParam(lParam);
            int y = Windowsx.GetYLParam(lParam);

            // ── Resize edge detection ──
            if (IsResizeFrameInClient)
            {
                if (!IsZoomed)
                {
                    int resizeHitTest = GetPixelResizeHitTest(hwnd, x, y);
                    if (resizeHitTest != 0)
                    {
                        ClearNcHover();
                        handled = true;
                        return (IntPtr)resizeHitTest;
                    }
                }
            }
            else
            {
                int resizeHitTest = GetResizeHitTest(hwnd, x, y);
                if (resizeHitTest != 0)
                {
                    ClearNcHover();
                    handled = true;
                    return (IntPtr)resizeHitTest;
                }
            }

            // ── Title bar / caption button hit testing (shared) ──
            if (TryGetTitleBarScreenRect(out RECT titleBarBounds)
                && x >= titleBarBounds.left
                && x < titleBarBounds.right
                && y >= titleBarBounds.top
                && y < titleBarBounds.bottom)
            {
                var pt = PointFromScreen(new Point(x, y));
                DependencyObject owner = GetHitTestOwner(InputHitTest(pt), out WindowHitTestRole role);
                int hitTest = GetNonClientHitTest(role);

                bool isCaptionButton = IsCaptionButtonHitTest(hitTest);
                UpdateNcHover(isCaptionButton ? owner : null);
                if (isCaptionButton)
                    RequestNcMouseLeave();
                handled = true;
                return (IntPtr)(hitTest == 0 ? (int)PInvoke.HTCAPTION : hitTest);
            }

            ClearNcHover();
            return IntPtr.Zero;
        }

        // ── System-mode resize hit-test (rect comparison) ──

        internal int GetResizeHitTest(IntPtr hwnd, int x, int y)
        {
            bool canResize = IsResizable(ResizeMode);
            if (!canResize || WindowState != WindowState.Normal)
                return 0;

            PInvoke.GetWindowRect((HWND)hwnd, out RECT windowBounds);
            int resizeHitTest;
            if (WindowsVersion.IsBelow(WindowsRelease.Win8))
            {
                resizeHitTest = GetResizeHitTest(
                    x, y, windowBounds,
                    GetLegacyResizeBounds(hwnd, windowBounds));
            }
            else
            {
                if (!TryGetClientScreenBounds(hwnd, out RECT clientBounds))
                    return 0;
                resizeHitTest = GetResizeHitTest(x, y, windowBounds, clientBounds);
            }

            return resizeHitTest;
        }

        private static RECT GetLegacyResizeBounds(IntPtr hwnd, RECT windowBounds)
        {
            if (DwmWindowAttribute.TryGetExtendedFrameBounds(hwnd, out RECT frameBounds)
                && IsResizeBand(windowBounds, frameBounds))
            {
                return frameBounds;
            }

            return GetOnePixelInteriorBounds(windowBounds);
        }

        private static bool IsResizeBand(RECT outerBounds, RECT innerBounds)
        {
            return innerBounds.left >= outerBounds.left
                && innerBounds.top >= outerBounds.top
                && innerBounds.right <= outerBounds.right
                && innerBounds.bottom <= outerBounds.bottom
                && innerBounds.left < innerBounds.right
                && innerBounds.top < innerBounds.bottom
                && (innerBounds.left > outerBounds.left
                    || innerBounds.top > outerBounds.top
                    || innerBounds.right < outerBounds.right
                    || innerBounds.bottom < outerBounds.bottom);
        }

        private static RECT GetOnePixelInteriorBounds(RECT outerBounds)
        {
            return new RECT
            {
                left = outerBounds.left + 1,
                top = outerBounds.top + 1,
                right = outerBounds.right - 1,
                bottom = outerBounds.bottom - 1,
            };
        }

        internal static int GetResizeHitTest(int x, int y, RECT outerBounds, RECT innerBounds)
        {
            if (innerBounds.left < outerBounds.left
                || innerBounds.top < outerBounds.top
                || innerBounds.right > outerBounds.right
                || innerBounds.bottom > outerBounds.bottom)
                return 0;

            bool left = x >= outerBounds.left && x < innerBounds.left;
            bool right = x >= innerBounds.right && x < outerBounds.right;
            bool top = y >= outerBounds.top && y < innerBounds.top;
            bool bottom = y >= innerBounds.bottom && y < outerBounds.bottom;

            if (top && left) return (int)PInvoke.HTTOPLEFT;
            if (top && right) return (int)PInvoke.HTTOPRIGHT;
            if (bottom && left) return (int)PInvoke.HTBOTTOMLEFT;
            if (bottom && right) return (int)PInvoke.HTBOTTOMRIGHT;
            if (top) return (int)PInvoke.HTTOP;
            if (bottom) return (int)PInvoke.HTBOTTOM;
            if (left) return (int)PInvoke.HTLEFT;
            if (right) return (int)PInvoke.HTRIGHT;
            return 0;
        }

        // ── Client-mode resize hit-test (pixel-based edge detection) ──

        /// <summary>
        /// Pixel-based resize edge detection using system metrics.
        /// Returns the appropriate HT* code or 0 if not on a resize edge.
        /// </summary>
        private int GetPixelResizeHitTest(IntPtr hwnd, int screenX, int screenY)
        {
            bool canResize = IsResizable(ResizeMode);
            if (!canResize) return 0;

            PInvoke.GetWindowRect((HWND)hwnd, out RECT wr);
            uint dpi = WindowsVersion.IsAtLeast(WindowsRelease.Win10_1607)
                ? PInvoke.GetDpiForWindow((HWND)hwnd)
                : 0;

            int frameX = GetSystemMetric(SYSTEM_METRICS_INDEX.SM_CXFRAME, dpi)
                       + GetSystemMetric(SYSTEM_METRICS_INDEX.SM_CXPADDEDBORDER, dpi);
            int frameY = GetSystemMetric(SYSTEM_METRICS_INDEX.SM_CYSIZEFRAME, dpi)
                       + GetSystemMetric(SYSTEM_METRICS_INDEX.SM_CXPADDEDBORDER, dpi);

            int w = wr.right - wr.left;
            int h = wr.bottom - wr.top;
            int x = screenX - wr.left;
            int y = screenY - wr.top;

            bool top = y < frameY;
            bool bottom = y >= h - frameY;
            bool left = x < frameX;
            bool right = x >= w - frameX;

            if (top && left) return (int)PInvoke.HTTOPLEFT;
            if (top && right) return (int)PInvoke.HTTOPRIGHT;
            if (bottom && left) return (int)PInvoke.HTBOTTOMLEFT;
            if (bottom && right) return (int)PInvoke.HTBOTTOMRIGHT;
            if (top) return (int)PInvoke.HTTOP;
            if (bottom) return (int)PInvoke.HTBOTTOM;
            if (left) return (int)PInvoke.HTLEFT;
            if (right) return (int)PInvoke.HTRIGHT;
            return 0;
        }

        // ── System-mode frame thickness helpers ──

        internal static int GetClientTopInset(bool isMaximized, int resizeBorderHeight)
        {
            return isMaximized ? resizeBorderHeight : 1;
        }

        internal void GetEffectiveResizeFrameThickness(
            out int left, out int top, out int right, out int bottom)
        {
            if (_hwnd == IntPtr.Zero)
            {
                left = top = right = bottom = 0;
                return;
            }

            var custom = ResizeFrameThickness;
            double scale = GetDpiScale();

            GetResizeBorderThickness((HWND)_hwnd, out int sysH, out int sysV);

            left = double.IsNaN(custom.Left)
                ? sysH : (int)(custom.Left * scale + 0.5);
            top = double.IsNaN(custom.Top)
                ? GetClientTopInset(IsZoomed, sysV) : (int)(custom.Top * scale + 0.5);
            right = double.IsNaN(custom.Right)
                ? sysH : (int)(custom.Right * scale + 0.5);
            bottom = double.IsNaN(custom.Bottom)
                ? sysV : (int)(custom.Bottom * scale + 0.5);
        }

        private static void GetResizeBorderThickness(HWND hwnd, out int horizontal, out int vertical)
        {
            uint dpi = WindowsVersion.IsAtLeast(WindowsRelease.Win10_1607)
                ? PInvoke.GetDpiForWindow(hwnd)
                : 0;
            horizontal = GetSystemMetric(SYSTEM_METRICS_INDEX.SM_CXSIZEFRAME, dpi);
            vertical = GetSystemMetric(SYSTEM_METRICS_INDEX.SM_CYSIZEFRAME, dpi);
            if (WindowsVersion.IsAtLeast(WindowsRelease.Win8))
            {
                int padding = GetSystemMetric(SYSTEM_METRICS_INDEX.SM_CXPADDEDBORDER, dpi);
                horizontal += padding;
                vertical += padding;
            }
            horizontal = Math.Max(1, horizontal);
            vertical = Math.Max(1, vertical);
        }

        private static int GetSystemMetric(SYSTEM_METRICS_INDEX metric, uint dpi)
        {
            return dpi != 0
                ? PInvoke.GetSystemMetricsForDpi(metric, dpi)
                : PInvoke.GetSystemMetrics(metric);
        }

        // ── SETICON / SETTEXT (classic theme only) ──

        /// <summary>
        /// Classic theme (non-DWM) workaround: temporarily remove WS_VISIBLE
        /// before calling DefWindowProc to prevent it from drawing a caption
        /// over the client area.
        /// </summary>
        private IntPtr HandleSetIconOrText(uint msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (_isDwmEnabled || _themeEnabled)
                return IntPtr.Zero;

            var hwnd = (HWND)_hwnd;
            int oldStyle = PInvoke.GetWindowLongPtr(
                hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE).ToInt32();

            PInvoke.SetWindowLongPtr(
                hwnd,
                WINDOW_LONG_PTR_INDEX.GWL_STYLE,
                new IntPtr(oldStyle & ~(int)WINDOW_STYLE.WS_VISIBLE));
            LRESULT result = PInvoke.DefWindowProc(
                hwnd, msg,
                new WPARAM((nuint)(int)wParam),
                new LPARAM(lParam));
            PInvoke.SetWindowLongPtr(
                hwnd,
                WINDOW_LONG_PTR_INDEX.GWL_STYLE,
                new IntPtr(oldStyle));

            handled = true;
            return (IntPtr)result.Value;
        }

        // ── Window Region (client mode only) ──

        /// <summary>
        /// Manage the window region when the frame overlaps the client area:
        ///   - Maximized: clip the non-client borders that hang over the screen edge.
        ///   - Classic theme: remove rounded top corners.
        ///   - DWM enabled, normal state: no region — preserve the shadow.
        /// </summary>
        private void UpdateRegion()
        {
            RECT newRgn;

            if (IsZoomed)
            {
                var wi = new WINDOWINFO
                {
                    cbSize = (uint)Marshal.SizeOf(typeof(WINDOWINFO)),
                };
                PInvoke.GetWindowInfo((HWND)_hwnd, ref wi);

                newRgn = new RECT
                {
                    left = wi.rcClient.left - wi.rcWindow.left,
                    top = wi.rcClient.top - wi.rcWindow.top,
                    right = wi.rcClient.right - wi.rcWindow.left,
                    bottom = wi.rcClient.bottom - wi.rcWindow.top,
                };
            }
            else if (!_isDwmEnabled)
            {
                newRgn = new RECT { left = 0, top = 0, right = 32767, bottom = 32767 };
            }
            else
            {
                newRgn = default;
            }

            if (_rgnValid
                && _rgn.left == newRgn.left
                && _rgn.top == newRgn.top
                && _rgn.right == newRgn.right
                && _rgn.bottom == newRgn.bottom)
                return;

            _rgn = newRgn;
            _rgnValid = true;

            if (newRgn.right == 0 && newRgn.bottom == 0)
            {
                PInvoke.SetWindowRgn((HWND)_hwnd, HRGN.Null, true);
            }
            else
            {
                var hrgn = PInvoke.CreateRectRgnIndirect(newRgn);
                PInvoke.SetWindowRgn((HWND)_hwnd, new HRGN(hrgn.DangerousGetHandle()), true);
            }
        }

        private IntPtr HandleLButtonDownForDrag(IntPtr lParam, ref bool handled)
        {
            if (!IsResizeFrameInClient || _isHelpMode)
                return IntPtr.Zero;

            if (!TryGetTitleBarScreenRect(out RECT tb))
                return IntPtr.Zero;

            if (Windowsx.GetXLParam(lParam) < tb.left
                || Windowsx.GetXLParam(lParam) >= tb.right
                || Windowsx.GetYLParam(lParam) < tb.top
                || Windowsx.GetYLParam(lParam) >= tb.bottom)
                return IntPtr.Zero;

            // When the resize frame lives in the client area, the
            // title-bar click must be routed to the system as HTCAPTION
            // so the modal move loop starts correctly.
            handled = true;
            BeginWindowMove();
            return IntPtr.Zero;
        }

        internal void BeginWindowMove()
        {
            if (_hwnd == IntPtr.Zero)
                return;

            PInvoke.ReleaseCapture();
            PInvoke.SendMessage(
                (HWND)_hwnd,
                PInvoke.WM_NCLBUTTONDOWN,
                new WPARAM(PInvoke.HTCAPTION),
                default);
        }

        private IntPtr HandleNcMouseLeave()
        {
            if (_isHelpMode) return IntPtr.Zero;
            ClearNcHover();
            ClearNcPressed();
            return IntPtr.Zero;
        }

        private IntPtr HandleThemeChanged()
        {
            if (IsResizeFrameInClient)
            {
                _themeEnabled = PInvoke.IsThemeActive();
                UpdateRegion();
            }
            return IntPtr.Zero;
        }

        private IntPtr HandleDwmCompositionChanged(int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            var result = base.OnWndProc(msg, wParam, lParam, ref handled);
            if (IsResizeFrameInClient)
                UpdateRegion();
            return result;
        }

        private IntPtr HandleWindowPosChanged(IntPtr lParam)
        {
            if (!IsResizeFrameInClient)
                return IntPtr.Zero;

            var pos = MarshalCompat.PtrToStructure<WINDOWPOS>(lParam);
            if ((pos.flags & SET_WINDOW_POS_FLAGS.SWP_FRAMECHANGED) != 0
                || IsZoomed)
            {
                UpdateRegion();
            }
            return IntPtr.Zero;
        }

        // ── NC button handlers ──

        private IntPtr HandleNcLButtonDown(IntPtr wParam, ref bool handled)
        {
            int ht = (int)wParam;
            if (ht == (int)PInvoke.HTMINBUTTON
                || ht == (int)PInvoke.HTMAXBUTTON
                || ht == (int)PInvoke.HTCLOSE
                || ht == (int)PInvoke.HTHELP)
            {
                handled = true;
                SetNcPressed();
            }

            return IntPtr.Zero;
        }

        private IntPtr HandleNcLButtonUp(IntPtr wParam, ref bool handled)
        {
            int ht = (int)wParam;
            if (ht == (int)PInvoke.HTMINBUTTON
                || ht == (int)PInvoke.HTMAXBUTTON
                || ht == (int)PInvoke.HTCLOSE
                || ht == (int)PInvoke.HTHELP)
            {
                handled = true;
                ClearNcPressed();

                if (ht == (int)PInvoke.HTMINBUTTON)
                    MinimizeCommand.Execute(null);
                else if (ht == (int)PInvoke.HTMAXBUTTON)
                    MaximizeRestoreCommand.Execute(null);
                else if (ht == (int)PInvoke.HTCLOSE)
                    CloseCommand.Execute(null);
                else
                    EnterHelpMode();
            }

            return IntPtr.Zero;
        }

        private IntPtr HandleNcLButtonDblClk(IntPtr wParam, ref bool handled)
        {
            int ht = (int)wParam;
            if (ht == (int)PInvoke.HTCAPTION && CanMaximizeRestore)
            {
                handled = true;
                MaximizeRestoreCommand.Execute(null);
            }

            return IntPtr.Zero;
        }

        private IntPtr HandleGetTitleBarInfoEx(IntPtr lParam, ref bool handled)
        {
            var max = FindHitTestElement(WindowHitTestRole.Maximize);
            if (max != null)
            {
                var info = MarshalCompat.PtrToStructure<TITLEBARINFOEX>(lParam);
                Point p1 = max.PointToScreen(new Point(0, 0));
                Point p2 = max.PointToScreen(new Point(max.ActualWidth, max.ActualHeight));
                info.rgrect._3 = new RECT
                {
                    left = (int)p1.X,
                    top = (int)p1.Y,
                    right = (int)p2.X,
                    bottom = (int)p2.Y,
                };
                MarshalCompat.StructureToPtr(info, lParam, true);
                handled = true;
            }
            return IntPtr.Zero;
        }

        // ── Hit test helpers ──

        internal static int GetNonClientHitTest(WindowHitTestRole role)
        {
            return role switch
            {
                WindowHitTestRole.Client => (int)PInvoke.HTCLIENT,
                WindowHitTestRole.Caption => (int)PInvoke.HTCAPTION,
                WindowHitTestRole.Minimize => (int)PInvoke.HTMINBUTTON,
                WindowHitTestRole.Maximize => (int)PInvoke.HTMAXBUTTON,
                WindowHitTestRole.Close => (int)PInvoke.HTCLOSE,
                WindowHitTestRole.Help => (int)PInvoke.HTHELP,
                _ => 0,
            };
        }

        private FrameworkElement FindHitTestElement(WindowHitTestRole role)
        {
            return WalkVisual(this);

            FrameworkElement WalkVisual(DependencyObject parent)
            {
                if (parent is FrameworkElement element
                    && element.IsVisible
                    && GetHitTestRole(element) == role)
                    return element;
                int count = VisualTreeHelper.GetChildrenCount(parent);
                for (int i = 0; i < count; i++)
                {
                    var r = WalkVisual(VisualTreeHelper.GetChild(parent, i));
                    if (r != null) return r;
                }
                return null;
            }
        }

        private static DependencyObject GetHitTestOwner(
            IInputElement hit,
            out WindowHitTestRole role)
        {
            if (hit is not DependencyObject owner)
            {
                role = WindowHitTestRole.None;
                return null;
            }

            role = GetHitTestRole(owner);
            if (role == WindowHitTestRole.None)
                return null;

            DependencyObject parent;
            while ((parent = GetHitTestParent(owner)) != null
                && GetHitTestRole(parent) == role)
            {
                owner = parent;
            }
            return owner;
        }

        private static DependencyObject GetHitTestParent(DependencyObject element)
        {
            if (element is FrameworkContentElement contentElement)
                return contentElement.Parent;

            return VisualTreeHelper.GetParent(element);
        }

        private static bool IsCaptionButtonHitTest(int hitTest)
        {
            return hitTest == (int)PInvoke.HTMINBUTTON
                || hitTest == (int)PInvoke.HTMAXBUTTON
                || hitTest == (int)PInvoke.HTCLOSE
                || hitTest == (int)PInvoke.HTHELP;
        }

        // ── Screen rect helpers ──

        private static RECT GetElementScreenRect(FrameworkElement element)
        {
            if (element == null) return default;
            Point topLeft = element.PointToScreen(new Point(0, 0));
            Point bottomRight = element.PointToScreen(new Point(element.ActualWidth, element.ActualHeight));
            return CreateScreenRect(topLeft, bottomRight);
        }

        internal static RECT CreateScreenRect(Point topLeft, Point bottomRight)
        {
            return new RECT
            {
                left = (int)Math.Floor(topLeft.X),
                top = (int)Math.Floor(topLeft.Y),
                right = (int)Math.Ceiling(bottomRight.X),
                bottom = (int)Math.Ceiling(bottomRight.Y),
            };
        }

        internal static RECT CreateTitleBarScreenRect(RECT clientBounds, int titleBarHeight)
        {
            return new RECT
            {
                left = clientBounds.left,
                top = clientBounds.top,
                right = clientBounds.right,
                bottom = clientBounds.top + titleBarHeight,
            };
        }

        private bool TryGetTitleBarScreenRect(out RECT bounds)
        {
            FrameworkElement titleBar = FindHitTestElement(WindowHitTestRole.Caption);
            if (titleBar != null
                && titleBar.ActualWidth > 0
                && titleBar.ActualHeight > 0)
            {
                bounds = GetElementScreenRect(titleBar);
                return true;
            }

            bounds = default;
            return false;
        }

        private static bool TryGetClientScreenBounds(IntPtr hwnd, out RECT bounds)
        {
            PInvoke.GetClientRect((HWND)hwnd, out RECT client);
            var topLeft = new System.Drawing.Point(client.left, client.top);
            var bottomRight = new System.Drawing.Point(client.right, client.bottom);
            if (!PInvoke.ClientToScreen((HWND)hwnd, ref topLeft)
                || !PInvoke.ClientToScreen((HWND)hwnd, ref bottomRight))
            {
                bounds = default;
                return false;
            }

            bounds = new RECT
            {
                left = topLeft.X,
                top = topLeft.Y,
                right = bottomRight.X,
                bottom = bottomRight.Y,
            };
            return true;
        }

        // ── Template client-area fix-up ──

        private void ScheduleTemplateClientAreaFixup()
        {
            LayoutUpdated += OnTemplateClientAreaLayoutUpdated;
        }

        private void OnTemplateClientAreaLayoutUpdated(object sender, EventArgs e)
        {
            if (!TryFixTemplateClientArea())
                return;

            LayoutUpdated -= OnTemplateClientAreaLayoutUpdated;
        }

        private bool TryFixTemplateClientArea()
        {
            if (_hwnd == IntPtr.Zero
                || _hwndSource == null
                || _hwndSource.CompositionTarget == null
                || VisualTreeHelper.GetChildrenCount(this) == 0)
                return false;

            if (VisualTreeHelper.GetChild(this, 0) is not FrameworkElement templateRoot
                || templateRoot.ActualWidth <= 0
                || templateRoot.ActualHeight <= 0)
                return false;

            PInvoke.GetClientRect((HWND)_hwnd, out RECT clientRect);
            var clientSize = _hwndSource.CompositionTarget.TransformFromDevice.Transform(
                new Point(clientRect.right - clientRect.left, clientRect.bottom - clientRect.top));

            Thickness fixedMargin = GetTemplateRootMarginForClientSize(
                templateRoot.Margin,
                new Size(templateRoot.ActualWidth, templateRoot.ActualHeight),
                new Size(clientSize.X, clientSize.Y));
            if (fixedMargin == templateRoot.Margin)
                return true;

            templateRoot.Margin = fixedMargin;
            return true;
        }

        internal static Thickness GetTemplateRootMarginForClientSize(
            Thickness templateMargin, Size arrangedSize, Size clientSize)
        {
            double missingWidth = Math.Max(0, clientSize.Width - arrangedSize.Width);
            double missingHeight = Math.Max(0, clientSize.Height - arrangedSize.Height);
            if (missingWidth <= 0.01 && missingHeight <= 0.01)
                return templateMargin;

            return new Thickness(
                templateMargin.Left,
                templateMargin.Top,
                templateMargin.Right - missingWidth,
                templateMargin.Bottom - missingHeight);
        }
    }
}
