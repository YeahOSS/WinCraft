using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WinCraft.Infrastructure;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Controls;
using Windows.Win32.UI.WindowsAndMessaging;

namespace WinCraft.UI
{
    /// <summary>
    /// Window with custom chrome: the native caption is removed and replaced
    /// by the templated <see cref="TitleBar"/>, with hit-testing, resize
    /// frame, and Snap Layout handled through the WndProc hook.
    /// </summary>
    public partial class ChromeWindow : WindowBase
    {
        // ── HitTestRole (attached, inheritable) ──

        public static readonly DependencyProperty HitTestRoleProperty =
            DependencyProperty.RegisterAttached(
                "HitTestRole",
                typeof(WindowHitTestRole),
                typeof(ChromeWindow),
                new FrameworkPropertyMetadata(
                    WindowHitTestRole.None,
                    FrameworkPropertyMetadataOptions.Inherits));

        public static WindowHitTestRole GetHitTestRole(DependencyObject obj) =>
            (WindowHitTestRole)obj.GetValue(HitTestRoleProperty);

        public static void SetHitTestRole(DependencyObject obj, WindowHitTestRole value) =>
            obj.SetValue(HitTestRoleProperty, value);

        // ── NC hover / pressed (attached, read-only) ──

        internal static readonly DependencyPropertyKey IsNonClientHoveredKey =
            DependencyProperty.RegisterAttachedReadOnly("IsNonClientHovered", typeof(bool), typeof(ChromeWindow),
                new PropertyMetadata(false));

        public static readonly DependencyProperty IsNonClientHoveredProperty = IsNonClientHoveredKey.DependencyProperty;
        public static bool GetIsNonClientHovered(DependencyObject obj) => (bool)obj.GetValue(IsNonClientHoveredProperty);

        internal static readonly DependencyPropertyKey IsNonClientPressedKey =
            DependencyProperty.RegisterAttachedReadOnly("IsNonClientPressed", typeof(bool), typeof(ChromeWindow),
                new PropertyMetadata(false));

        public static readonly DependencyProperty IsNonClientPressedProperty = IsNonClientPressedKey.DependencyProperty;

        public static bool GetIsNonClientPressed(DependencyObject obj) => (bool)obj.GetValue(IsNonClientPressedProperty);

        // ── EffectiveIcon (read-only) ──

        private static readonly DependencyPropertyKey EffectiveIconPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(EffectiveIcon), typeof(ImageSource), typeof(ChromeWindow),
                new PropertyMetadata(null));
        public static readonly DependencyProperty EffectiveIconProperty = EffectiveIconPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyDescriptor IconPropertyDescriptor =
            DependencyPropertyDescriptor.FromProperty(IconProperty, typeof(Window));

        // ── Resize frame ──

        /// <summary>Custom resize frame thickness per edge in WPF units.  <see cref="double.NaN"/> components fall back to system metrics.  Ignored when <see cref="IsResizeFrameInClient"/> is <see langword="true"/>.</summary>
        public static readonly DependencyProperty ResizeFrameThicknessProperty =
            DependencyProperty.Register(
                nameof(ResizeFrameThickness),
                typeof(Thickness),
                typeof(ChromeWindow),
                new PropertyMetadata(
                    new Thickness(double.NaN),
                    OnResizePolicyChanged));

        /// <summary>
        /// When <see langword="true"/>, the resize frame overlaps the WPF
        /// client area — Window Width/Height matches the design spec exactly,
        /// but edge controls (scrollbars, WebView2) need padding to avoid
        /// resize-handle conflicts.  When <see langword="false"/> (default),
        /// the frame sits outside the client area like a standard Win32
        /// window — edge controls work naturally but Width/Height includes
        /// invisible borders.
        /// </summary>
        public static readonly DependencyProperty IsResizeFrameInClientProperty =
            DependencyProperty.Register(
                nameof(IsResizeFrameInClient),
                typeof(bool),
                typeof(ChromeWindow),
                new PropertyMetadata(false, OnResizePolicyChanged));

        // ── CLR properties ──

        public ImageSource EffectiveIcon => (ImageSource)GetValue(EffectiveIconProperty);

        public Thickness ResizeFrameThickness
        {
            get => (Thickness)GetValue(ResizeFrameThicknessProperty);
            set => SetValue(ResizeFrameThicknessProperty, value);
        }

        public bool IsResizeFrameInClient
        {
            get => (bool)GetValue(IsResizeFrameInClientProperty);
            set => SetValue(IsResizeFrameInClientProperty, value);
        }

        // ── Fields ──

        private DependencyObject _ncHovered;
        private DependencyObject _ncPressed;

        // ── Constructor ──

        public ChromeWindow()
        {
            SetResourceReference(StyleProperty, typeof(ChromeWindow));
            ResizeMode = ResizeMode.CanResize;
            IconPropertyDescriptor.AddValueChanged(this, OnIconChanged);
            Closed += OnClosed;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            if (_hwnd == IntPtr.Zero) return;

            ScheduleTemplateClientAreaFixup();
        }

        private protected override void OnHandleReady()
        {
            UpdateEffectiveIcon();

            unsafe
            {
                PInvoke.ChangeWindowMessageFilterEx(
                    (HWND)_hwnd, PInvoke.WM_GETTITLEBARINFOEX,
                    WINDOW_MESSAGE_FILTER_ACTION.MSGFLT_ALLOW, null);
            }

            SuppressNativeTitleBar();

            if (IsResizeFrameInClient)
            {
                EnsureAppWindowStyle();
                UpdateRegion();
            }
        }


        private void SuppressNativeTitleBar()
        {
            const uint flags = PInvoke.WTNCA_NODRAWCAPTION
                | PInvoke.WTNCA_NODRAWICON
                | PInvoke.WTNCA_NOSYSMENU;

            var options = new WTA_OPTIONS { dwFlags = flags, dwMask = flags };

            unsafe
            {
                PInvoke.SetWindowThemeAttribute(
                    (HWND)_hwnd,
                    WINDOWTHEMEATTRIBUTETYPE.WTA_NONCLIENT,
                    &options,
                    (uint)Marshal.SizeOf(typeof(WTA_OPTIONS)));
            }
        }

        private void EnsureAppWindowStyle()
        {
            int exStyle = PInvoke.GetWindowLongPtr(
                (HWND)_hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE).ToInt32();

            if ((exStyle & (int)WINDOW_EX_STYLE.WS_EX_APPWINDOW) == 0)
            {
                exStyle |= (int)WINDOW_EX_STYLE.WS_EX_APPWINDOW;
                PInvoke.SetWindowLongPtr(
                    (HWND)_hwnd,
                    WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE,
                    new IntPtr(exStyle));
            }
        }

        private protected override void ApplyDwmFrame()
        {
            if (!_isDwmEnabled) return;

            DwmCorner.EnableNcRendering(_hwnd);

            if (WindowsVersion.IsBelow(WindowsRelease.Win8))
                return;

            bool useFullGlass =
                WindowsVersion.IsAtLeast(WindowsRelease.Win11_21H2)
                && BackdropType != WindowBackdropType.None;

            // Full-glass (-1,-1,-1,-1) creates the backdrop render surface.
            // Otherwise a single 1 px frame extension on any edge is enough
            // to tell DWM "this window has a frame" so it renders the shadow.
            // The direction does not matter — the original borderless-window.c
            // uses {0, 0, 1, 0} (top=1).
            var margins = useFullGlass
                ? new MARGINS { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 }
                : new MARGINS { cyTopHeight = 1 };

            PInvoke.DwmExtendFrameIntoClientArea((HWND)_hwnd, in margins);
        }

        private static void OnResizePolicyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var w = (ChromeWindow)d;
            if (w._hwnd != IntPtr.Zero)
                w.RefreshNonClientFrame();
        }

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            ClearNcPressed();
            ClearNcHover();

            if (IsResizeFrameInClient)
                UpdateRegion();
        }

        // ── NC Hover / Pressed state ──

        private void UpdateNcHover(DependencyObject element)
        {
            if (element == _ncHovered) return;
            _ncHovered?.SetValue(IsNonClientHoveredKey, false);
            element?.SetValue(IsNonClientHoveredKey, true);
            _ncHovered = element;
        }

        private void SetNcPressed()
        {
            if (_ncHovered == null) return;
            _ncHovered.SetValue(IsNonClientPressedKey, true);
            _ncPressed = _ncHovered;
        }

        private void ClearNcHover()
        {
            if (_ncHovered == null) return;
            _ncHovered.SetValue(IsNonClientHoveredKey, false);
            _ncHovered = null;
        }

        private void ClearNcPressed()
        {
            if (_ncPressed == null) return;
            _ncPressed.SetValue(IsNonClientPressedKey, false);
            _ncPressed = null;
        }

        private void RequestNcMouseLeave()
        {
            var tme = new TRACKMOUSEEVENT
            {
                cbSize = (uint)Marshal.SizeOf(typeof(TRACKMOUSEEVENT)),
                dwFlags = TRACKMOUSEEVENT_FLAGS.TME_NONCLIENT,
                hwndTrack = _hwnd,
            };
            PInvoke.TrackMouseEvent(ref tme);
        }

        // ── Window button capabilities ──

        private protected override void ApplyWindowButtonStyle()
        {
            base.ApplyWindowButtonStyle();

            if (_hwnd == IntPtr.Zero)
                return;

            int style = PInvoke.GetWindowLongPtr(
                (HWND)_hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE).ToInt32();
            int newStyle = style & ~(int)WINDOW_STYLE.WS_SYSMENU;
            if (newStyle != style)
                PInvoke.SetWindowLongPtr(
                    (HWND)_hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE, new IntPtr(newStyle));
        }

        // ── Window icon ──

        private void OnIconChanged(object sender, EventArgs e)
        {
            UpdateEffectiveIcon();
        }

        private void OnClosed(object sender, EventArgs e)
        {
            IconPropertyDescriptor.RemoveValueChanged(this, OnIconChanged);
            Closed -= OnClosed;
        }

        private void UpdateEffectiveIcon()
        {
            if (Icon != null)
            {
                SetValue(EffectiveIconPropertyKey, Icon);
                return;
            }

            if (_hwnd == IntPtr.Zero)
            {
                SetValue(EffectiveIconPropertyKey, null);
                return;
            }

            IntPtr icon = GetNativeSmallIcon((HWND)_hwnd);
            var source = Imaging.CreateBitmapSourceFromHIcon(
                icon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            if (source.CanFreeze)
                source.Freeze();

            SetValue(EffectiveIconPropertyKey, source);
        }

        private static IntPtr GetNativeSmallIcon(HWND hwnd)
        {
            LRESULT result = PInvoke.SendMessage(
                hwnd, PInvoke.WM_GETICON, new WPARAM(PInvoke.ICON_SMALL2), default);
            IntPtr icon = new(result.Value);
            if (icon != IntPtr.Zero)
                return icon;

            icon = PInvoke.GetClassLongPtr(hwnd, GET_CLASS_LONG_INDEX.GCLP_HICONSM);
            if (icon != IntPtr.Zero)
                return icon;

            icon = PInvoke.GetClassLongPtr(hwnd, GET_CLASS_LONG_INDEX.GCLP_HICON);
            return icon != IntPtr.Zero ? icon : System.Drawing.SystemIcons.Application.Handle;
        }
    }
}
