using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using WinCraft.Infrastructure;
using WinCraft.Infrastructure.Shell.DragDrop;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;

namespace WinCraft.UI
{
    /// <summary>
    /// Shared base for WinCraft windows: DWM effects (backdrop, border color,
    /// dark mode, corner preference), startup erase fill, and window-button
    /// capability gates.  <see cref="SystemWindow"/> keeps the native chrome;
    /// <see cref="ChromeWindow"/> replaces it.
    /// </summary>
    public abstract partial class WindowBase : Window
    {
        public static readonly DependencyProperty BackdropTypeProperty =
            DependencyProperty.Register(nameof(BackdropType), typeof(WindowBackdropType), typeof(WindowBase),
                new PropertyMetadata(WindowBackdropType.Auto, OnBackdropChanged));

        /// <summary>DWM border color mode (Win11+).</summary>
        public static readonly DependencyProperty DwmBorderColorModeProperty =
            DependencyProperty.Register(nameof(DwmBorderColorMode), typeof(DwmColorMode), typeof(WindowBase),
                new PropertyMetadata(DwmColorMode.Default, OnDwmAppearanceChanged));

        /// <summary>DWM custom border color (Win11+, used when DwmBorderColorMode=Custom).  Opaque only — COLORREF has no alpha channel.</summary>
        public static readonly DependencyProperty DwmBorderColorProperty =
            DependencyProperty.Register(nameof(DwmBorderColor), typeof(Color), typeof(WindowBase),
                new PropertyMetadata(SystemColors.ActiveBorderColor, OnDwmAppearanceChanged),
                IsOpaqueColor);

        /// <summary>Whether DWM should use its immersive dark-mode appearance.</summary>
        public static readonly DependencyProperty DwmUseDarkModeProperty =
            DependencyProperty.Register(nameof(DwmUseDarkMode), typeof(bool), typeof(WindowBase),
                new PropertyMetadata(false, OnDwmAppearanceChanged));

        /// <summary>Acrylic fallback tint, including opacity.</summary>
        public static readonly DependencyProperty AcrylicGradientColorProperty =
            DependencyProperty.Register(nameof(AcrylicGradientColor), typeof(Color), typeof(WindowBase),
                new PropertyMetadata(Color.FromArgb(0xCC, 0xFE, 0xFF, 0xFF), OnDwmAppearanceChanged));

        /// <summary>
        /// Optional opaque color used while Windows erases the client area before WPF renders.
        /// When unset, an opaque <see cref="Window.Background"/> solid color is used.
        /// </summary>
        public static readonly DependencyProperty EraseBackgroundColorProperty =
            DependencyProperty.Register(nameof(EraseBackgroundColor), typeof(Color?), typeof(WindowBase),
                new PropertyMetadata(null));

        /// <summary>DWM corner rounding preference (Win11+; square below).</summary>
        public static readonly DependencyProperty DwmCornerPreferenceProperty =
            DependencyProperty.Register(nameof(DwmCornerPreference), typeof(DwmCornerPreference), typeof(WindowBase),
                new PropertyMetadata(DwmCornerPreference.Default, OnDwmAppearanceChanged));

        /// <summary>Whether the minimize capability is available: button, taskbar command, Win+Down.</summary>
        public static readonly DependencyProperty ShowMinimizeProperty =
            DependencyProperty.Register(nameof(ShowMinimize), typeof(bool), typeof(WindowBase),
                new PropertyMetadata(true, OnWindowButtonCapabilityChanged));

        /// <summary>Whether the maximize capability is available: button, title-bar double-click, Snap, Win+Up.  Restoring an already maximized window stays allowed.</summary>
        public static readonly DependencyProperty ShowMaximizeProperty =
            DependencyProperty.Register(nameof(ShowMaximize), typeof(bool), typeof(WindowBase),
                new PropertyMetadata(true, OnWindowButtonCapabilityChanged));

        /// <summary>Whether the close capability is available: button and Alt+F4.  Programmatic <see cref="Window.Close"/> is unaffected.</summary>
        public static readonly DependencyProperty ShowCloseProperty =
            DependencyProperty.Register(nameof(ShowClose), typeof(bool), typeof(WindowBase),
                new PropertyMetadata(true));

        /// <summary>Whether the window icon is visible in the title bar.</summary>
        public static readonly DependencyProperty ShowIconProperty =
            DependencyProperty.Register(nameof(ShowIcon), typeof(bool), typeof(WindowBase),
                new PropertyMetadata(true, OnShowIconChanged));

        /// <summary>
        /// When <see langword="true"/>, keeps the DWM-managed window visuals
        /// in their active state after deactivation. This includes Mica intensity,
        /// shadow, and the native caption of <see cref="SystemWindow"/>.
        /// </summary>
        public static readonly DependencyProperty KeepActiveVisualsProperty =
            DependencyProperty.Register(nameof(KeepActiveVisuals), typeof(bool), typeof(WindowBase),
                new PropertyMetadata(false));

        /// <summary>
        /// Whether the help button is enabled.  <see cref="ChromeWindow"/> shows
        /// its template help button; <see cref="SystemWindow"/> enables the
        /// native <c>WS_EX_CONTEXTHELP</c> button.  Requests route through
        /// <see cref="HelpCommand"/> / <see cref="HelpRequested"/>.
        /// </summary>
        public static readonly DependencyProperty ShowHelpProperty =
            DependencyProperty.Register(nameof(ShowHelp), typeof(bool), typeof(WindowBase),
                new PropertyMetadata(false, OnShowHelpChanged));

        private static readonly DependencyPropertyKey IsBackdropActivePropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(IsBackdropActive), typeof(bool), typeof(WindowBase),
                new PropertyMetadata(false));

        public static readonly DependencyProperty IsBackdropActiveProperty = IsBackdropActivePropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey IsWindowActivePropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(IsWindowActive), typeof(bool), typeof(WindowBase),
                new PropertyMetadata(true));

        public static readonly DependencyProperty IsWindowActiveProperty = IsWindowActivePropertyKey.DependencyProperty;

        /// <summary>
        /// Interactive move/resize state, driven by the WM_ENTERSIZEMOVE modal
        /// loop.  Programmatic size changes, maximize/restore, and Win+Arrow
        /// snaps stay <see cref="SizeMoveState.None"/>.
        /// </summary>
        private static readonly DependencyPropertyKey SizeMoveStatePropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(SizeMoveState), typeof(SizeMoveState), typeof(WindowBase),
                new PropertyMetadata(SizeMoveState.None));

        public static readonly DependencyProperty SizeMoveStateProperty = SizeMoveStatePropertyKey.DependencyProperty;

        public WindowBackdropType BackdropType
        {
            get => (WindowBackdropType)GetValue(BackdropTypeProperty);
            set => SetValue(BackdropTypeProperty, value);
        }

        public DwmColorMode DwmBorderColorMode
        {
            get => (DwmColorMode)GetValue(DwmBorderColorModeProperty);
            set => SetValue(DwmBorderColorModeProperty, value);
        }

        public Color DwmBorderColor
        {
            get => (Color)GetValue(DwmBorderColorProperty);
            set => SetValue(DwmBorderColorProperty, value);
        }

        public bool DwmUseDarkMode
        {
            get => (bool)GetValue(DwmUseDarkModeProperty);
            set => SetValue(DwmUseDarkModeProperty, value);
        }

        public Color AcrylicGradientColor
        {
            get => (Color)GetValue(AcrylicGradientColorProperty);
            set => SetValue(AcrylicGradientColorProperty, value);
        }

        public Color? EraseBackgroundColor
        {
            get => (Color?)GetValue(EraseBackgroundColorProperty);
            set => SetValue(EraseBackgroundColorProperty, value);
        }

        public DwmCornerPreference DwmCornerPreference
        {
            get => (DwmCornerPreference)GetValue(DwmCornerPreferenceProperty);
            set => SetValue(DwmCornerPreferenceProperty, value);
        }

        public bool ShowMinimize
        {
            get => (bool)GetValue(ShowMinimizeProperty);
            set => SetValue(ShowMinimizeProperty, value);
        }

        public bool ShowMaximize
        {
            get => (bool)GetValue(ShowMaximizeProperty);
            set => SetValue(ShowMaximizeProperty, value);
        }

        public bool ShowClose
        {
            get => (bool)GetValue(ShowCloseProperty);
            set => SetValue(ShowCloseProperty, value);
        }

        public bool ShowIcon
        {
            get => (bool)GetValue(ShowIconProperty);
            set => SetValue(ShowIconProperty, value);
        }

        public bool KeepActiveVisuals
        {
            get => (bool)GetValue(KeepActiveVisualsProperty);
            set => SetValue(KeepActiveVisualsProperty, value);
        }

        public bool ShowHelp
        {
            get => (bool)GetValue(ShowHelpProperty);
            set => SetValue(ShowHelpProperty, value);
        }

        public bool IsBackdropActive => (bool)GetValue(IsBackdropActiveProperty);

        /// <summary>
        /// Whether the window is currently the active (foreground) window.
        /// Used to dim the title bar when the window is inactive.
        /// </summary>
        public bool IsWindowActive => (bool)GetValue(IsWindowActiveProperty);

        public ICommand MinimizeCommand { get; }
        public ICommand MaximizeRestoreCommand { get; }
        public ICommand CloseCommand { get; }

        public SizeMoveState SizeMoveState => (SizeMoveState)GetValue(SizeMoveStateProperty);

        /// <summary>
        /// Help topic token for context help.  Inherits down the element tree;
        /// the value at the help-click position becomes the
        /// <see cref="HelpCommand"/> parameter and
        /// <see cref="HelpRequestedEventArgs.Topic"/>.
        /// </summary>
        public static readonly DependencyProperty HelpTopicProperty =
            DependencyProperty.RegisterAttached(
                "HelpTopic",
                typeof(object),
                typeof(WindowBase),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));

        public static object GetHelpTopic(DependencyObject obj) => obj.GetValue(HelpTopicProperty);

        public static void SetHelpTopic(DependencyObject obj, object value) => obj.SetValue(HelpTopicProperty, value);

        /// <summary>
        /// Command executed when a help request is triggered.  The parameter is
        /// the nearest inherited <see cref="HelpTopicProperty"/> value at the
        /// click position, or <see langword="null"/> — never a view object.
        /// </summary>
        public static readonly DependencyProperty HelpCommandProperty =
            DependencyProperty.Register(
                nameof(HelpCommand),
                typeof(ICommand),
                typeof(WindowBase));

        public ICommand HelpCommand
        {
            get => (ICommand)GetValue(HelpCommandProperty);
            set => SetValue(HelpCommandProperty, value);
        }

        /// <summary>
        /// Raised on a help request when no <see cref="HelpCommand"/> is bound,
        /// or the command refuses execution.
        /// <see cref="HelpRequestedEventArgs.Element"/> is the WPF control at
        /// the click position, or <see langword="null"/>.
        /// </summary>
        public event EventHandler<HelpRequestedEventArgs> HelpRequested;

        /// <summary>
        /// Hit-tests the element at a screen point (device pixels), normalizes
        /// it to its templated control, and routes the help request.
        /// </summary>
        private protected void DispatchHelpRequestAt(int screenX, int screenY)
        {
            DispatchHelpRequest(ResolveHelpElement(
                InputHitTest(PointFromScreen(new Point(screenX, screenY))) as FrameworkElement));
        }

        /// <summary>Routes a help request: the inherited help topic goes to <see cref="HelpCommand"/>, falling back to <see cref="HelpRequested"/>.</summary>
        private protected void DispatchHelpRequest(FrameworkElement element)
        {
            object topic = GetHelpTopic(element ?? (DependencyObject)this);

            var command = HelpCommand;
            if (command != null && command.CanExecute(topic))
            {
                command.Execute(topic);
                return;
            }

            HelpRequested?.Invoke(this, new HelpRequestedEventArgs(topic));
        }

        /// <summary>
        /// Normalizes a hit-test result to the control that owns it:
        /// ControlTemplate internals resolve to their templated control, while
        /// DataTemplate content (whose TemplatedParent is a ContentPresenter,
        /// not a Control) stays as authored.
        /// </summary>
        internal static FrameworkElement ResolveHelpElement(FrameworkElement hit)
        {
            FrameworkElement element = hit;
            while (element?.TemplatedParent is Control control)
                element = control;
            return element;
        }

        private protected IntPtr _hwnd;
        private protected HwndSource _hwndSource;
        private protected bool _isDwmEnabled;

        private protected bool IsZoomed =>
            _hwnd != IntPtr.Zero
                ? PInvoke.IsZoomed((HWND)_hwnd)
                : WindowState == WindowState.Maximized;

        /// <summary>Maximize is gated by <see cref="ShowMaximize"/>; restoring an already maximized window stays allowed.</summary>
        private protected bool CanMaximizeRestore =>
            IsResizable(ResizeMode)
                && (WindowState == WindowState.Maximized || ShowMaximize);

        private protected static bool IsResizable(ResizeMode resizeMode)
        {
            return resizeMode == ResizeMode.CanResize
                || resizeMode == ResizeMode.CanResizeWithGrip;
        }

        static WindowBase()
        {
            ResizeModeProperty.OverrideMetadata(
                typeof(WindowBase),
                new FrameworkPropertyMetadata(OnResizeModeChanged));
        }

        private protected WindowBase()
        {
            FocusAdornerManager.Attach(this);

            MinimizeCommand = new RelayCommand(() =>
            {
                if (ShowMinimize) WindowState = WindowState.Minimized;
            });
            MaximizeRestoreCommand = new RelayCommand(() =>
            {
                if (!CanMaximizeRestore) return;
                WindowState = WindowState == WindowState.Maximized
                    ? WindowState.Normal : WindowState.Maximized;
            });
            CloseCommand = new RelayCommand(Close);

            // Default F1 handler on the standard help command: only claimed
            // while the help pipeline has a consumer, and inner bindings
            // override it by routed-command precedence.
            CommandBindings.Add(new CommandBinding(
                ApplicationCommands.Help, OnHelpCommandExecuted, OnHelpCommandCanExecute));
        }

        private void OnHelpCommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = HelpCommand != null || HelpRequested != null;
        }

        private void OnHelpCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            // DefWindowProc may turn the unhandled KeyDown into the WM_HELP
            // that follows; the squelch must be set before Dispatch (which may
            // block inside a modal dialog) so the nested WM_HELP doesn't get
            // through.
            _helpSquelch = true;
            DispatchHelpRequest(
                ResolveHelpElement(Keyboard.FocusedElement as FrameworkElement) ?? this);
            e.Handled = true;
        }

        private bool _helpSquelch;

        // ── Context Help (hand-written loop, shared by both subclasses) ──
        private protected bool _isHelpMode;
        private Cursor _previousCursor;

        private protected void EnterHelpMode()
        {
            _isHelpMode = true;
            _previousCursor = Cursor;
            Cursor = Cursors.Help;
            Mouse.Capture(this);
        }

        private void ExitHelpMode()
        {
            _isHelpMode = false;
            Cursor = _previousCursor;
            _previousCursor = null;
            Mouse.Capture(null);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            _hwndSource = (HwndSource)PresentationSource.FromVisual(this);
            if (_hwndSource == null) return;
            _hwnd = _hwndSource.Handle;
            _hwndSource.AddHook(WndProc);
            OnHandleReady();
            ApplyShowIcon();
            EnableShellDragVisual();

            _isDwmEnabled = DwmCorner.IsDwmAvailable();
            UpdateBackdropSurface(_isDwmEnabled && BackdropType != WindowBackdropType.None);
            ApplyDwmFrame();
            ApplyWindowButtonStyle();
            ApplyDwmEffects();
            RefreshNonClientFrame();
            BeginStartupPositionCorrection();
        }

        private void BeginStartupPositionCorrection()
        {
            if (WindowStartupLocation != WindowStartupLocation.CenterScreen
                && WindowStartupLocation != WindowStartupLocation.CenterOwner)
            {
                return;
            }

            ApplyStartupPosition(true);
            Loaded += OnStartupPositionLoaded;
        }

        private void OnStartupPositionLoaded(object sender, RoutedEventArgs e)
        {
            ApplyStartupPosition(false);
            EndStartupPositionCorrection();
        }

        private void EndStartupPositionCorrection()
        {
            Loaded -= OnStartupPositionLoaded;
        }

        private void ApplyStartupPosition(bool synchronizeDpi)
        {
            switch (WindowStartupLocation)
            {
                case WindowStartupLocation.CenterScreen:
                    ApplyCenterScreenPosition(new WindowInteropHelper(this).Owner, synchronizeDpi);
                    break;
                case WindowStartupLocation.CenterOwner:
                    ApplyCenterOwnerPosition(synchronizeDpi);
                    break;
            }
        }

        private void ApplyCenterScreenPosition(IntPtr owner, bool synchronizeDpi)
        {
            if (!PInvoke.GetWindowRect((HWND)_hwnd, out RECT windowBounds)
                || !TryGetMonitorWorkArea(GetCenterScreenMonitor(owner), out RECT workArea))
            {
                return;
            }

            int width = windowBounds.right - windowBounds.left;
            int height = windowBounds.bottom - windowBounds.top;
            SetStartupPosition(
                windowBounds,
                CenterCoordinate(workArea.left, workArea.right, width),
                CenterCoordinate(workArea.top, workArea.bottom, height),
                synchronizeDpi);
        }

        private void ApplyCenterOwnerPosition(bool synchronizeDpi)
        {
            var owner = new WindowInteropHelper(this).Owner;
            if (owner == IntPtr.Zero)
                return;

            if (Owner == null || Owner.WindowState != WindowState.Normal)
            {
                ApplyCenterScreenPosition(owner, synchronizeDpi);
                return;
            }

            if (!PInvoke.GetWindowRect((HWND)_hwnd, out RECT windowBounds)
                || !PInvoke.GetWindowRect((HWND)owner, out RECT ownerBounds)
                || !TryGetMonitorWorkArea(
                    PInvoke.MonitorFromWindow(
                        (HWND)owner,
                        MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST),
                    out RECT workArea))
            {
                return;
            }

            int width = windowBounds.right - windowBounds.left;
            int height = windowBounds.bottom - windowBounds.top;
            int x = ConstrainCoordinate(
                CenterCoordinate(ownerBounds.left, ownerBounds.right, width),
                workArea.left,
                workArea.right,
                width);
            int y = ConstrainCoordinate(
                CenterCoordinate(ownerBounds.top, ownerBounds.bottom, height),
                workArea.top,
                workArea.bottom,
                height);
            SetStartupPosition(windowBounds, x, y, synchronizeDpi);
        }

        private void SetStartupPosition(RECT windowBounds, int x, int y, bool synchronizeDpi)
        {
            if (windowBounds.left == x && windowBounds.top == y)
                return;

            var flags = SET_WINDOW_POS_FLAGS.SWP_NOZORDER
                | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE;

            if (!synchronizeDpi)
            {
                PInvoke.SetWindowPos(
                    (HWND)_hwnd,
                    HWND.Null,
                    x,
                    y,
                    0,
                    0,
                    flags | SET_WINDOW_POS_FLAGS.SWP_NOSIZE);
                return;
            }

            int width = windowBounds.right - windowBounds.left;
            int height = windowBounds.bottom - windowBounds.top;

            // The first device-pixel nudge makes WPF apply the destination
            // monitor's DPI; the second writes the intended physical bounds.
            // Without the nudge, later shell Snap operations can use stale
            // WPF size conversions after a cross-DPI startup move.
            PInvoke.SetWindowPos(
                (HWND)_hwnd,
                HWND.Null,
                x + 1,
                y,
                Math.Max(1, width - 1),
                height,
                flags);
            PInvoke.SetWindowPos(
                (HWND)_hwnd,
                HWND.Null,
                x,
                y,
                width,
                height,
                flags);
        }

        private static bool TryGetMonitorWorkArea(HMONITOR monitor, out RECT workArea)
        {
            workArea = default;
            if (monitor.IsNull)
                return false;

            var monitorInfo = new MONITORINFO
            {
                cbSize = (uint)Marshal.SizeOf(typeof(MONITORINFO)),
            };
            if (!PInvoke.GetMonitorInfo(monitor, ref monitorInfo))
                return false;

            workArea = monitorInfo.rcWork;
            return true;
        }

        private static int CenterCoordinate(int start, int end, int length)
        {
            return (int)Math.Round(
                start + (end - start - length) / 2.0,
                MidpointRounding.AwayFromZero);
        }

        private static int ConstrainCoordinate(int coordinate, int start, int end, int length)
        {
            return Math.Max(start, Math.Min(coordinate, end - length));
        }

        private static HMONITOR GetCenterScreenMonitor(IntPtr owner)
        {
            if (owner != IntPtr.Zero)
            {
                return PInvoke.MonitorFromWindow(
                    (HWND)owner,
                    MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);
            }

            if (!PInvoke.GetCursorPos(out System.Drawing.Point cursor))
                return default;

            return PInvoke.MonitorFromPoint(
                cursor,
                MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);
        }

        /// <summary>Called once the HWND exists and the hook is attached, before DWM effects apply.</summary>
        private protected virtual void OnHandleReady()
        {
        }

        /// <summary>
        /// Hooks tunneling preview drag events so every drag operation over
        /// this window automatically renders the Shell drag image (copy/move/
        /// link icon + drop description).  Child controls receive bubbling
        /// events unmodified — no <c>e.Handled</c> is set here.
        /// </summary>
        private void EnableShellDragVisual()
        {
            PreviewDragEnter += (_, e) =>
                ShellDropTarget.OnDragEnter(new ShellDragEventArgs(e));
            PreviewDragOver += (_, e) =>
                ShellDropTarget.OnDragOver(new ShellDragEventArgs(e));
            PreviewDragLeave += (_, _) =>
                ShellDropTarget.OnDragLeave();
            PreviewDrop += (_, e) =>
            {
                ShellDropTarget.OnDragLeave();
                ShellDropTarget.OnDrop(new ShellDragEventArgs(e));
            };
        }

        private protected void ApplyDwmEffects()
        {
            if (!_isDwmEnabled) return;

            // DWM windows default to a light frame. Set this before the
            // system backdrop so Mica/Acrylic's first composed frame uses
            // the window's intended color mode.
            DwmDarkMode.Apply(_hwnd, DwmUseDarkMode);
            DwmBackdrop.Apply(
                _hwnd,
                ToDwmBackdropType(BackdropType),
                ToAccentColor(AcrylicGradientColor));
            if (WindowsVersion.IsAtLeast(WindowsRelease.Win11_21H2))
            {
                DwmCorner.Apply(_hwnd, ToDwmCornerPreference(DwmCornerPreference));
                ApplyDwmColors();
            }
        }

        /// <summary>Apply Win11+ DWM colors.  Base sets the border color only.</summary>
        private protected virtual void ApplyDwmColors()
        {
            if (!WindowsVersion.IsAtLeast(WindowsRelease.Win11_21H2)) return;

            DwmWindowAttribute.Set(
                _hwnd,
                DWMWINDOWATTRIBUTE.DWMWA_BORDER_COLOR,
                ToDwmColorRef(DwmBorderColorMode, DwmBorderColor));
        }

        /// <summary>Resolve a mode + custom color pair to the COLORREF DWM expects.</summary>
        internal static int ToDwmColorRef(DwmColorMode mode, Color custom)
        {
            return mode switch
            {
                DwmColorMode.None => unchecked((int)PInvoke.DWMWA_COLOR_NONE),
                DwmColorMode.Custom => unchecked((int)ToColorRef(custom)),
                _ => unchecked((int)PInvoke.DWMWA_COLOR_DEFAULT),
            };
        }

        private protected static uint ToColorRef(Color c) => (uint)(c.B << 16 | c.G << 8 | c.R);

        /// <summary>Converts a WPF color to the AABBGGRR format used by Accent Policy.</summary>
        internal static int ToAccentColor(Color color) =>
            unchecked((int)((uint)color.A << 24 | (uint)color.B << 16 | (uint)color.G << 8 | color.R));

        /// <summary>DWM COLORREF colors have no alpha channel; reject translucent values instead of silently dropping alpha.</summary>
        private protected static bool IsOpaqueColor(object value) =>
            value is Color color && color.A == byte.MaxValue;

        internal static DWM_WINDOW_CORNER_PREFERENCE ToDwmCornerPreference(DwmCornerPreference preference)
        {
            return preference switch
            {
                DwmCornerPreference.DoNotRound => DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_DONOTROUND,
                DwmCornerPreference.Round => DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND,
                DwmCornerPreference.RoundSmall => DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUNDSMALL,
                _ => DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_DEFAULT,
            };
        }

        private bool TryGetEraseBackgroundColor(out Color color)
        {
            if (EraseBackgroundColor.HasValue)
            {
                color = EraseBackgroundColor.Value;
                return color.A == byte.MaxValue;
            }

            if (Background is SolidColorBrush background)
            {
                color = background.Color;
                return color.A == byte.MaxValue;
            }

            color = default;
            return false;
        }

        private protected static void OnDwmAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var w = (WindowBase)d;
            if (w._hwnd != IntPtr.Zero && w._isDwmEnabled)
                w.ApplyDwmEffects();
        }

        private static void OnResizeModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var w = (WindowBase)d;
            if (w._hwnd == IntPtr.Zero)
                return;

            w.ApplyWindowButtonStyle();
            if (w.IsZoomed)
                return;

            w.RefreshNonClientFrame();
        }

        private static void OnWindowButtonCapabilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var w = (WindowBase)d;
            if (w._hwnd != IntPtr.Zero)
                w.ApplyWindowButtonStyle();
        }

        private static void OnShowHelpChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var w = (WindowBase)d;
            if (w._hwnd != IntPtr.Zero)
                w.ApplyShowHelp();
        }

        private static void OnShowIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var w = (WindowBase)d;
            if (w._hwnd != IntPtr.Zero)
                w.ApplyShowIcon();
        }

        /// <summary>Subclass hook applying <see cref="ShowIcon"/> to native chrome. ChromeWindow's template binds the property directly.</summary>
        private protected virtual void ApplyShowIcon()
        {
        }

        /// <summary>Subclass hook applying <see cref="ShowHelp"/>.  ChromeWindow's template binds the property directly; SystemWindow toggles the native ex-style.</summary>
        private protected virtual void ApplyShowHelp()
        {
        }

        private static void OnBackdropChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var w = (WindowBase)d;
            var newType = (WindowBackdropType)e.NewValue;
            if (w._hwnd == IntPtr.Zero) return;

            w.UpdateBackdropSurface(w._isDwmEnabled && newType != WindowBackdropType.None);
            if (!w._isDwmEnabled) return;

            w.ApplyDwmEffects();
            w.ApplyDwmFrame();
            w.RefreshNonClientFrame();
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            SetValue(IsWindowActivePropertyKey, true);
        }

        protected override void OnDeactivated(EventArgs e)
        {
            base.OnDeactivated(e);
            SetValue(IsWindowActivePropertyKey, false);
        }

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);

            if (_hwnd == IntPtr.Zero || !_isDwmEnabled || WindowState == WindowState.Minimized)
                return;

            // Frame extension depends on the current native window geometry.
            // Reapply it after maximize and restore so DWM does not retain the
            // normal-state client boundary.
            ApplyDwmFrame();
            RefreshNonClientFrame();
        }

        private void UpdateBackdropSurface(bool isActive)
        {
            SetValue(IsBackdropActivePropertyKey, isActive);
            if (_hwndSource?.CompositionTarget != null)
                _hwndSource.CompositionTarget.BackgroundColor = isActive
                    ? Colors.Transparent
                    : SystemColors.WindowColor;
        }

        private static DwmBackdropType ToDwmBackdropType(WindowBackdropType type)
        {
            return type switch
            {
                WindowBackdropType.None => DwmBackdropType.None,
                WindowBackdropType.Mica => DwmBackdropType.Mica,
                WindowBackdropType.MicaAlt => DwmBackdropType.MicaAlt,
                WindowBackdropType.Acrylic => DwmBackdropType.Acrylic,
                _ => DwmBackdropType.Auto,
            };
        }
    }
}
