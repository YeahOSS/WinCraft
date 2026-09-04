using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using NUnit.Framework;
using WinCraft.UI;

namespace WinCraft.Tests.UI.Theme
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    internal sealed class PasswordRevealTests
    {
        [Test]
        public void PasswordReveal_UpdatesThePasswordFromVisibleText()
        {
            var passwordBox = CreatePasswordBox("initial");
            var window = CreateHiddenWindow(passwordBox);

            try
            {
                PasswordReveal.SetIsEnabled(passwordBox, true);
                window.Show();
                passwordBox.Focus();
                PasswordReveal.SetIsRevealed(passwordBox, true);
                ProcessDispatcherQueue();

                var revealedTextBox = GetRevealedTextBox(passwordBox);
                revealedTextBox.Text = "visible";

                Assert.That(passwordBox.Password, Is.EqualTo("visible"));
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void PasswordReveal_RevealedWhileUnfocused_SynchronizesVisibleText()
        {
            var passwordBox = CreatePasswordBox("secret");
            PasswordReveal.SetIsEnabled(passwordBox, true);
            passwordBox.ApplyTemplate();

            Assert.That(passwordBox.IsKeyboardFocusWithin, Is.False);

            PasswordReveal.SetIsRevealed(passwordBox, true);

            Assert.That(GetRevealedTextBox(passwordBox).Text, Is.EqualTo("secret"));
        }

        [Test]
        public void PasswordReveal_DisablingHidesAndClearsTheVisibleText()
        {
            var passwordBox = CreatePasswordBox("secret");
            var window = CreateHiddenWindow(passwordBox);

            try
            {
                PasswordReveal.SetIsEnabled(passwordBox, true);
                window.Show();
                passwordBox.Focus();
                PasswordReveal.SetIsRevealed(passwordBox, true);
                ProcessDispatcherQueue();

                var revealedTextBox = GetRevealedTextBox(passwordBox);
                Assert.That(revealedTextBox.Text, Is.EqualTo("secret"));

                PasswordReveal.SetIsEnabled(passwordBox, false);

                Assert.That(PasswordReveal.GetIsRevealed(passwordBox), Is.False);
                Assert.That(revealedTextBox.Text, Is.Empty);
                Assert.That(passwordBox.Password, Is.EqualTo("secret"));
            }
            finally
            {
                window.Close();
            }
        }

        private static PasswordBox CreatePasswordBox(string password)
        {
            return new PasswordBox
            {
                Password = password,
                Style = GetStyle(),
            };
        }

        private static Window CreateHiddenWindow(PasswordBox passwordBox)
        {
            return new Window
            {
                Content = passwordBox,
                Height = 1,
                Left = -10000,
                Opacity = 0,
                ShowInTaskbar = false,
                Top = -10000,
                Width = 1,
                WindowStyle = WindowStyle.None,
            };
        }

        private static TextBox GetRevealedTextBox(PasswordBox passwordBox)
        {
            return (TextBox)passwordBox.Template.FindName(
                "PART_RevealedTextBox",
                passwordBox);
        }

        private static Style GetStyle()
        {
            var resources = new ResourceDictionary
            {
                Source = new Uri(
                    "/WinCraft;component/UI/Theme/Controls.Input.xaml",
                    UriKind.Relative),
            };

            return (Style)resources[typeof(PasswordBox)];
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
    }
}
