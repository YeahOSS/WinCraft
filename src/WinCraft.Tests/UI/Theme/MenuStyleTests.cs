using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using NUnit.Framework;
using WinCraft.UI;

namespace WinCraft.Tests.UI.Theme
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    internal sealed class MenuStyleTests
    {
        [Test]
        public void Menu_TopLevelSubmenuOpensBelowWithoutAnArrow()
        {
            var styles = LoadStyles();
            var nestedItem = CreateMenuItem(styles, "Open");
            var topLevelItem = CreateMenuItem(styles, "File");
            topLevelItem.Items.Add(nestedItem);

            var menu = new Menu { Style = styles.MenuStyle };
            menu.Items.Add(topLevelItem);
            var window = ShowInWindow(menu);
            try
            {
                ApplyTemplates(menu, topLevelItem, nestedItem);

                var popup = (Popup)topLevelItem.Template.FindName("PART_Popup", topLevelItem);
                var arrow = (IconBlock)topLevelItem.Template.FindName("SubMenuArrow", topLevelItem);

                Assert.That(topLevelItem.Role, Is.EqualTo(MenuItemRole.TopLevelHeader));
                Assert.That(popup.Placement, Is.EqualTo(PlacementMode.Bottom));
                Assert.That(arrow.Visibility, Is.EqualTo(Visibility.Collapsed));
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void ContextMenu_SubmenuKeepsRightPlacementAndArrow()
        {
            var styles = LoadStyles();
            var nestedItem = CreateMenuItem(styles, "Open");
            var contextItem = CreateMenuItem(styles, "File");
            contextItem.Items.Add(nestedItem);

            var contextMenu = new ContextMenu { Style = styles.ContextMenuStyle };
            contextMenu.Items.Add(contextItem);
            var target = new Button { ContextMenu = contextMenu };
            var window = ShowInWindow(target);
            try
            {
                contextMenu.PlacementTarget = target;
                contextMenu.IsOpen = true;
                ApplyTemplates(contextMenu, contextItem, nestedItem);

                var popup = (Popup)contextItem.Template.FindName("PART_Popup", contextItem);
                var arrow = (IconBlock)contextItem.Template.FindName("SubMenuArrow", contextItem);

                Assert.That(contextItem.Role, Is.EqualTo(MenuItemRole.SubmenuHeader));
                Assert.That(popup.Placement, Is.EqualTo(PlacementMode.Right));
                Assert.That(arrow.Visibility, Is.EqualTo(Visibility.Visible));
            }
            finally
            {
                contextMenu.IsOpen = false;
                window.Close();
            }
        }

        [Test]
        public void Menu_IconOnlyCentersTheTopLevelIconAndUsesCompactWidth()
        {
            var styles = LoadStyles();
            var topLevelItem = CreateMenuItem(styles, "File");
            topLevelItem.Items.Add(CreateMenuItem(styles, "Open"));
            Design.SetIcon(topLevelItem, IconGlyph.Navigation16);

            var menu = new Menu { Style = styles.MenuStyle };
            MenuLayout.SetIsIconOnly(menu, true);
            menu.Items.Add(topLevelItem);
            var window = ShowInWindow(menu);
            try
            {
                ApplyTemplates(menu, topLevelItem);

                var iconHost = (Grid)topLevelItem.Template.FindName("IconHost", topLevelItem);
                var icon = (IconBlock)topLevelItem.Template.FindName("Icon", topLevelItem);
                var header = (ContentPresenter)topLevelItem.Template.FindName("HeaderHost", topLevelItem);
                var iconCenter = icon.TranslatePoint(
                    new Point(icon.ActualWidth / 2, icon.ActualHeight / 2),
                    topLevelItem);

                Assert.That(topLevelItem.GetValue(MenuLayout.IsIconOnlyProperty), Is.True);
                Assert.That(header.Visibility, Is.EqualTo(Visibility.Collapsed));
                Assert.That(Grid.GetColumnSpan(iconHost), Is.EqualTo(4));
                Assert.That(double.IsNaN(iconHost.Width), Is.True);
                Assert.That(topLevelItem.MinWidth, Is.EqualTo(40));
                Assert.That(topLevelItem.ActualWidth, Is.EqualTo(40).Within(0.5));
                Assert.That(
                    iconCenter.X,
                    Is.EqualTo(topLevelItem.ActualWidth / 2).Within(0.5));
            }
            finally
            {
                window.Close();
            }
        }

        private static MenuItem CreateMenuItem(MenuStyles styles, string header) =>
            new() { Header = header, Style = styles.MenuItemStyle };

        private static void ApplyTemplates(ItemsControl owner, params MenuItem[] items)
        {
            owner.ApplyTemplate();
            foreach (var item in items)
                item.ApplyTemplate();
        }

        private static Window ShowInWindow(FrameworkElement content)
        {
            var window = new Window
            {
                Content = content,
                Height = 1,
                Left = -10000,
                Opacity = 0,
                ShowInTaskbar = false,
                Top = -10000,
                Width = 240,
                WindowStyle = WindowStyle.None,
            };
            window.Show();
            window.UpdateLayout();
            return window;
        }

        private static MenuStyles LoadStyles()
        {
            var resources = (ResourceDictionary)Application.LoadComponent(
                new Uri(
                    "/WinCraft;component/UI/Theme/Controls.Menus.xaml",
                    UriKind.Relative));

            return new MenuStyles(
                (Style)resources[typeof(Menu)],
                (Style)resources[typeof(ContextMenu)],
                (Style)resources[typeof(MenuItem)]);
        }

        private sealed class MenuStyles
        {
            public MenuStyles(
                Style menuStyle,
                Style contextMenuStyle,
                Style menuItemStyle)
            {
                MenuStyle = menuStyle;
                ContextMenuStyle = contextMenuStyle;
                MenuItemStyle = menuItemStyle;
            }

            public Style MenuStyle { get; }

            public Style ContextMenuStyle { get; }

            public Style MenuItemStyle { get; }
        }
    }
}
