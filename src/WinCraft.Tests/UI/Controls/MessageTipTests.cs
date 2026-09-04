using System.Threading;
using System.Windows;
using NUnit.Framework;
using WinCraft.UI;

namespace WinCraft.Tests.UI.Controls
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    internal sealed class MessageTipTests
    {
        [Test]
        public void MessageTipPlacement_CentersAtTheOwnerTopEighth()
        {
            // Keep the popup inside the virtual screen: offscreen owners get
            // clamped back by the popup layer on multi-monitor desktops.
            var screenLeft = SystemParameters.VirtualScreenLeft + 100;
            var screenTop = SystemParameters.VirtualScreenTop + 100;
            var owner = new Window
            {
                Width = 640,
                Height = 480,
                Left = screenLeft,
                Top = screenTop,
                Opacity = 0,
                WindowStartupLocation = WindowStartupLocation.Manual,
            };
            var tip = new MessageTipControl
            {
                Message = "Test",
                Width = 200,
                Height = 50,
                AutoHideDelay = 0,
                Opacity = 0,
            };

            try
            {
                owner.Show();
                tip.Show(owner);
                owner.UpdateLayout();
                tip.UpdateLayout();

                var expected = owner.PointToScreen(
                    new Point(owner.ActualWidth / 2, owner.ActualHeight / 8));
                var actual = tip.PointToScreen(new Point(tip.ActualWidth / 2, 0));

                Assert.That(actual.X, Is.EqualTo(expected.X).Within(1));
                Assert.That(actual.Y, Is.EqualTo(expected.Y).Within(1));

                owner.Left = screenLeft + 500;
                owner.Top = screenTop + 500;
                owner.UpdateLayout();
                tip.UpdateLayout();

                expected = owner.PointToScreen(
                    new Point(owner.ActualWidth / 2, owner.ActualHeight / 8));
                actual = tip.PointToScreen(new Point(tip.ActualWidth / 2, 0));

                Assert.That(actual.X, Is.EqualTo(expected.X).Within(1));
                Assert.That(actual.Y, Is.EqualTo(expected.Y).Within(1));
            }
            finally
            {
                tip.Visibility = Visibility.Collapsed;
                owner.Close();
            }
        }
    }
}
