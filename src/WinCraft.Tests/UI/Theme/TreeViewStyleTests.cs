using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using NUnit.Framework;
using WinCraft.UI;

namespace WinCraft.Tests.UI.Theme
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    internal sealed class TreeViewStyleTests
    {
        [Test]
        public void TreeViewItem_HeaderHeightMatchesTheRowHeightWhenExpanded()
        {
            var styles = LoadStyles();
            var leaf = CreateItem(styles, "Leaf");
            var branch = CreateItem(styles, "Branch", isExpanded: true);
            branch.Items.Add(leaf);
            var root = CreateItem(styles, "Root", isExpanded: true);
            root.Items.Add(branch);
            var foreground = new SolidColorBrush(Colors.CornflowerBlue);
            var cornerRadius = new CornerRadius(6);
            root.Foreground = foreground;
            Design.SetCornerRadius(root, cornerRadius);
            var treeView = new TreeView
            {
                Height = 200,
                Style = styles.TreeViewStyle,
            };
            treeView.Items.Add(root);
            var window = ShowInWindow(treeView);

            try
            {
                window.UpdateLayout();

                var rootHeader = (SuperellipseBorder)root.Template.FindName("Border", root);
                var leafHeader = (SuperellipseBorder)leaf.Template.FindName("Border", leaf);
                var expandButton = (ToggleButton)root.Template.FindName("ExpandButton", root);
                var chevronIcon = (IconBlock)expandButton.Template.FindName("ChevronIcon", expandButton);

                Assert.That(rootHeader.ActualHeight, Is.EqualTo(leafHeader.ActualHeight).Within(0.01));
                Assert.That(rootHeader.CornerRadius, Is.EqualTo(cornerRadius));
                Assert.That(expandButton.ActualHeight, Is.EqualTo(16).Within(0.01));
                Assert.That(expandButton.Style, Is.Null);
                Assert.That(expandButton.Focusable, Is.False);
                Assert.That(chevronIcon.Foreground, Is.SameAs(foreground));
                Assert.That(
                    expandButton.TranslatePoint(new Point(), rootHeader).X,
                    Is.EqualTo(root.Padding.Left).Within(0.01));
                Assert.That(root.ActualHeight, Is.GreaterThan(rootHeader.ActualHeight));
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void TreeView_ContentViewportExcludesItsPadding()
        {
            var styles = LoadStyles();
            var treeView = new TreeView
            {
                Height = 100,
                Style = styles.TreeViewStyle,
            };
            treeView.Items.Add(CreateItem(styles, "Item"));
            var window = ShowInWindow(treeView);

            try
            {
                window.UpdateLayout();

                var scrollViewer = treeView.FindVisualChild<ScrollViewer>();

                Assert.That(treeView.FocusVisualStyle, Is.Null);
                Assert.That(ScrollViewer.GetCanContentScroll(treeView), Is.False);
                Assert.That(scrollViewer.ViewportHeight, Is.EqualTo(
                    treeView.ActualHeight
                    - treeView.Padding.Top
                    - treeView.Padding.Bottom).Within(0.01));
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void TreeView_PropagatesScrollViewerSettings()
        {
            var styles = LoadStyles();
            var treeView = new TreeView
            {
                Height = 100,
                Style = styles.TreeViewStyle,
            };
            ScrollViewer.SetCanContentScroll(treeView, true);
            ScrollViewer.SetHorizontalScrollBarVisibility(treeView, ScrollBarVisibility.Hidden);
            ScrollViewer.SetVerticalScrollBarVisibility(treeView, ScrollBarVisibility.Disabled);
            treeView.Items.Add(CreateItem(styles, "Item"));
            var window = ShowInWindow(treeView);

            try
            {
                window.UpdateLayout();

                var scrollViewer = treeView.FindVisualChild<ScrollViewer>();

                Assert.That(scrollViewer.CanContentScroll, Is.True);
                Assert.That(scrollViewer.HorizontalScrollBarVisibility, Is.EqualTo(ScrollBarVisibility.Hidden));
                Assert.That(scrollViewer.VerticalScrollBarVisibility, Is.EqualTo(ScrollBarVisibility.Disabled));
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void TreeViewItem_DisabledSelectionUsesTheDisabledSelectionBackground()
        {
            var styles = LoadStyles();
            var disabledSelectionBackground = new SolidColorBrush(Colors.DarkGray);
            var item = CreateItem(styles, "Item");
            var treeView = new TreeView
            {
                Height = 100,
                Style = styles.TreeViewStyle,
            };
            treeView.Resources["ControlCheckedDisabledBackground"] = disabledSelectionBackground;
            treeView.Items.Add(item);
            var window = ShowInWindow(treeView);

            try
            {
                item.IsSelected = true;
                item.IsEnabled = false;
                window.UpdateLayout();

                var border = (SuperellipseBorder)item.Template.FindName("Border", item);

                Assert.That(border.Background, Is.SameAs(disabledSelectionBackground));
            }
            finally
            {
                window.Close();
            }
        }

        private static TreeViewItem CreateItem(
            TreeViewStyles styles,
            string header,
            bool isExpanded = false) =>
            new()
            {
                Header = header,
                IsExpanded = isExpanded,
                Style = styles.TreeViewItemStyle,
            };

        private static Window ShowInWindow(FrameworkElement content)
        {
            var window = new Window
            {
                Content = content,
                Height = 240,
                Left = -10000,
                Opacity = 0,
                ShowInTaskbar = false,
                Top = -10000,
                Width = 400,
                WindowStyle = WindowStyle.None,
            };
            window.Show();
            window.UpdateLayout();
            return window;
        }

        private static TreeViewStyles LoadStyles()
        {
            var resources = (ResourceDictionary)Application.LoadComponent(
                new Uri(
                    "/WinCraft;component/UI/Theme/Controls.Extended.xaml",
                    UriKind.Relative));

            return new TreeViewStyles(
                (Style)resources[typeof(TreeView)],
                (Style)resources[typeof(TreeViewItem)]);
        }

        private sealed class TreeViewStyles
        {
            public TreeViewStyles(Style treeViewStyle, Style treeViewItemStyle)
            {
                TreeViewStyle = treeViewStyle;
                TreeViewItemStyle = treeViewItemStyle;
            }

            public Style TreeViewStyle { get; }

            public Style TreeViewItemStyle { get; }
        }
    }
}
