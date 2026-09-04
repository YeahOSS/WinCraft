using System;
using System.Windows;
using System.Windows.Media;
using WinCraft.Infrastructure;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;

namespace WinCraft.UI
{
    /// <summary>
    /// Window with system-provided chrome: the native title bar, frame, and
    /// caption buttons stay untouched while DWM enhancements from
    /// <see cref="WindowBase"/> apply.  The DWM caption colors only render on
    /// this class — <see cref="ChromeWindow"/> removes the native caption.
    /// </summary>
    public partial class SystemWindow : WindowBase
    {
        private bool _iconStyleInitialized;
        private bool _hadDialogModalFrame;
        private bool _iconsHidden;
        private bool _isUpdatingIconVisibility;
        private IntPtr _hiddenSmallIcon;
        private IntPtr _hiddenBigIcon;

        static SystemWindow()
        {
            ShowCloseProperty.OverrideMetadata(
                typeof(SystemWindow), new PropertyMetadata(true, OnShowCloseChanged));
        }

        /// <summary>DWM caption color mode (Win11+). With an active backdrop, <see cref="DwmColorMode.None"/> extends it through the native title bar.</summary>
        public static readonly DependencyProperty DwmCaptionColorModeProperty =
            DependencyProperty.Register(nameof(DwmCaptionColorMode), typeof(DwmColorMode), typeof(SystemWindow),
                new PropertyMetadata(DwmColorMode.Default, OnDwmAppearanceChanged));

        /// <summary>DWM caption color (Win11+, used when DwmCaptionColorMode=Custom).  Opaque only — COLORREF has no alpha channel.</summary>
        public static readonly DependencyProperty DwmCaptionColorProperty =
            DependencyProperty.Register(nameof(DwmCaptionColor), typeof(Color), typeof(SystemWindow),
                new PropertyMetadata(Colors.White, OnDwmAppearanceChanged),
                IsOpaqueColor);

        /// <summary>DWM caption-text color mode (Win11+). <see cref="DwmColorMode.None"/> is coerced to <see cref="DwmColorMode.Default"/> because DWM does not define a no-text state.</summary>
        public static readonly DependencyProperty DwmTextColorModeProperty =
            DependencyProperty.Register(nameof(DwmTextColorMode), typeof(DwmColorMode), typeof(SystemWindow),
                new PropertyMetadata(DwmColorMode.Default, OnDwmAppearanceChanged, CoerceTextColorMode));

        /// <summary>DWM caption-text color (Win11+, used when DwmTextColorMode=Custom).  Opaque only — COLORREF has no alpha channel.</summary>
        public static readonly DependencyProperty DwmTextColorProperty =
            DependencyProperty.Register(nameof(DwmTextColor), typeof(Color), typeof(SystemWindow),
                new PropertyMetadata(Colors.Black, OnDwmAppearanceChanged),
                IsOpaqueColor);

        public DwmColorMode DwmCaptionColorMode
        {
            get => (DwmColorMode)GetValue(DwmCaptionColorModeProperty);
            set => SetValue(DwmCaptionColorModeProperty, value);
        }

        public Color DwmCaptionColor
        {
            get => (Color)GetValue(DwmCaptionColorProperty);
            set => SetValue(DwmCaptionColorProperty, value);
        }

        public DwmColorMode DwmTextColorMode
        {
            get => (DwmColorMode)GetValue(DwmTextColorModeProperty);
            set => SetValue(DwmTextColorModeProperty, value);
        }

        private static object CoerceTextColorMode(DependencyObject d, object baseValue)
        {
            return (DwmColorMode)baseValue == DwmColorMode.None
                ? DwmColorMode.Default
                : baseValue;
        }

        public Color DwmTextColor
        {
            get => (Color)GetValue(DwmTextColorProperty);
            set => SetValue(DwmTextColorProperty, value);
        }

        public SystemWindow()
        {
            SetResourceReference(StyleProperty, typeof(SystemWindow));
        }

        private protected override void OnHandleReady()
        {
            ApplyHelpButtonStyle();
            UpdateCloseMenuItem();
        }

        private static void OnShowCloseChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var w = (SystemWindow)d;
            if (w._hwnd != IntPtr.Zero)
                w.UpdateCloseMenuItem();
        }

        /// <summary>
        /// Applies <see cref="WindowBase.ShowHelp"/> as the native help button
        /// (<c>WS_EX_CONTEXTHELP</c> → <c>WM_HELP</c>).  Win32 only renders it
        /// when the minimize/maximize boxes are absent — combine with
        /// <see cref="WindowBase.ShowMinimize"/>/<see cref="WindowBase.ShowMaximize"/>
        /// set to <see langword="false"/> or <see cref="ResizeMode.NoResize"/>.
        /// </summary>
        private protected override void ApplyShowHelp()
        {
            ApplyHelpButtonStyle();
            RefreshNonClientFrame();
        }

        /// <summary>
        /// Hides the native caption icon. WS_EX_DLGMODALFRAME prevents the
        /// window class icon from being used as a fallback after the two icons
        /// associated with the HWND are cleared.
        /// </summary>
        private protected override void ApplyShowIcon()
        {
            var hwnd = (HWND)_hwnd;
            int exStyle = PInvoke.GetWindowLongPtr(
                hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE).ToInt32();
            int dialogModalFrame = (int)WINDOW_EX_STYLE.WS_EX_DLGMODALFRAME;

            if (!_iconStyleInitialized)
            {
                _iconStyleInitialized = true;
                _hadDialogModalFrame = (exStyle & dialogModalFrame) != 0;
            }

            int newExStyle = !ShowIcon || _hadDialogModalFrame
                ? exStyle | dialogModalFrame
                : exStyle & ~dialogModalFrame;
            bool visibilityChanged = ShowIcon ? _iconsHidden : !_iconsHidden;
            if (newExStyle == exStyle && !visibilityChanged)
                return;

            if (newExStyle != exStyle)
            {
                PInvoke.SetWindowLongPtr(
                    hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, new IntPtr(newExStyle));
            }

            _isUpdatingIconVisibility = true;
            try
            {
                if (ShowIcon)
                {
                    SetNativeIcon(hwnd, PInvoke.ICON_SMALL, _hiddenSmallIcon);
                    SetNativeIcon(hwnd, PInvoke.ICON_BIG, _hiddenBigIcon);
                    _iconsHidden = false;
                }
                else
                {
                    _hiddenSmallIcon = SetNativeIcon(hwnd, PInvoke.ICON_SMALL, IntPtr.Zero);
                    _hiddenBigIcon = SetNativeIcon(hwnd, PInvoke.ICON_BIG, IntPtr.Zero);
                    _iconsHidden = true;
                }
            }
            finally
            {
                _isUpdatingIconVisibility = false;
            }

            RefreshNonClientFrame();
            unsafe
            {
                PInvoke.RedrawWindow(
                    hwnd,
                    null,
                    default(HRGN),
                    REDRAW_WINDOW_FLAGS.RDW_INVALIDATE | REDRAW_WINDOW_FLAGS.RDW_FRAME);
            }
        }

        private static IntPtr SetNativeIcon(HWND hwnd, uint iconType, IntPtr icon)
        {
            LRESULT previous = PInvoke.SendMessage(
                hwnd, PInvoke.WM_SETICON, new WPARAM(iconType), new LPARAM(icon));
            return new IntPtr(previous.Value);
        }

        private void ApplyHelpButtonStyle()
        {
            int exStyle = PInvoke.GetWindowLongPtr(
                (HWND)_hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE).ToInt32();
            int newExStyle = ShowHelp
                ? exStyle | (int)WINDOW_EX_STYLE.WS_EX_CONTEXTHELP
                : exStyle & ~(int)WINDOW_EX_STYLE.WS_EX_CONTEXTHELP;
            if (newExStyle != exStyle)
                PInvoke.SetWindowLongPtr(
                    (HWND)_hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, new IntPtr(newExStyle));
        }

        private protected override void ApplyDwmColors()
        {
            base.ApplyDwmColors();

            DwmWindowAttribute.Set(_hwnd, DWMWINDOWATTRIBUTE.DWMWA_CAPTION_COLOR,
                ToDwmColorRef(DwmCaptionColorMode, DwmCaptionColor));
            DwmWindowAttribute.Set(_hwnd, DWMWINDOWATTRIBUTE.DWMWA_TEXT_COLOR,
                ToDwmColorRef(DwmTextColorMode, DwmTextColor));
        }
    }
}
