using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using WinCraft.Infrastructure.Diagnostics;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;
using Icon = System.Drawing.Icon;
using SystemIcons = System.Drawing.SystemIcons;

namespace WinCraft.UI
{
    internal sealed class TrayIcon : FrameworkElement, IDisposable
    {
        public static readonly DependencyProperty ToolTipTextProperty =
            DependencyProperty.Register(
                nameof(ToolTipText),
                typeof(string),
                typeof(TrayIcon),
                new PropertyMetadata(string.Empty, OnToolTipTextChanged));

        private const uint IconId = 1;
        private const uint CallbackMessage = PInvoke.WM_USER + 1;
        private const uint NinSelect = PInvoke.WM_USER;
        private const uint NinKeySelect = PInvoke.WM_USER + 1;
        private const string TaskbarCreatedMessageName = "TaskbarCreated";

        private readonly HwndSource _source;
        private readonly Icon _icon;
        private readonly uint _taskbarCreatedMessage;
        private bool _isAdded;
        private bool _isDisposed;

        public event EventHandler Click;

        public string ToolTipText
        {
            get => (string)GetValue(ToolTipTextProperty);
            set => SetValue(ToolTipTextProperty, value);
        }

        public TrayIcon()
        {
            var parameters = new HwndSourceParameters(nameof(TrayIcon), 0, 0)
            {
                WindowStyle = 0
            };
            _source = new HwndSource(parameters);
            _icon = LoadApplicationIcon();
            _taskbarCreatedMessage = PInvoke.RegisterWindowMessage(TaskbarCreatedMessageName);
            _source.AddHook(WindowMessageHook);
            AddIcon();
            Dispatcher.ShutdownStarted += DispatcherShutdownStarted;
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            Dispatcher.ShutdownStarted -= DispatcherShutdownStarted;
            RemoveIcon();
            _source.RemoveHook(WindowMessageHook);
            _source.Dispose();
            _icon.Dispose();
        }

        private static void OnToolTipTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TrayIcon)d).UpdateIcon();
        }

        private IntPtr WindowMessageHook(
            IntPtr hwnd,
            int message,
            IntPtr wParam,
            IntPtr lParam,
            ref bool handled)
        {
            uint windowMessage = unchecked((uint)message);
            if (_taskbarCreatedMessage != 0 && windowMessage == _taskbarCreatedMessage)
            {
                AddIcon();
                return IntPtr.Zero;
            }

            if (windowMessage != CallbackMessage)
                return IntPtr.Zero;

            handled = true;
            uint notification = unchecked((uint)lParam.ToInt64()) & ushort.MaxValue;

            switch (notification)
            {
                case PInvoke.WM_LBUTTONUP:
                case NinSelect:
                case NinKeySelect:
                    Click?.Invoke(this, EventArgs.Empty);
                    break;

                case PInvoke.WM_CONTEXTMENU:
                    ShowContextMenu();
                    break;
            }

            return IntPtr.Zero;
        }

        private void AddIcon()
        {
            if (_isDisposed)
                return;

            var data = CreateData();
            if (!PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_ADD, ref data))
            {
                Log.Warn("Failed to add the notification-area icon.");
                return;
            }

            _isAdded = true;
            data.uVersion = PInvoke.NOTIFYICON_VERSION_4;
            if (!PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_SETVERSION, ref data))
                Log.Warn("Failed to set the notification-area icon version.");
        }

        private void UpdateIcon()
        {
            if (!_isAdded || _isDisposed)
                return;

            var data = CreateData();
            if (!PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_MODIFY, ref data))
                Log.Warn("Failed to update the notification-area icon.");
        }

        private void RemoveIcon()
        {
            if (!_isAdded)
                return;

            var data = CreateData();
            PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_DELETE, ref data);
            _isAdded = false;
        }

        private NOTIFYICONDATA CreateData()
        {
            return new NOTIFYICONDATA
            {
                cbSize = unchecked((uint)Marshal.SizeOf(typeof(NOTIFYICONDATA))),
                hWnd = new HWND(_source.Handle),
                uID = IconId,
                uFlags = NOTIFY_ICON_DATA_FLAGS.NIF_MESSAGE
                    | NOTIFY_ICON_DATA_FLAGS.NIF_ICON
                    | NOTIFY_ICON_DATA_FLAGS.NIF_TIP
                    | NOTIFY_ICON_DATA_FLAGS.NIF_SHOWTIP,
                uCallbackMessage = CallbackMessage,
                hIcon = new HICON(_icon.Handle),
                szTip = ToolTipText
            };
        }

        private void ShowContextMenu()
        {
            var contextMenu = ContextMenu;
            if (contextMenu == null || contextMenu.IsOpen)
                return;

            // Bring the handler window to the front so the menu doesn't appear behind the taskbar.
            PInvoke.SetForegroundWindow(new HWND(_source.Handle));

            contextMenu.Placement = PlacementMode.MousePoint;
            contextMenu.Closed += ContextMenuClosed;
            contextMenu.IsOpen = true;
        }

        private void ContextMenuClosed(object sender, RoutedEventArgs e)
        {
            var contextMenu = sender as ContextMenu;
            if (contextMenu != null)
                contextMenu.Closed -= ContextMenuClosed;

            if (!_isAdded || _isDisposed)
                return;

            var data = CreateData();
            PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_SETFOCUS, ref data);
        }

        private void DispatcherShutdownStarted(object sender, EventArgs e)
        {
            Dispose();
        }

        private static Icon LoadApplicationIcon()
        {
            try
            {
                using (var process = Process.GetCurrentProcess())
                {
                    var icon = Icon.ExtractAssociatedIcon(process.MainModule.FileName);
                    if (icon != null)
                        return icon;
                }
            }
            catch (Exception ex)
            {
                Log.Warn($"Failed to load the application icon: {ex.Message}");
            }

            return (Icon)SystemIcons.Application.Clone();
        }
    }
}
