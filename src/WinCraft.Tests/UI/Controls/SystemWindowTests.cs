using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NUnit.Framework;
using WinCraft.UI;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;

namespace WinCraft.Tests.UI.Controls
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    internal sealed class SystemWindowTests
    {
        [Test]
        public void SystemWindow_ThemeAppearanceResources_AreDynamic()
        {
            var dictionary = (ResourceDictionary)Application.LoadComponent(
                new Uri("/WinCraft;component/UI/Theme/Controls.Window.xaml", UriKind.Relative));
            var window = new SystemWindow();
            var caption = Color.FromRgb(0x12, 0x34, 0x56);
            var text = Color.FromRgb(0x65, 0x43, 0x21);

            window.Resources["WindowDwmUseDarkMode"] = true;
            window.Resources["WindowDwmCaptionColor"] = caption;
            window.Resources["WindowDwmTextColor"] = text;
            window.Style = (Style)dictionary[typeof(SystemWindow)];

            Assert.That(window.DwmUseDarkMode, Is.True);
            Assert.That(window.DwmCaptionColorMode, Is.EqualTo(DwmColorMode.Custom));
            Assert.That(window.DwmCaptionColor, Is.EqualTo(caption));
            Assert.That(window.DwmTextColorMode, Is.EqualTo(DwmColorMode.Custom));
            Assert.That(window.DwmTextColor, Is.EqualTo(text));
        }

        [Test]
        public void SystemWindow_DwmTextColorMode_CoercesNoneToDefault()
        {
            var window = new SystemWindow { DwmTextColorMode = DwmColorMode.None };

            Assert.That(window.DwmTextColorMode, Is.EqualTo(DwmColorMode.Default));
        }

        [Test]
        public void SystemWindow_MaximizeUsesTheNativeOverhangingGeometry()
        {
            var window = new SystemWindow
            {
                Width = 320,
                Height = 240,
                Left = -10000,
                Top = -10000,
                Opacity = 0,
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.Manual,
            };

            try
            {
                window.Show();
                var hwnd = (HWND)new WindowInteropHelper(window).Handle;

                window.WindowState = WindowState.Maximized;

                var monitor = PInvoke.MonitorFromWindow(
                    hwnd, Windows.Win32.Graphics.Gdi.MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);
                var mi = new Windows.Win32.Graphics.Gdi.MONITORINFO
                {
                    cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(
                        typeof(Windows.Win32.Graphics.Gdi.MONITORINFO)),
                };
                Assert.That((bool)PInvoke.GetMonitorInfo(monitor, ref mi), Is.True);
                PInvoke.GetWindowRect(hwnd, out RECT windowRect);

                // Native maximize overhangs the work area by the frame thickness;
                // the work-area clamp is a ChromeWindow-only NCCALCSIZE companion.
                Assert.That(windowRect.right - windowRect.left,
                    Is.GreaterThan(mi.rcWork.right - mi.rcWork.left));
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void SystemWindow_ShowHelp_TogglesTheContextHelpStyle()
        {
            var window = new SystemWindow
            {
                Width = 320,
                Height = 240,
                Left = -10000,
                Top = -10000,
                Opacity = 0,
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.Manual,
            };

            try
            {
                window.Show();
                var hwnd = (HWND)new WindowInteropHelper(window).Handle;

                Assert.That(GetExStyle(hwnd) & (int)WINDOW_EX_STYLE.WS_EX_CONTEXTHELP, Is.Zero);

                window.ShowHelp = true;
                Assert.That(GetExStyle(hwnd) & (int)WINDOW_EX_STYLE.WS_EX_CONTEXTHELP, Is.Not.Zero);

                window.ShowHelp = false;
                Assert.That(GetExStyle(hwnd) & (int)WINDOW_EX_STYLE.WS_EX_CONTEXTHELP, Is.Zero);
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void SystemWindow_ShowIconFalse_AppliesWhenHandleIsCreated()
        {
            var window = new SystemWindow
            {
                Width = 320,
                Height = 240,
                Left = -10000,
                Top = -10000,
                Opacity = 0,
                ShowIcon = false,
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.Manual,
            };

            try
            {
                window.Show();
                var hwnd = (HWND)new WindowInteropHelper(window).Handle;

                Assert.That(
                    GetExStyle(hwnd) & (int)WINDOW_EX_STYLE.WS_EX_DLGMODALFRAME,
                    Is.Not.Zero);
                Assert.That(GetWindowIcon(hwnd, PInvoke.ICON_SMALL), Is.EqualTo(IntPtr.Zero));
                Assert.That(GetWindowIcon(hwnd, PInvoke.ICON_BIG), Is.EqualTo(IntPtr.Zero));
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void SystemWindow_ShowIcon_HidesAndRestoresNativeIcons()
        {
            var pixels = new byte[16 * 16 * 4];
            for (int i = 3; i < pixels.Length; i += 4)
                pixels[i] = byte.MaxValue;

            var window = new SystemWindow
            {
                Width = 320,
                Height = 240,
                Icon = BitmapSource.Create(
                    16, 16, 96, 96, PixelFormats.Bgra32, null, pixels, 16 * 4),
                Left = -10000,
                Top = -10000,
                Opacity = 0,
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.Manual,
            };

            try
            {
                window.Show();
                var hwnd = (HWND)new WindowInteropHelper(window).Handle;
                int originalExStyle = GetExStyle(hwnd);
                IntPtr originalSmallIcon = GetWindowIcon(hwnd, PInvoke.ICON_SMALL);
                IntPtr originalBigIcon = GetWindowIcon(hwnd, PInvoke.ICON_BIG);

                Assert.That(originalSmallIcon, Is.Not.EqualTo(IntPtr.Zero));
                Assert.That(originalBigIcon, Is.Not.EqualTo(IntPtr.Zero));

                window.ShowIcon = false;

                Assert.That(
                    GetExStyle(hwnd) & (int)WINDOW_EX_STYLE.WS_EX_DLGMODALFRAME,
                    Is.Not.Zero);
                Assert.That(GetWindowIcon(hwnd, PInvoke.ICON_SMALL), Is.EqualTo(IntPtr.Zero));
                Assert.That(GetWindowIcon(hwnd, PInvoke.ICON_BIG), Is.EqualTo(IntPtr.Zero));

                window.ShowIcon = true;

                Assert.That(
                    GetExStyle(hwnd) & (int)WINDOW_EX_STYLE.WS_EX_DLGMODALFRAME,
                    Is.EqualTo(originalExStyle & (int)WINDOW_EX_STYLE.WS_EX_DLGMODALFRAME));
                Assert.That(GetWindowIcon(hwnd, PInvoke.ICON_SMALL), Is.EqualTo(originalSmallIcon));
                Assert.That(GetWindowIcon(hwnd, PInvoke.ICON_BIG), Is.EqualTo(originalBigIcon));

                window.ShowIcon = false;
                var updatedPixels = new byte[16 * 16 * 4];
                for (int i = 0; i < updatedPixels.Length; i += 4)
                {
                    updatedPixels[i + 2] = byte.MaxValue;
                    updatedPixels[i + 3] = byte.MaxValue;
                }
                window.Icon = BitmapSource.Create(
                    16, 16, 96, 96, PixelFormats.Bgra32, null, updatedPixels, 16 * 4);

                Assert.That(GetWindowIcon(hwnd, PInvoke.ICON_SMALL), Is.EqualTo(IntPtr.Zero));
                Assert.That(GetWindowIcon(hwnd, PInvoke.ICON_BIG), Is.EqualTo(IntPtr.Zero));

                window.ShowIcon = true;

                IntPtr updatedSmallIcon = GetWindowIcon(hwnd, PInvoke.ICON_SMALL);
                IntPtr updatedBigIcon = GetWindowIcon(hwnd, PInvoke.ICON_BIG);
                Assert.That(updatedSmallIcon, Is.Not.EqualTo(IntPtr.Zero));
                Assert.That(updatedBigIcon, Is.Not.EqualTo(IntPtr.Zero));
                Assert.That(updatedSmallIcon, Is.Not.EqualTo(originalSmallIcon));
                Assert.That(updatedBigIcon, Is.Not.EqualTo(originalBigIcon));
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void SystemWindow_WmHelp_RaisesHelpRequested()
        {
            var window = new SystemWindow
            {
                Width = 320,
                Height = 240,
                Left = -10000,
                Top = -10000,
                Opacity = 0,
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.Manual,
            };
            WindowBase.SetHelpTopic(window, "window-root");
            bool raised = false;
            object topic = null;
            window.HelpRequested += (_, e) =>
            {
                raised = true;
                topic = e.Topic;
            };

            try
            {
                window.Show();
                var hwnd = (HWND)new WindowInteropHelper(window).Handle;

                var info = new HELPINFO
                {
                    cbSize = (uint)Marshal.SizeOf(typeof(HELPINFO)),
                };
                IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(HELPINFO)));
                try
                {
                    Marshal.StructureToPtr(info, ptr, false);
                    PInvoke.SendMessage(hwnd, PInvoke.WM_HELP, default, new LPARAM(ptr));
                }
                finally
                {
                    Marshal.FreeHGlobal(ptr);
                }

                Assert.That(raised, Is.True);
                Assert.That(topic, Is.EqualTo("window-root"), "falls back to the window's own topic");
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void WindowBase_HelpCommandBinding_ResolvesTheTopicFromKeyboardFocus()
        {
            var box = new System.Windows.Controls.TextBox();
            WindowBase.SetHelpTopic(box, "focused-topic");
            object received = null;
            var window = new SystemWindow
            {
                Width = 320,
                Height = 240,
                Left = -10000,
                Top = -10000,
                Opacity = 0,
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Content = box,
                HelpCommand = new RelayCommand<object>(topic => received = topic),
            };

            try
            {
                window.Show();
                box.Focus();
                Assert.That(System.Windows.Input.Keyboard.FocusedElement, Is.SameAs(box));

                System.Windows.Input.ApplicationCommands.Help.Execute(null, box);

                Assert.That(received, Is.EqualTo("focused-topic"));
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void WindowBase_HelpCommandBinding_IsOnlyClaimedWithAConsumer()
        {
            var window = new SystemWindow
            {
                Width = 320,
                Height = 240,
                Left = -10000,
                Top = -10000,
                Opacity = 0,
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.Manual,
            };

            try
            {
                window.Show();

                Assert.That(
                    System.Windows.Input.ApplicationCommands.Help.CanExecute(null, window),
                    Is.False, "no consumer — F1 stays unclaimed");

                window.HelpCommand = new RelayCommand<object>(_ => { });
                Assert.That(
                    System.Windows.Input.ApplicationCommands.Help.CanExecute(null, window),
                    Is.True);
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void ChromeWindow_HelpButtonPipeline_FiresWhenWmHelpArrives()
        {
            var dictionary = (ResourceDictionary)Application.LoadComponent(
                new Uri("/WinCraft;component/UI/Theme/Controls.Window.xaml", UriKind.Relative));
            var window = new ChromeWindow
            {
                Width = 320,
                Height = 240,
                Left = -10000,
                Top = -10000,
                Opacity = 0,
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.Manual,
                ShowHelp = true,
            };
            window.Resources.MergedDictionaries.Add(dictionary);
            bool raised = false;
            window.HelpRequested += (_, __) => raised = true;

            try
            {
                window.Show();
                window.UpdateLayout();
                var hwnd = (HWND)new WindowInteropHelper(window).Handle;

                // Inject WM_HELP with a valid HELPINFO — lParam=0 produces
                // an unreadable struct, aborting HandleHelp before dispatch.
                var info = new HELPINFO
                {
                    cbSize = (uint)Marshal.SizeOf(typeof(HELPINFO)),
                };
                IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(HELPINFO)));
                try
                {
                    Marshal.StructureToPtr(info, ptr, false);
                    PInvoke.SendMessage(hwnd, PInvoke.WM_HELP, default, new LPARAM(ptr));
                }
                finally
                {
                    Marshal.FreeHGlobal(ptr);
                }

                Assert.That(raised, Is.True, "WM_HELP should dispatch through HandleHelp");
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void WindowBase_ResolveHelpElement_NormalizesTemplateInternals()
        {
            var button = new System.Windows.Controls.Button { Content = "?" };
            var window = new SystemWindow
            {
                Width = 320,
                Height = 240,
                Left = -10000,
                Top = -10000,
                Opacity = 0,
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Content = button,
            };

            try
            {
                window.Show();
                window.UpdateLayout();

                var templateChild = (FrameworkElement)VisualTreeHelper.GetChild(button, 0);
                Assert.That(templateChild.TemplatedParent, Is.SameAs(button));

                Assert.That(WindowBase.ResolveHelpElement(templateChild), Is.SameAs(button),
                    "template internals resolve to their control");
                Assert.That(WindowBase.ResolveHelpElement(null), Is.Null);
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void WindowBase_SizeMoveState_FollowsTheModalLoopMessages()
        {
            var window = new SystemWindow
            {
                Width = 320,
                Height = 240,
                Left = -10000,
                Top = -10000,
                Opacity = 0,
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.Manual,
            };

            try
            {
                window.Show();
                var hwnd = (HWND)new WindowInteropHelper(window).Handle;

                Assert.That(window.SizeMoveState, Is.EqualTo(SizeMoveState.None));

                // Do not send WM_SYSCOMMAND SC_MOVE/SC_SIZE here — DefWindowProc
                // would enter a real modal loop and hang the test.
                PInvoke.SendMessage(hwnd, PInvoke.WM_ENTERSIZEMOVE, default, default);
                Assert.That(window.SizeMoveState, Is.Not.EqualTo(SizeMoveState.None));

                PInvoke.SendMessage(hwnd, PInvoke.WM_EXITSIZEMOVE, default, default);
                Assert.That(window.SizeMoveState, Is.EqualTo(SizeMoveState.None));
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void WindowBase_InteractiveResizeStopsStartupPositionCorrection()
        {
            var window = new SystemWindow
            {
                Width = 320,
                Height = 240,
                Opacity = 0,
                ShowActivated = false,
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
            };

            try
            {
                window.Show();
                var hwnd = (HWND)new WindowInteropHelper(window).Handle;
                Assert.That((bool)PInvoke.GetWindowRect(hwnd, out RECT before), Is.True);

                PInvoke.SendMessage(hwnd, PInvoke.WM_ENTERSIZEMOVE, default, default);

                const int draggedDistance = 40;
                Assert.That(
                    (bool)PInvoke.SetWindowPos(
                        hwnd,
                        HWND.Null,
                        before.left - draggedDistance,
                        before.top,
                        before.right - before.left + draggedDistance,
                        before.bottom - before.top,
                        SET_WINDOW_POS_FLAGS.SWP_NOZORDER
                        | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE),
                    Is.True);
                Assert.That((bool)PInvoke.GetWindowRect(hwnd, out RECT after), Is.True);

                Assert.Multiple(() =>
                {
                    Assert.That(after.left, Is.EqualTo(before.left - draggedDistance));
                    Assert.That(after.right, Is.EqualTo(before.right));
                    Assert.That(after.top, Is.EqualTo(before.top));
                    Assert.That(after.bottom, Is.EqualTo(before.bottom));
                });

                PInvoke.SendMessage(hwnd, PInvoke.WM_EXITSIZEMOVE, default, default);
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void WindowBase_LoadedWindowWithoutContentStopsStartupPositionCorrection()
        {
            var window = new SystemWindow
            {
                Width = 320,
                Height = 240,
                Opacity = 0,
                ShowActivated = false,
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
            };

            try
            {
                window.Show();
                var hwnd = (HWND)new WindowInteropHelper(window).Handle;
                Assert.That(window.IsLoaded, Is.True);
                Assert.That((bool)PInvoke.GetWindowRect(hwnd, out RECT before), Is.True);

                const int movedDistance = 40;
                Assert.That(
                    (bool)PInvoke.SetWindowPos(
                        hwnd,
                        HWND.Null,
                        before.left - movedDistance,
                        before.top,
                        before.right - before.left + movedDistance,
                        before.bottom - before.top,
                        SET_WINDOW_POS_FLAGS.SWP_NOZORDER
                        | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE),
                    Is.True);
                Assert.That((bool)PInvoke.GetWindowRect(hwnd, out RECT after), Is.True);

                Assert.Multiple(() =>
                {
                    Assert.That(after.left, Is.EqualTo(before.left - movedDistance));
                    Assert.That(after.right, Is.EqualTo(before.right));
                    Assert.That(after.top, Is.EqualTo(before.top));
                    Assert.That(after.bottom, Is.EqualTo(before.bottom));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void SystemWindow_RejectsTranslucentDwmColors()
        {
            var window = new SystemWindow();
            var translucent = Color.FromArgb(0x80, 0x12, 0x34, 0x56);

            Assert.That(() => window.DwmBorderColor = translucent, Throws.ArgumentException);
            Assert.That(() => window.DwmCaptionColor = translucent, Throws.ArgumentException);
            Assert.That(() => window.DwmTextColor = translucent, Throws.ArgumentException);
        }

        [TestCase(DwmColorMode.Default, unchecked((int)0xFFFFFFFF))]
        [TestCase(DwmColorMode.None, unchecked((int)0xFFFFFFFE))]
        [TestCase(DwmColorMode.Custom, 0x00563412)]
        public void WindowBase_DwmColorMode_MapsToTheExpectedColorRef(DwmColorMode mode, int expected)
        {
            var custom = Color.FromRgb(0x12, 0x34, 0x56);

            Assert.That(WindowBase.ToDwmColorRef(mode, custom), Is.EqualTo(expected));
        }

        [Test]
        public void WindowBase_AcrylicColor_MapsToTheExpectedAccentPolicyColor()
        {
            var color = Color.FromArgb(0xCC, 0x11, 0x22, 0x33);

            Assert.That(
                WindowBase.ToAccentColor(color),
                Is.EqualTo(unchecked((int)0xCC332211)));
        }

        [Test]
        public void SystemWindow_ReservesTheNativeCaption()
        {
            var window = new SystemWindow
            {
                Width = 320,
                Height = 240,
                Left = -10000,
                Top = -10000,
                Opacity = 0,
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.Manual,
            };

            try
            {
                window.Show();
                var hwnd = (HWND)new WindowInteropHelper(window).Handle;

                PInvoke.GetWindowRect(hwnd, out RECT windowRect);
                PInvoke.GetClientRect(hwnd, out RECT clientRect);
                int nonClientHeight = (windowRect.bottom - windowRect.top)
                    - (clientRect.bottom - clientRect.top);
                int captionHeight = PInvoke.GetSystemMetrics(
                    Windows.Win32.UI.WindowsAndMessaging.SYSTEM_METRICS_INDEX.SM_CYCAPTION);

                Assert.That(nonClientHeight, Is.GreaterThanOrEqualTo(captionHeight),
                    "native caption remains part of the non-client area");
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void SystemWindow_BackgroundStaysOpaqueForTheEraseFill()
        {
            var brushes = (ResourceDictionary)Application.LoadComponent(
                new Uri("/WinCraft;component/UI/Theme/Brushes.Light.xaml", UriKind.Relative));
            var dictionary = (ResourceDictionary)Application.LoadComponent(
                new Uri("/WinCraft;component/UI/Theme/Controls.Window.xaml", UriKind.Relative));
            var window = new SystemWindow
            {
                Width = 320,
                Height = 240,
                Left = -10000,
                Top = -10000,
                Opacity = 0,
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.Manual,
            };
            window.Resources.MergedDictionaries.Add(brushes);
            window.Resources.MergedDictionaries.Add(dictionary);

            try
            {
                window.Show();
                window.UpdateLayout();

                // The startup erase fill reads Window.Background; a backdrop must
                // only affect the template surface, never the window property.
                var background = window.Background as SolidColorBrush;
                Assert.That(background, Is.Not.Null);
                Assert.That(background.Color.A, Is.EqualTo(byte.MaxValue),
                    $"opaque background required (IsBackdropActive={window.IsBackdropActive})");
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void SystemWindow_ShowCloseFalseSwallowsScClose()
        {
            var window = new SystemWindow
            {
                Width = 320,
                Height = 240,
                Left = -10000,
                Top = -10000,
                Opacity = 0,
                ShowInTaskbar = false,
                ShowClose = false,
                WindowStartupLocation = WindowStartupLocation.Manual,
            };
            bool closed = false;
            window.Closed += (_, __) => closed = true;

            try
            {
                window.Show();
                var hwnd = (HWND)new WindowInteropHelper(window).Handle;

                PInvoke.SendMessage(hwnd, PInvoke.WM_SYSCOMMAND, new WPARAM(PInvoke.SC_CLOSE), default);

                Assert.That(closed, Is.False);
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void SystemWindow_ShowClose_SynchronizesNativeMenuItemState()
        {
            var window = new SystemWindow
            {
                Width = 320,
                Height = 240,
                Left = -10000,
                Top = -10000,
                Opacity = 0,
                ShowInTaskbar = false,
                ShowClose = false,
                WindowStartupLocation = WindowStartupLocation.Manual,
            };

            try
            {
                window.Show();
                var hwnd = (HWND)new WindowInteropHelper(window).Handle;
                var menu = PInvoke.GetSystemMenu(hwnd, false);

                bool wasGrayed = PInvoke.EnableMenuItem(
                    menu, PInvoke.SC_CLOSE, MENU_ITEM_FLAGS.MF_ENABLED);

                Assert.That(wasGrayed, Is.True);

                window.ShowClose = true;
                bool wasEnabled = PInvoke.EnableMenuItem(
                    menu, PInvoke.SC_CLOSE, MENU_ITEM_FLAGS.MF_GRAYED);

                Assert.That(wasEnabled, Is.False);
            }
            finally
            {
                window.Close();
            }
        }

        private static int GetExStyle(HWND hwnd) =>
            PInvoke.GetWindowLongPtr(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE).ToInt32();

        private static IntPtr GetWindowIcon(HWND hwnd, uint iconType)
        {
            LRESULT result = PInvoke.SendMessage(
                hwnd, PInvoke.WM_GETICON, new WPARAM(iconType), default);
            return new IntPtr(result.Value);
        }
    }
}
