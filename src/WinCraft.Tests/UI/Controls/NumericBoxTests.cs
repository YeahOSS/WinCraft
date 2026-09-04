using System;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using NUnit.Framework;
using WinCraft.UI;

namespace WinCraft.Tests.UI.Controls
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    internal sealed class NumericBoxTests
    {
        [Test]
        public void Maximum_BelowMinimum_AlignsMinimumToTheLastConfiguredEndpoint()
        {
            var box = new NumericBox { Minimum = 10 };

            box.Maximum = 5;

            Assert.That(box.Minimum, Is.EqualTo(5));
            Assert.That(box.Maximum, Is.EqualTo(5));
            Assert.That(box.Value, Is.EqualTo(5));
        }

        [Test]
        public void Minimum_AboveMaximum_AlignsMaximumToTheLastConfiguredEndpoint()
        {
            var box = new NumericBox { Maximum = 5 };

            box.Minimum = 10;

            Assert.That(box.Minimum, Is.EqualTo(10));
            Assert.That(box.Maximum, Is.EqualTo(10));
            Assert.That(box.Value, Is.EqualTo(10));
        }

        [Test]
        public void TextAboveMaximum_ImmediatelyDisplaysTheCoercedValue()
        {
            var box = new NumericBox { Maximum = 100 };

            ((TextBox)box).Text = "101";

            Assert.That(box.Value, Is.EqualTo(100));
            Assert.That(((TextBox)box).Text, Is.EqualTo("100"));
        }

        [Test]
        public void TextSlightlyAboveMaximum_ImmediatelyDisplaysTheCoercedValue()
        {
            var box = new NumericBox
            {
                InputType = NumericInputType.Decimal,
                DecimalPlaces = 12,
                Maximum = 1,
            };
            var separator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;

            ((TextBox)box).Text = "1" + separator + "0000000000005";

            Assert.That(box.Value, Is.EqualTo(1));
            Assert.That(((TextBox)box).Text, Is.EqualTo(1d.ToString("F12", CultureInfo.CurrentCulture)));
        }

        [Test]
        public void MouseWheel_WithKeyboardFocus_AdjustsTheValueByStep()
        {
            var window = CreateOffScreenWindow();
            var box = new NumericBox { Value = 5 };
            window.Content = box;
            window.Show();
            box.Focus();
            try
            {
                Assert.That(box.IsKeyboardFocused, Is.True);

                RaiseMouseWheel(box, 120);
                Assert.That(box.Value, Is.EqualTo(6));

                RaiseMouseWheel(box, -120);
                Assert.That(box.Value, Is.EqualTo(5));
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void MouseWheel_WithoutKeyboardFocus_DoesNotConsumeTheWheel()
        {
            var box = new NumericBox { Value = 5 };
            var args = new MouseWheelEventArgs(Mouse.PrimaryDevice, Environment.TickCount, 120)
            {
                RoutedEvent = UIElement.MouseWheelEvent,
            };

            box.RaiseEvent(args);

            Assert.That(box.Value, Is.EqualTo(5));
            Assert.That(args.Handled, Is.False);
        }

        private static Window CreateOffScreenWindow()
        {
            var window = new Window
            {
                WindowStyle = WindowStyle.None,
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Left = -10000,
                Top = -10000,
                Width = 200,
                Height = 100,
                Opacity = 0,
            };
            new WindowInteropHelper(window).EnsureHandle();
            return window;
        }

        private static void RaiseMouseWheel(UIElement element, int delta)
        {
            var args = new MouseWheelEventArgs(Mouse.PrimaryDevice, Environment.TickCount, delta)
            {
                RoutedEvent = UIElement.MouseWheelEvent,
            };
            element.RaiseEvent(args);
        }
    }
}
