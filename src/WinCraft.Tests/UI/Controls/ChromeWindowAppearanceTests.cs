using System;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Media;
using NUnit.Framework;
using WinCraft.UI;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.UI.WindowsAndMessaging;

namespace WinCraft.Tests.UI.Controls
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    internal sealed class ChromeWindowAppearanceTests
    {
        [Test]
        public void ChromeWindow_ThemeAppearanceResources_AreDynamic()
        {
            var dictionary = (ResourceDictionary)Application.LoadComponent(
                new Uri("/WinCraft;component/UI/Theme/Controls.Window.xaml", UriKind.Relative));
            var window = new ChromeWindow();
            var acrylic = Color.FromArgb(0xCC, 0x11, 0x22, 0x33);

            window.Resources["WindowDwmUseDarkMode"] = true;
            window.Resources["WindowAcrylicGradientColor"] = acrylic;
            window.Style = (Style)dictionary[typeof(ChromeWindow)];

            Assert.That(window.DwmUseDarkMode, Is.True);
            Assert.That(window.AcrylicGradientColor, Is.EqualTo(acrylic));

            window.Resources["WindowDwmUseDarkMode"] = false;
            Assert.That(window.DwmUseDarkMode, Is.False);
        }

        [Test]
        public void ChromeWindow_HitTestRole_IsInheritedByTemplateDescendants()
        {
            var titleBar = new Border();
            var child = new Border();
            titleBar.Child = child;

            ChromeWindow.SetHitTestRole(titleBar, WindowHitTestRole.Caption);

            Assert.That(ChromeWindow.GetHitTestRole(child), Is.EqualTo(WindowHitTestRole.Caption));
        }

        [Test]
        public void ChromeWindow_HitTestOwner_UsesTheLogicalParentOfRun()
        {
            var textBlock = new TextBlock();
            var run = new Run("Caption");
            textBlock.Inlines.Add(run);
            ChromeWindow.SetHitTestRole(textBlock, WindowHitTestRole.Caption);

            var result = InvokeGetHitTestOwner(run, out var role);

            Assert.That(role, Is.EqualTo(WindowHitTestRole.Caption));
            Assert.That(result, Is.SameAs(textBlock));
        }

        [Test]
        public void ChromeWindow_CenterOwner_CentersOnItsOwner()
        {
            var owner = new ChromeWindow
            {
                Width = 640,
                Height = 480,
                Left = 200,
                Top = 160,
                Opacity = 0,
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.Manual,
            };
            ChromeWindow window = null;

            try
            {
                owner.Show();
                window = new ChromeWindow
                {
                    Width = 320,
                    Height = 240,
                    Opacity = 0,
                    ShowInTaskbar = false,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = owner,
                };
                window.Show();

                Assert.That(
                    PInvoke.GetWindowRect(
                        (HWND)new WindowInteropHelper(owner).Handle,
                        out RECT ownerBounds),
                    Is.Not.EqualTo(default(BOOL)));
                Assert.That(
                    PInvoke.GetWindowRect(
                        (HWND)new WindowInteropHelper(window).Handle,
                        out RECT windowBounds),
                    Is.Not.EqualTo(default(BOOL)));

                Assert.That(
                    windowBounds.left + windowBounds.right,
                    Is.EqualTo(ownerBounds.left + ownerBounds.right).Within(1));
                Assert.That(
                    windowBounds.top + windowBounds.bottom,
                    Is.EqualTo(ownerBounds.top + ownerBounds.bottom).Within(1));
            }
            finally
            {
                window?.Close();
                owner.Close();
            }
        }

        [Test]
        public void ChromeWindow_CenterOwner_CentersSizeToContentWindowOnItsOwner()
        {
            var owner = new ChromeWindow
            {
                Width = 640,
                Height = 480,
                Left = 200,
                Top = 160,
                Opacity = 0,
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.Manual,
            };
            ChromeWindow window = null;

            try
            {
                owner.Show();
                window = new ChromeWindow
                {
                    Width = 320,
                    Content = new Border { Height = 120 },
                    Opacity = 0,
                    ShowInTaskbar = false,
                    SizeToContent = SizeToContent.Height,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = owner,
                };
                window.Show();

                Assert.That(
                    PInvoke.GetWindowRect(
                        (HWND)new WindowInteropHelper(owner).Handle,
                        out RECT ownerBounds),
                    Is.Not.EqualTo(default(BOOL)));
                Assert.That(
                    PInvoke.GetWindowRect(
                        (HWND)new WindowInteropHelper(window).Handle,
                        out RECT windowBounds),
                    Is.Not.EqualTo(default(BOOL)));

                Assert.That(
                    windowBounds.top + windowBounds.bottom,
                    Is.EqualTo(ownerBounds.top + ownerBounds.bottom).Within(1));
            }
            finally
            {
                window?.Close();
                owner.Close();
            }
        }

        [Test]
        public void ChromeWindow_MaximizeButton_TracksWindowState()
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
                WindowStartupLocation = WindowStartupLocation.Manual,
            };
            window.Resources.MergedDictionaries.Add(dictionary);

            try
            {
                window.Show();
                window.UpdateLayout();
                var maximize = FindNamedChild<TitleBarButton>(window, "MaximizeButton");

                Assert.That(ChromeWindow.GetHitTestRole(maximize), Is.EqualTo(WindowHitTestRole.Maximize));
                Assert.That(maximize.Icon, Is.EqualTo(IconGlyph.Maximize16));

                window.WindowState = WindowState.Maximized;
                window.UpdateLayout();

                Assert.That(maximize.Icon, Is.EqualTo(IconGlyph.SquareMultiple16));
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void ChromeWindow_HelpButton_IsHiddenUnlessShowHelp()
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
                WindowStartupLocation = WindowStartupLocation.Manual,
            };
            window.Resources.MergedDictionaries.Add(dictionary);

            try
            {
                window.Show();
                window.UpdateLayout();
                var help = FindNamedChild<TitleBarButton>(window, "HelpButton");

                Assert.That(window.ShowHelp, Is.False);
                Assert.That(help.Visibility, Is.EqualTo(Visibility.Collapsed));

                window.ShowHelp = true;
                window.UpdateLayout();

                Assert.That(help.Visibility, Is.EqualTo(Visibility.Visible));
            }
            finally
            {
                window.Close();
            }
        }

        [TestCase(DwmCornerPreference.Default, DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_DEFAULT)]
        [TestCase(DwmCornerPreference.DoNotRound, DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_DONOTROUND)]
        [TestCase(DwmCornerPreference.Round, DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND)]
        [TestCase(DwmCornerPreference.RoundSmall, DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUNDSMALL)]
        public void ChromeWindow_CornerPreference_MapsToTheDwmValue(
            DwmCornerPreference preference,
            DWM_WINDOW_CORNER_PREFERENCE expected)
        {
            Assert.That(ChromeWindow.ToDwmCornerPreference(preference), Is.EqualTo(expected));
        }

        [TestCase(WindowHitTestRole.Client, (int)PInvoke.HTCLIENT)]
        [TestCase(WindowHitTestRole.Caption, (int)PInvoke.HTCAPTION)]
        [TestCase(WindowHitTestRole.Minimize, (int)PInvoke.HTMINBUTTON)]
        [TestCase(WindowHitTestRole.Maximize, (int)PInvoke.HTMAXBUTTON)]
        [TestCase(WindowHitTestRole.Close, (int)PInvoke.HTCLOSE)]
        public void ChromeWindow_HitTestRole_MapsToTheExpectedNonClientCode(
            WindowHitTestRole role,
            int expected)
        {
            Assert.That(ChromeWindow.GetNonClientHitTest(role), Is.EqualTo(expected));
        }

        [Test]
        public void ChromeWindow_ResizeHitTest_PreservesContentControlsAndHandlesTheOuterEdge()
        {
            var scrollViewer = new ScrollViewer
            {
                Content = new Border { Height = 1000 },
                VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
            };
            var window = new ChromeWindow
            {
                Content = scrollViewer,
                Width = 320,
                Height = 240,
                Left = -10000,
                Top = -10000,
                Opacity = 0,
                WindowStartupLocation = WindowStartupLocation.Manual,
            };
            Assert.That(window.WindowStyle, Is.EqualTo(WindowStyle.SingleBorderWindow));

            try
            {
                window.Show();
                window.UpdateLayout();

                var scrollBar = FindVerticalScrollBar(scrollViewer);
                Assert.That(scrollBar, Is.Not.Null);
                var scrollBarPoint = scrollBar.PointToScreen(
                    new Point(scrollBar.ActualWidth / 2, scrollBar.ActualHeight / 2));
                var hwnd = new WindowInteropHelper(window).Handle;

                Assert.That(
                    window.GetResizeHitTest(hwnd, (int)scrollBarPoint.X, (int)scrollBarPoint.Y),
                    Is.EqualTo(0));

                PInvoke.GetWindowRect((HWND)hwnd, out RECT rect);
                Assert.That(
                    window.GetResizeHitTest(hwnd, rect.right - 1, rect.top + (rect.bottom - rect.top) / 2),
                    Is.EqualTo((int)PInvoke.HTRIGHT),
                    "outer right edge");

                PInvoke.GetClientRect((HWND)hwnd, out RECT client);
                var clientRight = new System.Drawing.Point(client.right, client.top + client.bottom / 2);
                Assert.That((bool)PInvoke.ClientToScreen((HWND)hwnd, ref clientRight), Is.True);
                Assert.That(
                    window.GetResizeHitTest(hwnd, clientRight.X, clientRight.Y),
                    Is.EqualTo((int)PInvoke.HTRIGHT),
                    "client right edge");

            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void ChromeWindow_UsesNativeIconWhenIconIsNotSpecified()
        {
            var window = new ChromeWindow
            {
                Width = 320,
                Height = 240,
                Left = -10000,
                Top = -10000,
                Opacity = 0,
                WindowStartupLocation = WindowStartupLocation.Manual,
            };

            try
            {
                window.Show();
                window.UpdateLayout();

                Assert.That(window.Icon, Is.Null);
                Assert.That(window.EffectiveIcon, Is.Not.Null);
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void ChromeWindow_UsesExplicitIconWhenSpecified()
        {
            var window = new ChromeWindow();
            var icon = new DrawingImage();

            window.Icon = icon;

            Assert.That(window.EffectiveIcon, Is.SameAs(icon));
        }

        [Test]
        public void ChromeWindow_ResizeBandDoesNotCoverTheVisibleFrame()
        {
            var windowBounds = new RECT { left = 0, top = 0, right = 100, bottom = 100 };
            var visibleFrameBounds = new RECT { left = 8, top = 8, right = 92, bottom = 92 };

            Assert.That(
                ChromeWindow.GetResizeHitTest(91, 50, windowBounds, visibleFrameBounds),
                Is.EqualTo(0));
            Assert.That(
                ChromeWindow.GetResizeHitTest(92, 50, windowBounds, visibleFrameBounds),
                Is.EqualTo((int)PInvoke.HTRIGHT));
            Assert.That(
                ChromeWindow.GetResizeHitTest(4, 4, windowBounds, visibleFrameBounds),
                Is.EqualTo((int)PInvoke.HTTOPLEFT));
        }

        [TestCase(false, 12, 1)]
        [TestCase(true, 12, 12)]
        public void ChromeWindow_ClientTopInset_ReservesTheFullFrameWhenMaximized(
            bool isMaximized,
            int resizeBorderHeight,
            int expected)
        {
            Assert.That(
                ChromeWindow.GetClientTopInset(isMaximized, resizeBorderHeight),
                Is.EqualTo(expected));
        }

        [Test]
        public void ChromeWindow_TemplateClientAreaFixup_ExpandsOnlyTheMissingRightAndBottom()
        {
            var margin = new Thickness(2, 3, 4, 5);

            Thickness result = ChromeWindow.GetTemplateRootMarginForClientSize(
                margin,
                new Size(780, 560),
                new Size(796, 600));

            Assert.That(result, Is.EqualTo(new Thickness(2, 3, -12, -35)));
        }

        [Test]
        public void ChromeWindow_TemplateClientAreaFixup_DoesNotShrinkAnAlreadyFullTemplate()
        {
            var margin = new Thickness(2, 3, 4, 5);

            Thickness result = ChromeWindow.GetTemplateRootMarginForClientSize(
                margin,
                new Size(800, 620),
                new Size(796, 600));

            Assert.That(result, Is.EqualTo(margin));
        }

        [Test]
        public void ChromeWindow_TemplateRoot_FillsTheExpandedClientArea()
        {
            var dictionary = (ResourceDictionary)Application.LoadComponent(
                new Uri("/WinCraft;component/UI/Theme/Controls.Window.xaml", UriKind.Relative));
            var window = new ChromeWindow
            {
                Width = 420,
                MinWidth = 320,
                MaxWidth = 560,
                ResizeMode = ResizeMode.NoResize,
                SizeToContent = SizeToContent.Height,
                Content = new Border { Height = 100 },
                Left = -10000,
                Top = -10000,
                Opacity = 0,
                WindowStartupLocation = WindowStartupLocation.Manual,
            };
            window.Resources.MergedDictionaries.Add(dictionary);

            try
            {
                window.Show();
                window.UpdateLayout();

                PInvoke.GetClientRect((HWND)new WindowInteropHelper(window).Handle, out RECT client);
                var clientSize = PresentationSource.FromVisual(window).CompositionTarget.TransformFromDevice.Transform(
                    new Point(client.right - client.left, client.bottom - client.top));
                var templateRoot = (FrameworkElement)VisualTreeHelper.GetChild(window, 0);

                Assert.That(templateRoot.ActualWidth, Is.EqualTo(clientSize.X).Within(0.01));
                Assert.That(templateRoot.ActualHeight, Is.EqualTo(clientSize.Y).Within(0.01));
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void ChromeWindow_TitleBarScreenRect_UsesBothTransformedCorners()
        {
            RECT result = ChromeWindow.CreateScreenRect(
                new Point(-100.25, 20.25),
                new Point(-50.75, 52.75));

            Assert.That(result.left, Is.EqualTo(-101));
            Assert.That(result.top, Is.EqualTo(20));
            Assert.That(result.right, Is.EqualTo(-50));
            Assert.That(result.bottom, Is.EqualTo(53));
        }

        [Test]
        public void ChromeWindow_TitleBarScreenRect_UsesTheClientOrigin()
        {
            RECT result = ChromeWindow.CreateTitleBarScreenRect(
                new RECT { left = -50, top = 100, right = 450, bottom = 700 },
                40);

            Assert.That(result, Is.EqualTo(
                new RECT { left = -50, top = 100, right = 450, bottom = 140 }));
        }

        [TestCase(true, true)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(false, false)]
        public void ChromeWindow_WindowStyle_FollowsBoxCapabilities(bool minimizeBox, bool maximizeBox)
        {
            const int baseStyle = unchecked((int)0x94000000); // WS_POPUP | WS_VISIBLE | WS_CLIPSIBLINGS

            int style = WindowBase.ComputeWindowStyle(baseStyle, minimizeBox, maximizeBox);

            Assert.That((style & (int)WINDOW_STYLE.WS_MINIMIZEBOX) != 0, Is.EqualTo(minimizeBox));
            Assert.That((style & (int)WINDOW_STYLE.WS_MAXIMIZEBOX) != 0, Is.EqualTo(maximizeBox));
            Assert.That(style & baseStyle, Is.EqualTo(baseStyle), "unrelated bits preserved");
        }

        [Test]
        public void ChromeWindow_MaximizeRestoreCommand_RespectsShowMaximize()
        {
            var window = new ChromeWindow { ShowMaximize = false };

            window.MaximizeRestoreCommand.Execute(null);
            Assert.That(window.WindowState, Is.EqualTo(WindowState.Normal), "maximize blocked");

            window.WindowState = WindowState.Maximized;
            window.MaximizeRestoreCommand.Execute(null);
            Assert.That(window.WindowState, Is.EqualTo(WindowState.Normal), "restore stays allowed");

            window.ShowMaximize = true;
            window.MaximizeRestoreCommand.Execute(null);
            Assert.That(window.WindowState, Is.EqualTo(WindowState.Maximized));
        }

        [Test]
        public void ChromeWindow_MinimizeCommand_RespectsShowMinimize()
        {
            var window = new ChromeWindow { ShowMinimize = false };

            window.MinimizeCommand.Execute(null);

            Assert.That(window.WindowState, Is.EqualTo(WindowState.Normal));
        }

        [Test]
        public void ChromeWindow_DialogStyleSettings_HideMinimizeAndMaximizeButtons()
        {
            var dictionary = (ResourceDictionary)Application.LoadComponent(
                new Uri("/WinCraft;component/UI/Theme/Controls.Window.xaml", UriKind.Relative));
            var dialog = new ChromeWindow
            {
                Width = 420,
                ResizeMode = ResizeMode.NoResize,
                ShowIcon = false,
                ShowInTaskbar = false,
                SizeToContent = SizeToContent.Height,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Left = -10000,
                Opacity = 0,
                Top = -10000,
            };
            dialog.Resources.MergedDictionaries.Add(dictionary);

            try
            {
                dialog.Show();
                dialog.UpdateLayout();
                var minimize = FindNamedChild<TitleBarButton>(dialog, "MinimizeButton");
                var maximize = FindNamedChild<TitleBarButton>(dialog, "MaximizeButton");
                var close = FindNamedChild<TitleBarButton>(dialog, "CloseButton");

                Assert.That(minimize.Visibility, Is.EqualTo(Visibility.Collapsed));
                Assert.That(maximize.Visibility, Is.EqualTo(Visibility.Collapsed));
                Assert.That(close.Visibility, Is.EqualTo(Visibility.Visible));
            }
            finally
            {
                dialog.Close();
            }
        }

        [Test]
        public void ChromeWindow_SystemMenuStyle_IsAlwaysAbsent()
        {
            var window = new ChromeWindow
            {
                Width = 420,
                Height = 240,
                ResizeMode = ResizeMode.CanResize,
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Left = -10000,
                Top = -10000,
                Opacity = 0,
            };

            try
            {
                window.Show();
                var hwnd = (HWND)new WindowInteropHelper(window).Handle;
                int style = PInvoke.GetWindowLongPtr(
                    hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE).ToInt32();

                Assert.That(
                    style & (int)WINDOW_STYLE.WS_SYSMENU,
                    Is.Zero);
                Assert.That(style & (int)WINDOW_STYLE.WS_MINIMIZEBOX, Is.Not.Zero);
                Assert.That(style & (int)WINDOW_STYLE.WS_MAXIMIZEBOX, Is.Not.Zero);

                window.WindowState = WindowState.Maximized;
                style = PInvoke.GetWindowLongPtr(
                    hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE).ToInt32();

                Assert.That(
                    style & (int)WINDOW_STYLE.WS_SYSMENU,
                    Is.Zero);
                Assert.That(style & (int)WINDOW_STYLE.WS_MINIMIZEBOX, Is.Not.Zero);
                Assert.That(style & (int)WINDOW_STYLE.WS_MAXIMIZEBOX, Is.Not.Zero);
            }
            finally
            {
                window.Close();
            }
        }

        private static T FindNamedChild<T>(DependencyObject parent, string name)
            where T : FrameworkElement
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typed && typed.Name == name)
                    return typed;

                var descendant = FindNamedChild<T>(child, name);
                if (descendant != null)
                    return descendant;
            }
            return null;
        }

        private static ScrollBar FindVerticalScrollBar(DependencyObject parent)
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is ScrollBar scrollBar
                    && scrollBar.Orientation == Orientation.Vertical
                    && scrollBar.IsVisible)
                    return scrollBar;

                var descendant = FindVerticalScrollBar(child);
                if (descendant != null)
                    return descendant;
            }
            return null;
        }

        private static DependencyObject InvokeGetHitTestOwner(
            IInputElement hit,
            out WindowHitTestRole role)
        {
            var method = typeof(ChromeWindow).GetMethod(
                "GetHitTestOwner",
                BindingFlags.NonPublic | BindingFlags.Static);
            var arguments = new object[] { hit, null };
            var result = (DependencyObject)method.Invoke(null, arguments);
            role = (WindowHitTestRole)arguments[1];
            return result;
        }

    }
}
