using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using NUnit.Framework;
using WinCraft.UI;

namespace WinCraft.Tests.UI
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    internal sealed class RightClickFocusTests
    {
        [Test]
        public void RightClickPasswordBox_MovesKeyboardFocusFromComboBox()
        {
            RightClickFocus.Register();

            var comboBox = new ComboBox();
            var passwordBox = new PasswordBox();
            var window = new Window
            {
                Content = new StackPanel { Children = { comboBox, passwordBox } },
            };
            window.Show();

            try
            {
                Keyboard.Focus(comboBox);
                Assert.That(comboBox.IsKeyboardFocusWithin, Is.True);

                RaiseRightButtonDown(passwordBox);

                Assert.That(passwordBox.IsKeyboardFocused, Is.True);
                Assert.That(comboBox.IsKeyboardFocusWithin, Is.False);
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void RightClickButton_MovesKeyboardFocusFromComboBox()
        {
            RightClickFocus.Register();

            var comboBox = new ComboBox();
            var button = new Button { ContextMenu = new ContextMenu() };
            var window = new Window
            {
                Content = new StackPanel { Children = { comboBox, button } },
            };
            window.Show();

            try
            {
                Keyboard.Focus(comboBox);

                RaiseRightButtonDown(button);

                Assert.That(button.IsKeyboardFocused, Is.True);
                Assert.That(comboBox.IsKeyboardFocusWithin, Is.False);
            }
            finally
            {
                window.Close();
            }
        }


        [Test]
        public void RightClickTextBox_MovesKeyboardFocusFromComboBox()
        {
            RightClickFocus.Register();

            var comboBox = new ComboBox();
            var textBox = new TextBox();
            var window = new Window
            {
                Content = new StackPanel { Children = { comboBox, textBox } },
            };
            window.Show();

            try
            {
                Keyboard.Focus(comboBox);

                RaiseRightButtonDown(textBox);

                Assert.That(textBox.IsKeyboardFocused, Is.True);
                Assert.That(comboBox.IsKeyboardFocusWithin, Is.False);
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void RightClickDisabledTextBox_DoesNotMoveKeyboardFocus()
        {
            RightClickFocus.Register();

            var comboBox = new ComboBox();
            var textBox = new TextBox { IsEnabled = false };
            var window = new Window
            {
                Content = new StackPanel { Children = { comboBox, textBox } },
            };
            window.Show();

            try
            {
                Keyboard.Focus(comboBox);

                RaiseRightButtonDown(textBox);

                Assert.That(comboBox.IsKeyboardFocusWithin, Is.True);
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void ContextMenuClose_WithoutRightClickFocus_RestoresFocusToComboBox()
        {
            var comboBox = new ComboBox();
            var passwordBox = new PasswordBox { ContextMenu = new ContextMenu() };
            var window = CreateWindow(comboBox, passwordBox);
            window.Show();

            try
            {
                Keyboard.Focus(comboBox);

                OpenAndCloseContextMenu(passwordBox);

                Assert.That(Keyboard.FocusedElement, Is.SameAs(comboBox),
                    "Baseline: without the right-click focus fix, focus should return to the ComboBox.");
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void ContextMenuClose_AfterRightClickFocus_RestoresFocusToPasswordBox()
        {
            RightClickFocus.Register();

            var comboBox = new ComboBox();
            var passwordBox = new PasswordBox { ContextMenu = new ContextMenu() };
            var window = CreateWindow(comboBox, passwordBox);
            window.Show();

            try
            {
                Keyboard.Focus(comboBox);

                RaiseRightButtonDown(passwordBox);
                Assert.That(Keyboard.FocusedElement, Is.SameAs(passwordBox));

                OpenAndCloseContextMenu(passwordBox);

                Assert.That(Keyboard.FocusedElement, Is.SameAs(passwordBox),
                    "After the menu closes, focus should return to the right-clicked PasswordBox.");
                Assert.That(comboBox.IsKeyboardFocusWithin, Is.False);
            }
            finally
            {
                window.Close();
            }
        }

        private static Window CreateWindow(params UIElement[] children)
        {
            var panel = new StackPanel();
            foreach (var child in children)
                panel.Children.Add(child);

            return new Window
            {
                Content = panel,
                Height = 200,
                Left = -10000,
                Opacity = 0,
                ShowInTaskbar = false,
                Top = -10000,
                Width = 200,
                WindowStyle = WindowStyle.None,
            };
        }

        [Test]
        [Explicit("Injects real mouse and keyboard input; moves the cursor.")]
        public void RealInputRightClick_MovesFocusFromComboBoxToPasswordBox()
        {
            RealInputRightClick_MovesFocusFromComboBoxToPasswordBoxCore(useTheme: false);
        }

        [Test]
        [Explicit("Injects real mouse and keyboard input; moves the cursor.")]
        public void RealInputRightClick_ThemedControls_MoveFocusFromComboBoxToPasswordBox()
        {
            RealInputRightClick_MovesFocusFromComboBoxToPasswordBoxCore(useTheme: true);
        }

        private static void RealInputRightClick_MovesFocusFromComboBoxToPasswordBoxCore(bool useTheme)
        {
            RightClickFocus.Register();

            var comboBox = new ComboBox();
            var passwordBox = new PasswordBox();
            if (!useTheme)
            {
                passwordBox.ContextMenu = new ContextMenu { Items = { new MenuItem { Header = "Item" } } };
            }

            var window = new Window
            {
                Content = new StackPanel { Children = { comboBox, passwordBox } },
                Height = 200,
                Left = 200,
                ShowInTaskbar = false,
                SizeToContent = SizeToContent.Width,
                Top = 200,
                Topmost = true,
                Width = 300,
                WindowStartupLocation = WindowStartupLocation.Manual,
            };
            if (useTheme)
            {
                window.Resources.MergedDictionaries.Add(new ResourceDictionary
                {
                    Source = new Uri(
                        "/WinCraft;component/UI/Theme/Controls.Input.xaml",
                        UriKind.Relative),
                });
            }

            window.Show();
            window.Activate();
            ProcessDispatcherQueue();

            try
            {
                Keyboard.Focus(comboBox);
                ProcessDispatcherQueue();
                Describe("after focusing ComboBox", comboBox, passwordBox);

                var center = passwordBox.PointToScreen(new Point(
                    passwordBox.ActualWidth / 2,
                    passwordBox.ActualHeight / 2));
                NativeMethods.MoveCursor((int)center.X, (int)center.Y);
                ProcessDispatcherQueue();

                NativeMethods.SendRightClick();
                ProcessDispatcherQueue();
                Describe("after real right click (menu should be open)", comboBox, passwordBox);

                NativeMethods.SendEscape();
                ProcessDispatcherQueue();
                Describe("after dismissing the menu", comboBox, passwordBox);

                Assert.That(comboBox.IsKeyboardFocusWithin, Is.False,
                    "ComboBox should no longer contain keyboard focus.");
                Assert.That(passwordBox.IsKeyboardFocusWithin, Is.True,
                    "PasswordBox should hold keyboard focus after the right click.");
            }
            finally
            {
                window.Close();
            }
        }

        private static void Describe(string stage, ComboBox comboBox, PasswordBox passwordBox)
        {
            var focused = Keyboard.FocusedElement;
            TestContext.Out.WriteLine(
                $"{stage}: focused={focused?.GetType().Name ?? "null"}, " +
                $"comboBox.IsKeyboardFocusWithin={comboBox.IsKeyboardFocusWithin}, " +
                $"passwordBox.IsKeyboardFocusWithin={passwordBox.IsKeyboardFocusWithin}, " +
                $"menuOpen={passwordBox.ContextMenu.IsOpen}");
        }

        private static void OpenAndCloseContextMenu(FrameworkElement owner)
        {
            owner.ContextMenu.PlacementTarget = owner;
            owner.ContextMenu.IsOpen = true;
            ProcessDispatcherQueue();

            owner.ContextMenu.IsOpen = false;
            ProcessDispatcherQueue();
        }

        private static void ProcessDispatcherQueue()
        {
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(
                DispatcherPriority.ApplicationIdle,
                new DispatcherOperationCallback(_ =>
                {
                    frame.Continue = false;
                    return null;
                }),
                null);
            Dispatcher.PushFrame(frame);
        }

        private static void RaiseRightButtonDown(UIElement element)
        {
            element.RaiseEvent(new MouseButtonEventArgs(
                InputManager.Current.PrimaryMouseDevice, 0, MouseButton.Right)
            {
                RoutedEvent = UIElement.PreviewMouseRightButtonDownEvent,
                Source = element,
            });
        }

        private static class NativeMethods
        {
            private const uint MouseRightDown = 0x0008;
            private const uint MouseRightUp = 0x0010;
            private const uint KeyEventKeyUp = 0x0002;
            private const byte VirtualKeyEscape = 0x1B;

            [DllImport("user32.dll")]
            private static extern bool SetCursorPos(int x, int y);

            [DllImport("user32.dll")]
            private static extern void mouse_event(
                uint dwFlags, uint dx, uint dy, uint dwData, IntPtr dwExtraInfo);

            [DllImport("user32.dll")]
            private static extern void keybd_event(
                byte bVk, byte bScan, uint dwFlags, IntPtr dwExtraInfo);

            public static void MoveCursor(int x, int y) => SetCursorPos(x, y);

            public static void SendRightClick()
            {
                mouse_event(MouseRightDown, 0, 0, 0, IntPtr.Zero);
                mouse_event(MouseRightUp, 0, 0, 0, IntPtr.Zero);
            }

            public static void SendEscape()
            {
                keybd_event(VirtualKeyEscape, 0, 0, IntPtr.Zero);
                keybd_event(VirtualKeyEscape, 0, KeyEventKeyUp, IntPtr.Zero);
            }
        }
    }
}
