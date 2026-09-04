using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using NUnit.Framework;
using WinCraft.UI;

namespace WinCraft.Tests.UI.Controls
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    internal sealed class TabWindowTests
    {
        [Test]
        public void TabWindow_TemplateBindsTabsAndSelectedContent()
        {
            var brushes = (ResourceDictionary)Application.LoadComponent(
                new Uri("/WinCraft;component/UI/Theme/Brushes.Light.xaml", UriKind.Relative));
            var controls = (ResourceDictionary)Application.LoadComponent(
                new Uri("/WinCraft;component/UI/Theme/Controls.Window.xaml", UriKind.Relative));
            object first = new();
            object second = new();
            var window = new TabWindow
            {
                Width = 640,
                Height = 400,
                Left = -10000,
                Top = -10000,
                Opacity = 0,
                ShowInTaskbar = false,
                CloseWhenEmpty = false,
                NewTabCommand = new RelayCommand(() => { }),
                Tabs = new ObservableCollection<object> { first, second },
                WindowStartupLocation = WindowStartupLocation.Manual,
            };
            window.Resources.MergedDictionaries.Add(brushes);
            window.Resources.MergedDictionaries.Add(controls);
            window.Style = (Style)controls[typeof(TabWindow)];

            try
            {
                window.Show();
                window.UpdateLayout();
                var tabStrip = window.FindVisualChild<WindowTabStrip>();
                var firstTab = tabStrip.GetVisualItem<WindowTabItem>(first);
                var secondTab = tabStrip.GetVisualItem<WindowTabItem>(second);
                var newTabButton = (IconButton)window.Template.FindName(
                    "NewTabButton",
                    window);
                var scrollBackButton = (IconButton)tabStrip.Template.FindName(
                    "PART_ScrollBackButton",
                    tabStrip);
                var scrollForwardButton = (IconButton)tabStrip.Template.FindName(
                    "PART_ScrollForwardButton",
                    tabStrip);

                Assert.Multiple(() =>
                {
                    Assert.That(tabStrip, Is.Not.Null);
                    Assert.That(tabStrip.Items.Count, Is.EqualTo(2));
                    Assert.That(window.SelectedTab, Is.SameAs(first));
                    Assert.That(tabStrip.SelectedItem, Is.SameAs(first));
                    Assert.That(window.IsResizeFrameInClient, Is.False);
                    Assert.That(
                        ChromeWindow.GetHitTestRole(tabStrip),
                        Is.EqualTo(WindowHitTestRole.Caption));
                    Assert.That(
                        ChromeWindow.GetHitTestRole(firstTab),
                        Is.EqualTo(WindowHitTestRole.Client));
                    Assert.That(
                        ChromeWindow.GetHitTestRole(newTabButton),
                        Is.EqualTo(WindowHitTestRole.Client));
                    Assert.That(
                        ChromeWindow.GetHitTestRole(scrollBackButton),
                        Is.EqualTo(WindowHitTestRole.Client));
                    Assert.That(
                        ChromeWindow.GetHitTestRole(scrollForwardButton),
                        Is.EqualTo(WindowHitTestRole.Client));
                    Assert.That(newTabButton.Visibility, Is.EqualTo(Visibility.Visible));
                });

                secondTab.RaiseEvent(
                    new MouseButtonEventArgs(
                        Mouse.PrimaryDevice,
                        Environment.TickCount,
                        MouseButton.Left)
                    {
                        RoutedEvent = Mouse.PreviewMouseDownEvent,
                    });
                Assert.Multiple(() =>
                {
                    Assert.That(tabStrip.SelectedItem, Is.SameAs(second));
                    Assert.That(window.SelectedTab, Is.SameAs(second));
                    Assert.That(
                        BindingOperations.IsDataBound(
                            tabStrip,
                            Selector.SelectedItemProperty),
                        Is.True);
                });

                tabStrip.BeginPreview(
                    second,
                    secondTab.PointToScreen(
                        new Point(secondTab.ActualWidth / 2, secondTab.ActualHeight / 2)),
                    0.5);
                ProcessDispatcherFor(TimeSpan.FromMilliseconds(20));
                Assert.Multiple(() =>
                {
                    Assert.That(tabStrip.SelectedItem, Is.SameAs(second));
                    Assert.That(window.SelectedTab, Is.SameAs(second));
                    Assert.That(
                        BindingOperations.IsDataBound(
                            tabStrip,
                            Selector.SelectedItemProperty),
                        Is.True);
                });
                tabStrip.EndPreview();

                window.ShowNewTabButton = false;
                Assert.That(newTabButton.Visibility, Is.EqualTo(Visibility.Collapsed));

                window.CloseTabCommand.Execute(first);
                window.UpdateLayout();

                Assert.Multiple(() =>
                {
                    Assert.That(window.Tabs, Is.EqualTo(new[] { second }));
                    Assert.That(window.SelectedTab, Is.SameAs(second));
                    Assert.That(tabStrip.SelectedItem, Is.SameAs(second));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void TabWindow_TabClosingCanCancelRemoval()
        {
            object tab = new();
            var window = new TabWindow
            {
                CloseWhenEmpty = false,
                Tabs = new ObservableCollection<object> { tab },
            };
            window.TabClosing += (_, e) => e.Cancel = true;

            window.CloseTab(tab);

            Assert.That(window.Tabs, Is.EqualTo(new[] { tab }));
        }

        [Test]
        public void TabWindow_TabShortcutsCreateCloseAndSelectTabs()
        {
            object first = new();
            object second = new();
            object third = new();
            int newTabCount = 0;
            var window = new TabWindow
            {
                CloseWhenEmpty = false,
                NewTabCommand = new RelayCommand(() => newTabCount++),
                Tabs = new ObservableCollection<object> { first, second, third },
            };

            bool created = window.HandleTabShortcut(Key.T, ModifierKeys.Control);
            bool selectedNext = window.HandleTabShortcut(Key.Tab, ModifierKeys.Control);
            bool selectedPrevious = window.HandleTabShortcut(
                Key.Tab,
                ModifierKeys.Control | ModifierKeys.Shift);
            bool selectedThird = window.HandleTabShortcut(Key.D3, ModifierKeys.Control);
            bool closed = window.HandleTabShortcut(Key.W, ModifierKeys.Control);

            Assert.Multiple(() =>
            {
                Assert.That(created, Is.True);
                Assert.That(newTabCount, Is.EqualTo(1));
                Assert.That(selectedNext, Is.True);
                Assert.That(selectedPrevious, Is.True);
                Assert.That(selectedThird, Is.True);
                Assert.That(closed, Is.True);
                Assert.That(window.Tabs, Is.EqualTo(new[] { first, second }));
                Assert.That(window.SelectedTab, Is.SameAs(second));
            });
        }

        [Test]
        public void TabWindow_HomeTabCannotBeClosedOrMoved()
        {
            object home = new();
            object tab = new();
            var window = new TabWindow
            {
                CloseWhenEmpty = false,
                Tabs = new ObservableCollection<object> { tab },
                HomeTab = home,
            };

            window.CloseTab(home);
            window.SelectedTab = home;
            bool closedWithShortcut = window.HandleTabShortcut(Key.W, ModifierKeys.Control);
            window.Tabs.Remove(home);
            bool moved = window.MoveTabForDrag(home, 1);
            bool transferred = window.TransferTabForDrag(
                new TabWindow { CloseWhenEmpty = false },
                home,
                0);
            window.CloseTab(tab);

            Assert.Multiple(() =>
            {
                Assert.That(window.CloseTabCommand.CanExecute(home), Is.False);
                Assert.That(closedWithShortcut, Is.True);
                Assert.That(moved, Is.False);
                Assert.That(transferred, Is.False);
                Assert.That(window.Tabs, Is.EqualTo(new[] { home }));
            });
        }

        [Test]
        public void TabWindow_HomeOnlyLayoutsMoveTheWindowInsteadOfTearingOffTabs()
        {
            object home = new();
            object tab = new();
            object secondTab = new();
            var window = new TabWindow
            {
                CloseWhenEmpty = false,
                HomeTab = home,
                Tabs = new ObservableCollection<object> { home },
            };

            bool homeOnlyMovesWindow = window.IsMoveOnlyTabLayout;
            window.Tabs.Add(tab);
            bool homeAndTabMoveWindow = window.IsMoveOnlyTabLayout;
            bool tabCanTearOff = window.CanTearOffTab(tab);
            window.Tabs.Add(secondTab);

            Assert.Multiple(() =>
            {
                Assert.That(homeOnlyMovesWindow, Is.True);
                Assert.That(homeAndTabMoveWindow, Is.True);
                Assert.That(tabCanTearOff, Is.False);
                Assert.That(window.IsMoveOnlyTabLayout, Is.False);
                Assert.That(window.CanTearOffTab(tab), Is.True);
            });
        }

        [Test]
        public void TabWindow_HomeTabUsesIconOnlyWidth()
        {
            var brushes = (ResourceDictionary)Application.LoadComponent(
                new Uri("/WinCraft;component/UI/Theme/Brushes.Light.xaml", UriKind.Relative));
            var controls = (ResourceDictionary)Application.LoadComponent(
                new Uri("/WinCraft;component/UI/Theme/Controls.Window.xaml", UriKind.Relative));
            object home = new();
            object tab = new();
            var window = new TabWindow
            {
                Width = 640,
                Height = 400,
                Left = -10000,
                Top = -10000,
                Opacity = 0,
                ShowInTaskbar = false,
                CloseWhenEmpty = false,
                HomeTab = home,
                Tabs = new ObservableCollection<object> { home, tab },
                WindowStartupLocation = WindowStartupLocation.Manual,
            };
            window.Resources.MergedDictionaries.Add(brushes);
            window.Resources.MergedDictionaries.Add(controls);
            window.Style = (Style)controls[typeof(TabWindow)];

            try
            {
                window.Show();
                window.UpdateLayout();
                var strip = window.FindVisualChild<WindowTabStrip>();
                var homeItem = strip.GetVisualItem<WindowTabItem>(home);
                var tabItem = strip.GetVisualItem<WindowTabItem>(tab);

                Assert.Multiple(() =>
                {
                    Assert.That(homeItem.IsHomeTab, Is.True);
                    Assert.That(tabItem.IsHomeTab, Is.False);
                    Assert.That(
                        homeItem.ActualWidth + homeItem.Margin.Left + homeItem.Margin.Right,
                        Is.LessThan(window.TabMinWidth));
                    Assert.That(
                        tabItem.ActualWidth + tabItem.Margin.Left + tabItem.Margin.Right,
                        Is.GreaterThan(window.TabMinWidth));
                });
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void TabWindow_DragSiblingDoesNotCopyHomeTab()
        {
            object tab = new();
            var source = new HomeTabWindow { CloseWhenEmpty = false };
            object home = source.HomeTab;
            source.Tabs.Add(tab);
            var sibling = (HomeTabWindow)source.CreateDragSiblingWindow();

            try
            {
                bool transferred = source.TransferTabForDrag(sibling, tab, 0);

                Assert.Multiple(() =>
                {
                    Assert.That(transferred, Is.True);
                    Assert.That(source.Tabs, Is.EqualTo(new[] { home }));
                    Assert.That(sibling.HomeTab, Is.Null);
                    Assert.That(sibling.Tabs, Is.EqualTo(new[] { tab }));
                    Assert.That(sibling.SelectedTab, Is.SameAs(tab));
                });
            }
            finally
            {
                sibling.Close();
                source.Close();
            }
        }

        [Test]
        public void TabWindow_DragSiblingClearsDataBoundHomeTab()
        {
            object tab = new();
            var source = new DataBoundHomeTabWindow
            {
                Left = -10000,
                Top = -10000,
                Opacity = 0,
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.Manual,
            };
            DataBoundHomeTabWindow sibling = null;

            try
            {
                source.Show();
                object home = source.HomeTab;
                source.Tabs.Add(tab);
                sibling = (DataBoundHomeTabWindow)source.CreateDragSiblingWindow();
                bool transferred = source.TransferTabForDrag(sibling, tab, 0);

                Assert.Multiple(() =>
                {
                    Assert.That(transferred, Is.True);
                    Assert.That(source.Tabs, Is.EqualTo(new[] { home }));
                    Assert.That(
                        BindingOperations.IsDataBound(sibling, TabWindow.HomeTabProperty),
                        Is.False);
                    Assert.That(sibling.HomeTab, Is.Null);
                    Assert.That(sibling.Tabs, Is.EqualTo(new[] { tab }));
                });
            }
            finally
            {
                sibling?.Close();
                source.Close();
            }
        }

        [Test]
        public void TabWindow_DragSiblingInheritsMissingNewTabCommand()
        {
            var command = new RelayCommand(() => { });
            var source = new TabWindow
            {
                NewTabCommand = command,
            };

            TabWindow sibling = source.CreateDragSiblingWindow();

            Assert.That(sibling.NewTabCommand, Is.SameAs(command));
        }

        [Test]
        public void TabWindow_DetachedTabRemainsVisuallySelectedAfterDragCompletes()
        {
            var brushes = (ResourceDictionary)Application.LoadComponent(
                new Uri("/WinCraft;component/UI/Theme/Brushes.Light.xaml", UriKind.Relative));
            var controls = (ResourceDictionary)Application.LoadComponent(
                new Uri("/WinCraft;component/UI/Theme/Controls.Window.xaml", UriKind.Relative));
            object draggedTab = new();
            var source = new StyledDataBoundHomeTabWindow
            {
                Width = 640,
                Height = 400,
                Left = -10000,
                Top = -10000,
                Opacity = 0,
                ShowInTaskbar = false,
                CloseWhenEmpty = false,
                WindowStartupLocation = WindowStartupLocation.Manual,
            };
            source.Resources.MergedDictionaries.Add(brushes);
            source.Resources.MergedDictionaries.Add(controls);
            source.Style = (Style)controls[typeof(TabWindow)];
            TabWindow sibling = null;

            try
            {
                source.Show();
                source.Tabs.Add(new object());
                source.Tabs.Add(draggedTab);
                source.UpdateLayout();
                sibling = source.CreateDragSiblingWindow();
                Assert.That(source.TransferTabForDrag(sibling, draggedTab, 0), Is.True);
                sibling.ShowForTabDrag();
                sibling.SetTabDragVisual(draggedTab, true);
                sibling.SelectTabForDrag(draggedTab);
                sibling.SetTabDragVisual(draggedTab, false);
                ProcessDispatcherFor(TimeSpan.FromMilliseconds(20));

                sibling.UpdateLayout();
                var siblingStrip = sibling.FindVisualChild<WindowTabStrip>();
                var siblingItem = siblingStrip.GetVisualItem<WindowTabItem>(draggedTab);
                Assert.Multiple(() =>
                {
                    Assert.That(sibling.SelectedTab, Is.SameAs(draggedTab));
                    Assert.That(siblingStrip.SelectedItem, Is.SameAs(draggedTab));
                    Assert.That(siblingItem.IsSelected, Is.True);
                    Assert.That(siblingItem.IsDragging, Is.False);
                });
            }
            finally
            {
                sibling?.Close();
                source.Close();
            }
        }

        [Test]
        public void TabWindow_DetachingTabRestoresSourcePreviousSelectionDuringDrag()
        {
            var brushes = (ResourceDictionary)Application.LoadComponent(
                new Uri("/WinCraft;component/UI/Theme/Brushes.Light.xaml", UriKind.Relative));
            var controls = (ResourceDictionary)Application.LoadComponent(
                new Uri("/WinCraft;component/UI/Theme/Controls.Window.xaml", UriKind.Relative));
            object previousTab = new();
            object draggedTab = new();
            object trailingTab = new();
            var source = new StyledDataBoundHomeTabWindow
            {
                Width = 640,
                Height = 400,
                Left = -10000,
                Top = -10000,
                Opacity = 0,
                ShowInTaskbar = false,
                CloseWhenEmpty = false,
                WindowStartupLocation = WindowStartupLocation.Manual,
            };
            source.Resources.MergedDictionaries.Add(brushes);
            source.Resources.MergedDictionaries.Add(controls);
            source.Style = (Style)controls[typeof(TabWindow)];
            TabDragSession session = null;

            try
            {
                var viewModel = (DataBoundHomeTabViewModel)source.DataContext;
                source.Show();
                source.UpdateLayout();
                viewModel.Tabs.Add(previousTab);
                viewModel.Tabs.Add(draggedTab);
                viewModel.Tabs.Add(trailingTab);
                viewModel.SelectedTab = previousTab;
                ProcessDispatcherFor(TimeSpan.FromMilliseconds(20));
                object selectionBeforeMouseDown =
                    source.GetSelectionBeforeTabDrag(draggedTab);
                Assert.That(selectionBeforeMouseDown, Is.SameAs(previousTab));
                viewModel.SelectedTab = draggedTab;
                ProcessDispatcherFor(TimeSpan.FromMilliseconds(20));
                source.UpdateLayout();
                var sourceStrip = source.FindVisualChild<WindowTabStrip>();
                var sourceItem = sourceStrip.GetVisualItem<WindowTabItem>(draggedTab);
                session = new TabDragSession(
                    sourceStrip,
                    source,
                    draggedTab,
                    sourceItem,
                    selectionBeforeMouseDown,
                    0.5,
                    sourceItem.ActualHeight / 2);
                Point detachPoint = sourceStrip.PointToScreen(
                    new Point(
                        sourceStrip.ActualWidth / 2,
                        sourceStrip.ActualHeight + 100));

                session.Update(detachPoint);
                ProcessDispatcherFor(TimeSpan.FromMilliseconds(20));

                Assert.Multiple(() =>
                {
                    Assert.That(source.ContainsTab(draggedTab), Is.False);
                    Assert.That(source.SelectedTab, Is.SameAs(previousTab));
                    Assert.That(viewModel.SelectedTab, Is.SameAs(previousTab));
                    Assert.That(sourceStrip.SelectedItem, Is.SameAs(previousTab));
                    Assert.That(
                        source.FindVisualChild<WindowTabStrip>()
                            .GetVisualItem<WindowTabItem>(previousTab)
                            .IsSelected,
                        Is.True);
                });

                Point towardHomePoint = sourceStrip.PointToScreen(
                    new Point(1, sourceStrip.ActualHeight + 100));
                session.Update(towardHomePoint);
                ProcessDispatcherFor(TimeSpan.FromMilliseconds(20));
                Assert.Multiple(() =>
                {
                    Assert.That(source.SelectedTab, Is.SameAs(previousTab));
                    Assert.That(viewModel.SelectedTab, Is.SameAs(previousTab));
                });

                session.Complete();
            }
            finally
            {
                session?.Cancel();
                source.DragSibling?.Close();
                source.Close();
            }
        }

        [Test]
        public void TabWindow_SecondDragParksSingleTabSourceWhileAttached()
        {
            var brushes = (ResourceDictionary)Application.LoadComponent(
                new Uri("/WinCraft;component/UI/Theme/Brushes.Light.xaml", UriKind.Relative));
            var controls = (ResourceDictionary)Application.LoadComponent(
                new Uri("/WinCraft;component/UI/Theme/Controls.Window.xaml", UriKind.Relative));
            object draggedTab = new();
            var source = new TabWindow
            {
                Width = 640,
                Height = 400,
                Left = -10000,
                Top = -10000,
                Opacity = 0.72,
                ShowInTaskbar = false,
                CloseWhenEmpty = false,
                Tabs = new ObservableCollection<object> { draggedTab },
                WindowStartupLocation = WindowStartupLocation.Manual,
            };
            var target = new TabWindow
            {
                Width = 640,
                Height = 400,
                Left = -8000,
                Top = -10000,
                ShowInTaskbar = false,
                CloseWhenEmpty = false,
                Tabs = new ObservableCollection<object> { new object(), new object() },
                WindowStartupLocation = WindowStartupLocation.Manual,
            };
            source.Resources.MergedDictionaries.Add(brushes);
            source.Resources.MergedDictionaries.Add(controls);
            source.Style = (Style)controls[typeof(TabWindow)];
            target.Resources.MergedDictionaries.Add(brushes);
            target.Resources.MergedDictionaries.Add(controls);
            target.Style = (Style)controls[typeof(TabWindow)];

            try
            {
                source.Show();
                target.Show();
                source.UpdateLayout();
                target.UpdateLayout();
                var sourceStrip = source.FindVisualChild<WindowTabStrip>();
                var targetStrip = target.FindVisualChild<WindowTabStrip>();
                var sourceItem = sourceStrip.GetVisualItem<WindowTabItem>(draggedTab);
                Point detachPoint = sourceStrip.PointToScreen(
                    new Point(
                        sourceStrip.ActualWidth / 2,
                        sourceStrip.ActualHeight + 100));
                Point attachPoint = targetStrip.PointToScreen(
                    new Point(
                        targetStrip.ActualWidth / 2,
                        targetStrip.ActualHeight / 2));
                var session = new TabDragSession(
                    sourceStrip,
                    source,
                    draggedTab,
                    sourceItem,
                    source.GetSelectionBeforeTabDrag(draggedTab),
                    0.5,
                    sourceItem.ActualHeight / 2);

                session.Update(detachPoint);
                session.Update(attachPoint);
                ProcessDispatcherFor(TimeSpan.FromMilliseconds(20));
                double parkedLeft = source.Left;
                double parkedTop = source.Top;

                Assert.Multiple(() =>
                {
                    Assert.That(source.IsVisible, Is.True);
                    Assert.That(
                        source.Left + source.ActualWidth,
                        Is.LessThan(SystemParameters.VirtualScreenLeft));
                    Assert.That(source.Opacity, Is.EqualTo(0.72));
                    Assert.That(source.Tabs, Is.Empty);
                    Assert.That(target.ContainsTab(draggedTab), Is.True);
                });

                Point leaveTargetPoint = targetStrip.PointToScreen(
                    new Point(
                        targetStrip.ActualWidth / 2,
                        targetStrip.ActualHeight + 100));
                session.Update(leaveTargetPoint);

                Assert.Multiple(() =>
                {
                    Assert.That(source.ContainsTab(draggedTab), Is.True);
                    Assert.That(target.ContainsTab(draggedTab), Is.False);
                    Assert.That(Math.Abs(source.Left - parkedLeft), Is.GreaterThan(100));
                    Assert.That(Math.Abs(source.Top - parkedTop), Is.GreaterThan(100));
                });

                session.Update(attachPoint);
                Assert.Multiple(() =>
                {
                    Assert.That(source.Tabs, Is.Empty);
                    Assert.That(target.ContainsTab(draggedTab), Is.True);
                    Assert.That(source.Left, Is.EqualTo(parkedLeft).Within(0.5));
                    Assert.That(source.Top, Is.EqualTo(parkedTop).Within(0.5));
                });

                session.Complete();
                ProcessDispatcherFor(TimeSpan.FromMilliseconds(20));

                Assert.Multiple(() =>
                {
                    Assert.That(source.Left, Is.EqualTo(-10000).Within(0.5));
                    Assert.That(source.Top, Is.EqualTo(-10000).Within(0.5));
                    Assert.That(source.Opacity, Is.EqualTo(0.72));
                });
            }
            finally
            {
                source.Close();
                target.Close();
            }
        }

        [Test]
        public void TabWindow_DragSiblingKeepsItsDataBoundTabState()
        {
            var source = new DataBoundTabWindow
            {
                Left = -10000,
                Top = -10000,
                Opacity = 0,
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.Manual,
            };
            DataBoundTabWindow sibling = null;

            try
            {
                source.Show();
                sibling = (DataBoundTabWindow)source.CreateDragSiblingWindow();
                var siblingViewModel = (DataBoundTabViewModel)sibling.DataContext;

                sibling.NewTabCommand.Execute(null);

                Assert.Multiple(() =>
                {
                    Assert.That(
                        BindingOperations.IsDataBound(sibling, TabWindow.TabsProperty),
                        Is.True);
                    Assert.That(
                        BindingOperations.IsDataBound(sibling, TabWindow.SelectedTabProperty),
                        Is.True);
                    Assert.That(
                        BindingOperations.IsDataBound(sibling, TabWindow.NewTabCommandProperty),
                        Is.True);
                    Assert.That(sibling.Tabs, Is.SameAs(siblingViewModel.Tabs));
                    Assert.That(sibling.Tabs.Count, Is.EqualTo(1));
                    Assert.That(sibling.SelectedTab, Is.SameAs(sibling.Tabs[0]));
                });
            }
            finally
            {
                sibling?.Close();
                source.Close();
            }
        }

        [Test]
        public void TabWindow_ManyTabsRespectMinimumWidthAndShowScrollButtons()
        {
            var brushes = (ResourceDictionary)Application.LoadComponent(
                new Uri("/WinCraft;component/UI/Theme/Brushes.Light.xaml", UriKind.Relative));
            var controls = (ResourceDictionary)Application.LoadComponent(
                new Uri("/WinCraft;component/UI/Theme/Controls.Window.xaml", UriKind.Relative));
            var tabs = new ObservableCollection<object>();
            for (int index = 0; index < 3; index++)
                tabs.Add($"Tab {index + 1}");

            var window = new TabWindow
            {
                Width = 640,
                Height = 400,
                Left = -10000,
                Top = -10000,
                Opacity = 0,
                ShowInTaskbar = false,
                CloseWhenEmpty = false,
                Tabs = tabs,
                WindowStartupLocation = WindowStartupLocation.Manual,
            };
            window.Resources.MergedDictionaries.Add(brushes);
            window.Resources.MergedDictionaries.Add(controls);
            window.Style = (Style)controls[typeof(TabWindow)];

            try
            {
                window.Show();
                ProcessDispatcherFor(TimeSpan.FromMilliseconds(50));
                window.UpdateLayout();
                var strip = window.FindVisualChild<WindowTabStrip>();
                var panel = strip.FindVisualChild<WindowTabPanel>();
                var scrollViewer = (ScrollViewer)strip.Template.FindName(
                    "PART_TabScrollViewer",
                    strip);
                var scrollBackButton = (IconButton)strip.Template.FindName(
                    "PART_ScrollBackButton",
                    strip);
                var scrollForwardButton = (IconButton)strip.Template.FindName(
                    "PART_ScrollForwardButton",
                    strip);
                Assert.Multiple(() =>
                {
                    Assert.That(scrollBackButton.Visibility, Is.EqualTo(Visibility.Collapsed));
                    Assert.That(scrollForwardButton.Visibility, Is.EqualTo(Visibility.Collapsed));
                });

                for (int index = tabs.Count; index < 24; index++)
                    tabs.Add($"Tab {index + 1}");
                ProcessDispatcherFor(TimeSpan.FromMilliseconds(50));
                window.UpdateLayout();

                var activeTab = strip.GetVisualItem<WindowTabItem>(0);
                var inactiveTab = strip.GetVisualItem<WindowTabItem>(1);
                var lastTab = strip.GetVisualItem<WindowTabItem>(tabs.Count - 1);
                Point lastOrigin = lastTab.TranslatePoint(new Point(0, 0), panel);
                double availableWidth = strip.ActualWidth;
                Assert.Multiple(() =>
                {
                    Assert.That(
                        activeTab.ActualWidth + activeTab.Margin.Left + activeTab.Margin.Right,
                        Is.EqualTo(window.TabMinWidth).Within(0.5));
                    Assert.That(
                        inactiveTab.ActualWidth + inactiveTab.Margin.Left + inactiveTab.Margin.Right,
                        Is.EqualTo(window.TabMinWidth).Within(0.5));
                    Assert.That(scrollViewer.ExtentWidth, Is.GreaterThan(scrollViewer.ViewportWidth));
                    Assert.That(scrollBackButton.Visibility, Is.EqualTo(Visibility.Visible));
                    Assert.That(scrollForwardButton.Visibility, Is.EqualTo(Visibility.Visible));
                    Assert.That(scrollBackButton.IsEnabled, Is.False);
                    Assert.That(scrollForwardButton.IsEnabled, Is.True);
                    Assert.That(
                        lastOrigin.X + lastTab.ActualWidth,
                        Is.LessThanOrEqualTo(panel.ActualWidth + 0.5));
                });

                scrollForwardButton.RaiseEvent(
                    new RoutedEventArgs(Button.ClickEvent));
                ProcessDispatcherFor(TimeSpan.FromMilliseconds(180));

                Assert.Multiple(() =>
                {
                    Assert.That(scrollViewer.HorizontalOffset, Is.GreaterThan(0));
                    Assert.That(scrollBackButton.IsEnabled, Is.True);
                });

                tabs.RemoveAt(tabs.Count - 1);
                window.UpdateLayout();
                inactiveTab = strip.GetVisualItem<WindowTabItem>(1);

                Assert.Multiple(() =>
                {
                    Assert.That(strip.ActualWidth, Is.EqualTo(availableWidth).Within(0.5));
                    Assert.That(
                        inactiveTab.ActualWidth + inactiveTab.Margin.Left + inactiveTab.Margin.Right,
                        Is.EqualTo(window.TabMinWidth).Within(0.5));
                });

                while (tabs.Count > 2)
                    tabs.RemoveAt(tabs.Count - 1);

                window.UpdateLayout();
                activeTab = strip.GetVisualItem<WindowTabItem>(0);
                inactiveTab = strip.GetVisualItem<WindowTabItem>(1);

                Assert.Multiple(() =>
                {
                    Assert.That(strip.ActualWidth, Is.EqualTo(availableWidth).Within(0.5));
                    Assert.That(scrollBackButton.Visibility, Is.EqualTo(Visibility.Collapsed));
                    Assert.That(scrollForwardButton.Visibility, Is.EqualTo(Visibility.Collapsed));
                    Assert.That(activeTab.IsCompact, Is.False);
                    Assert.That(inactiveTab.IsCompact, Is.False);
                    Assert.That(activeTab.ActualWidth, Is.GreaterThan(200));
                    Assert.That(inactiveTab.ActualWidth, Is.EqualTo(activeTab.ActualWidth).Within(0.5));
                });
            }
            finally
            {
                window.Close();
            }
        }

        private static void ProcessDispatcherFor(TimeSpan duration)
        {
            var frame = new DispatcherFrame();
            var timer = new DispatcherTimer(
                DispatcherPriority.ApplicationIdle,
                Dispatcher.CurrentDispatcher)
            {
                Interval = duration,
            };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                frame.Continue = false;
            };
            timer.Start();
            Dispatcher.PushFrame(frame);
        }

        private sealed class DataBoundTabWindow : TabWindow
        {
            public DataBoundTabWindow()
            {
                SetBinding(TabsProperty, new Binding(nameof(DataBoundTabViewModel.Tabs)));
                SetBinding(
                    SelectedTabProperty,
                    new Binding(nameof(DataBoundTabViewModel.SelectedTab))
                    {
                        Mode = BindingMode.TwoWay,
                    });
                SetBinding(
                    NewTabCommandProperty,
                    new Binding(nameof(DataBoundTabViewModel.NewTabCommand)));
                DataContext = new DataBoundTabViewModel();
            }
        }

        private sealed class StyledDataBoundHomeTabWindow : DataBoundHomeTabWindow
        {
            public TabWindow DragSibling { get; private set; }

            protected override TabWindow CreateSiblingWindow()
            {
                var sibling = new StyledDataBoundHomeTabWindow();
                foreach (ResourceDictionary dictionary in Resources.MergedDictionaries)
                    sibling.Resources.MergedDictionaries.Add(dictionary);
                sibling.Style = Style;
                DragSibling = sibling;
                return sibling;
            }
        }

        private sealed class HomeTabWindow : TabWindow
        {
            public HomeTabWindow()
            {
                HomeTab = new object();
                Tabs = new ObservableCollection<object> { HomeTab };
            }
        }

        private class DataBoundHomeTabWindow : TabWindow
        {
            public DataBoundHomeTabWindow()
            {
                SetBinding(TabsProperty, new Binding(nameof(DataBoundHomeTabViewModel.Tabs)));
                SetBinding(HomeTabProperty, new Binding(nameof(DataBoundHomeTabViewModel.HomeTab)));
                SetBinding(
                    SelectedTabProperty,
                    new Binding(nameof(DataBoundHomeTabViewModel.SelectedTab))
                    {
                        Mode = BindingMode.TwoWay,
                    });
                DataContext = new DataBoundHomeTabViewModel();
            }
        }

        private sealed class DataBoundHomeTabViewModel : ObservableObject
        {
            public DataBoundHomeTabViewModel()
            {
                HomeTab = new object();
                Tabs = new ObservableCollection<object> { HomeTab };
                SelectedTab = HomeTab;
            }

            public object HomeTab { get; }

            public ObservableCollection<object> Tabs { get; }

            public object SelectedTab
            {
                get => GetValue<object>();
                set => SetValue(value);
            }
        }

        private sealed class DataBoundTabViewModel : ObservableObject
        {
            public DataBoundTabViewModel()
            {
                Tabs = new ObservableCollection<object> { new object() };
                SelectedTab = Tabs[0];
                NewTabCommand = new RelayCommand(AddTab);
            }

            public ObservableCollection<object> Tabs { get; }

            public object SelectedTab
            {
                get => GetValue<object>();
                set => SetValue(value);
            }

            public RelayCommand NewTabCommand { get; }

            private void AddTab()
            {
                var tab = new object();
                Tabs.Add(tab);
                SelectedTab = tab;
            }
        }
    }
}
