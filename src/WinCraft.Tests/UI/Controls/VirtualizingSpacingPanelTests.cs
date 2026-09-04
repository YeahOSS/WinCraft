using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using NUnit.Framework;
using WinCraft.UI;

namespace WinCraft.Tests.UI.Controls
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    internal sealed class VirtualizingSpacingPanelTests
    {
        private const int ItemCount = 1000;

        [Test]
        public void VirtualizingSpacingStackPanel_RealizesOnlyViewportItems()
        {
            var panelFactory = new FrameworkElementFactory(typeof(VirtualizingSpacingStackPanel));
            panelFactory.SetValue(VirtualizingSpacingStackPanel.ItemHeightProperty, 20.0);
            panelFactory.SetValue(VirtualizingSpacingStackPanel.SpacingProperty, 4.0);
            var listBox = CreateListBox(panelFactory);
            var window = CreateWindow(listBox);

            try
            {
                window.Show();
                listBox.UpdateLayout();

                var panel = FindVisualChild<VirtualizingSpacingStackPanel>(listBox);
                Assert.That(panel, Is.Not.Null);
                Assert.That(panel.ExtentHeight, Is.EqualTo(ItemCount * 20 + (ItemCount - 1) * 4));
                Assert.That(CountRealizedContainers(listBox), Is.LessThan(ItemCount));

                panel.SetVerticalOffset(480);
                listBox.UpdateLayout();

                Assert.That(listBox.ItemContainerGenerator.ContainerFromIndex(0), Is.Null);
                Assert.That(CountRealizedContainers(listBox), Is.LessThan(ItemCount));
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void VirtualizingSpacingWrapPanel_RealizesOnlyViewportItems()
        {
            var panelFactory = new FrameworkElementFactory(typeof(VirtualizingSpacingWrapPanel));
            panelFactory.SetValue(VirtualizingSpacingWrapPanel.ItemWidthProperty, 40.0);
            panelFactory.SetValue(VirtualizingSpacingWrapPanel.ItemHeightProperty, 20.0);
            panelFactory.SetValue(VirtualizingSpacingWrapPanel.SpacingProperty, 4.0);
            var listBox = CreateListBox(panelFactory);
            var window = CreateWindow(listBox);

            try
            {
                window.Show();
                listBox.UpdateLayout();

                var panel = FindVisualChild<VirtualizingSpacingWrapPanel>(listBox);
                Assert.That(panel, Is.Not.Null);
                Assert.That(panel.ExtentHeight, Is.GreaterThan(1000.0));
                Assert.That(CountRealizedContainers(listBox), Is.LessThan(ItemCount));

                panel.SetVerticalOffset(480);
                listBox.UpdateLayout();

                Assert.That(listBox.ItemContainerGenerator.ContainerFromIndex(0), Is.Null);
                Assert.That(CountRealizedContainers(listBox), Is.LessThan(ItemCount));
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void VirtualizingSpacingPanels_UseTheSelectedDesignToken()
        {
            var stackPanel = new VirtualizingSpacingStackPanel();
            stackPanel.Resources["SpacingLarge"] = 20.0;
            LayoutTokens.SetSpacing(stackPanel, Spacing.Large);

            var wrapPanel = new VirtualizingSpacingWrapPanel();
            wrapPanel.Resources["SpacingSmall"] = 4.0;
            LayoutTokens.SetSpacing(wrapPanel, Spacing.Small);

            Assert.That(stackPanel.Spacing, Is.EqualTo(20));
            Assert.That(wrapPanel.Spacing, Is.EqualTo(4));
        }

        private static ListBox CreateListBox(FrameworkElementFactory panelFactory)
        {
            var listBox = new ListBox
            {
                Width = 200,
                Height = 100,
                ItemsPanel = new ItemsPanelTemplate(panelFactory)
            };

            ScrollViewer.SetCanContentScroll(listBox, true);
            VirtualizingPanel.SetIsVirtualizing(listBox, true);
            for (var index = 0; index < ItemCount; index++)
                listBox.Items.Add(index);

            return listBox;
        }

        private static Window CreateWindow(FrameworkElement content)
        {
            return new Window
            {
                Content = content,
                Width = 200,
                Height = 100,
                WindowStyle = WindowStyle.None,
                ShowInTaskbar = false,
                Opacity = 0
            };
        }

        private static int CountRealizedContainers(ItemsControl control)
        {
            var count = 0;
            for (var index = 0; index < ItemCount; index++)
            {
                if (control.ItemContainerGenerator.ContainerFromIndex(index) != null)
                    count++;
            }

            return count;
        }

        private static T FindVisualChild<T>(DependencyObject root)
            where T : DependencyObject
        {
            for (var childIndex = 0; childIndex < VisualTreeHelper.GetChildrenCount(root); childIndex++)
            {
                var child = VisualTreeHelper.GetChild(root, childIndex);
                if (child is T result)
                    return result;

                result = FindVisualChild<T>(child);
                if (result != null)
                    return result;
            }

            return null;
        }
    }
}
