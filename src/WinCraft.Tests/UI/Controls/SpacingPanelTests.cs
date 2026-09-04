using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NUnit.Framework;
using WinCraft.UI;

namespace WinCraft.Tests.UI.Controls
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    internal sealed class SpacingPanelTests
    {
        [Test]
        public void SpacingStackPanel_Vertical_UsesSpacingBetweenVisibleChildren()
        {
            var panel = new SpacingStackPanel { Spacing = 5 };
            var first = CreateChild(20, 10);
            var second = CreateChild(20, 10);
            var third = CreateChild(20, 10);
            panel.Children.Add(first);
            panel.Children.Add(second);
            panel.Children.Add(third);

            Layout(panel, new Size(100, 40));

            Assert.That(panel.DesiredSize, Is.EqualTo(new Size(20, 40)));
            Assert.That(GetOffset(second, panel).Y, Is.EqualTo(15));
            Assert.That(GetOffset(third, panel).Y, Is.EqualTo(30));
        }

        [Test]
        public void SpacingWrapPanel_Horizontal_WrapsWithSpacingBetweenLines()
        {
            var panel = new SpacingWrapPanel { Spacing = 5 };
            var first = CreateChild(10, 10);
            var second = CreateChild(10, 10);
            var third = CreateChild(10, 10);
            panel.Children.Add(first);
            panel.Children.Add(second);
            panel.Children.Add(third);

            Layout(panel, new Size(25, 25));

            Assert.That(panel.DesiredSize, Is.EqualTo(new Size(25, 25)));
            Assert.That(GetOffset(second, panel).X, Is.EqualTo(15));
            Assert.That(GetOffset(third, panel).Y, Is.EqualTo(15));
        }

        [Test]
        public void SpacingUniformGrid_UsesSpacingBetweenCells()
        {
            var panel = new SpacingUniformGrid { Columns = 3, Rows = 1, Spacing = 5 };
            var first = CreateChild(10, 10);
            var second = CreateChild(10, 10);
            var third = CreateChild(10, 10);
            panel.Children.Add(first);
            panel.Children.Add(second);
            panel.Children.Add(third);

            Layout(panel, new Size(40, 10));

            Assert.That(panel.DesiredSize, Is.EqualTo(new Size(40, 10)));
            Assert.That(second.ActualWidth, Is.EqualTo(10));
            Assert.That(GetOffset(third, panel).X, Is.EqualTo(30));
        }

        [Test]
        public void SpacingDockPanel_ReservesSpacingBeforeFillChild()
        {
            var panel = new SpacingDockPanel { Spacing = 5 };
            var docked = CreateChild(10, 20);
            var fill = new Border();
            SpacingDockPanel.SetDock(docked, Dock.Left);
            panel.Children.Add(docked);
            panel.Children.Add(fill);

            Layout(panel, new Size(50, 20));

            Assert.That(GetOffset(fill, panel).X, Is.EqualTo(15));
            Assert.That(fill.ActualWidth, Is.EqualTo(35));
        }

        [Test]
        public void SpacingDockPanel_RendersOnlyRequestedBorderEdges()
        {
            var panel = new SpacingDockPanel
            {
                BorderBrush = Brushes.Red,
                BorderThickness = new Thickness(0, 0, 0, 1)
            };
            var child = new Border();
            panel.Children.Add(child);

            Layout(panel, new Size(20, 20));
            var bitmap = new RenderTargetBitmap(20, 20, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(panel);

            Assert.That(child.ActualHeight, Is.EqualTo(20));
            Assert.That(GetPixel(bitmap, 10, 0).A, Is.Zero);
            Assert.That(GetPixel(bitmap, 0, 10).A, Is.Zero);
            Assert.That(GetPixel(bitmap, 19, 10).A, Is.Zero);
            Assert.That(GetPixel(bitmap, 10, 19), Is.EqualTo(Colors.Red));
        }

        [Test]
        public void LayoutSpacing_UsesTheSelectedDesignToken()
        {
            var panel = new SpacingStackPanel();
            panel.Resources["SpacingLarge"] = 20.0;

            LayoutTokens.SetSpacing(panel, Spacing.Large);

            Assert.That(panel.Spacing, Is.EqualTo(20));

            LayoutTokens.SetSpacing(panel, Spacing.None);

            Assert.That(panel.Spacing, Is.EqualTo(0));
        }

        [Test]
        public void SpacingItemsControl_UsesTheSelectedDesignToken()
        {
            var control = new SpacingItemsControl();
            control.Resources["SpacingLarge"] = 20.0;

            LayoutTokens.SetSpacing(control, Spacing.Large);

            Assert.That(control.Spacing, Is.EqualTo(20));
            Assert.That(control.ItemsPanel, Is.Not.Null);
        }

        private static Border CreateChild(double width, double height) =>
            new Border { Width = width, Height = height };

        private static void Layout(Panel panel, Size size)
        {
            panel.Measure(size);
            panel.Arrange(new Rect(size));
        }

        private static Point GetOffset(UIElement element, Visual ancestor) =>
            element.TransformToAncestor(ancestor).Transform(new Point());

        private static Color GetPixel(BitmapSource bitmap, int x, int y)
        {
            var pixels = new byte[4];
            bitmap.CopyPixels(new Int32Rect(x, y, 1, 1), pixels, 4, 0);
            return Color.FromArgb(pixels[3], pixels[2], pixels[1], pixels[0]);
        }
    }
}
