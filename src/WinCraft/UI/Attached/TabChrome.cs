using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace WinCraft.UI
{
    public static class TabChrome
    {
        private static readonly DependencyProperty GeneratorStatusStateProperty =
            DependencyProperty.RegisterAttached(
                "GeneratorStatusState",
                typeof(TabChromeState),
                typeof(TabChrome),
                new FrameworkPropertyMetadata(null));

        private static readonly DependencyProperty UpdatePendingProperty =
            DependencyProperty.RegisterAttached(
                "UpdatePending",
                typeof(bool),
                typeof(TabChrome),
                new FrameworkPropertyMetadata(false));

        private static readonly DependencyPropertyKey IsSeparatorVisiblePropertyKey =
            DependencyProperty.RegisterAttachedReadOnly(
                "IsSeparatorVisible",
                typeof(bool),
                typeof(TabChrome),
                new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty ShowUnselectedTabSeparatorsProperty =
            DependencyProperty.RegisterAttached(
                "ShowUnselectedTabSeparators",
                typeof(bool),
                typeof(TabChrome),
                new FrameworkPropertyMetadata(false, OnShowUnselectedTabSeparatorsChanged));

        public static readonly DependencyProperty IsSeparatorVisibleProperty =
            IsSeparatorVisiblePropertyKey.DependencyProperty;

        public static bool GetShowUnselectedTabSeparators(TabControl target) =>
            (bool)target.GetValue(ShowUnselectedTabSeparatorsProperty);

        public static void SetShowUnselectedTabSeparators(TabControl target, bool value) =>
            target.SetValue(ShowUnselectedTabSeparatorsProperty, value);

        public static bool GetIsSeparatorVisible(TabItem target) =>
            (bool)target.GetValue(IsSeparatorVisibleProperty);

        private static void OnShowUnselectedTabSeparatorsChanged(
            DependencyObject target,
            DependencyPropertyChangedEventArgs e)
        {
            if (target is not TabControl tabControl)
                return;

            if (e.OldValue is true)
            {
                tabControl.Loaded -= OnTabControlLoaded;
                tabControl.SelectionChanged -= OnTabControlSelectionChanged;
                UnsubscribeFromGeneratorStatus(tabControl);
                ClearSeparators(tabControl);
            }

            if (e.NewValue is true)
            {
                tabControl.Loaded += OnTabControlLoaded;
                tabControl.SelectionChanged += OnTabControlSelectionChanged;
                SubscribeToGeneratorStatus(tabControl);
                ScheduleSeparatorUpdate(tabControl);
            }
        }

        private static void OnTabControlLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is TabControl tabControl)
                ScheduleSeparatorUpdate(tabControl);
        }

        private static void OnTabControlSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is TabControl tabControl && ReferenceEquals(e.OriginalSource, tabControl))
                ScheduleSeparatorUpdate(tabControl);
        }

        private static void SubscribeToGeneratorStatus(TabControl tabControl)
        {
            if (tabControl.GetValue(GeneratorStatusStateProperty) is TabChromeState)
                return;

            var state = new TabChromeState(tabControl);
            tabControl.SetValue(GeneratorStatusStateProperty, state);
            tabControl.ItemContainerGenerator.StatusChanged += state.Handler;
        }

        private static void UnsubscribeFromGeneratorStatus(TabControl tabControl)
        {
            if (tabControl.GetValue(GeneratorStatusStateProperty) is not TabChromeState state)
                return;

            tabControl.ItemContainerGenerator.StatusChanged -= state.Handler;
            tabControl.ClearValue(GeneratorStatusStateProperty);
        }

        private static void ScheduleSeparatorUpdate(TabControl tabControl)
        {
            if ((bool)tabControl.GetValue(UpdatePendingProperty))
                return;

            tabControl.SetValue(UpdatePendingProperty, true);
            tabControl.Dispatcher.BeginInvoke(
                DispatcherPriority.Loaded,
                new Action(() =>
                {
                    tabControl.SetValue(UpdatePendingProperty, false);
                    if (GetShowUnselectedTabSeparators(tabControl))
                        UpdateSeparators(tabControl);
                }));
        }

        private static void ClearSeparators(TabControl tabControl)
        {
            for (var index = 0; index < tabControl.Items.Count; index++)
            {
                if (tabControl.GetVisualItem<TabItem>(index) is TabItem tabItem &&
                    GetIsSeparatorVisible(tabItem))
                {
                    SetIsSeparatorVisible(tabItem, false);
                }
            }
        }

        private static void UpdateSeparators(TabControl tabControl)
        {
            var selectedIndex = tabControl.SelectedIndex;
            var itemCount = tabControl.Items.Count;

            for (var index = 0; index < itemCount; index++)
            {
                if (tabControl.GetVisualItem<TabItem>(index) is not TabItem tabItem)
                    continue;

                var isVisible =
                    index < itemCount - 1 &&
                    index != selectedIndex &&
                    index + 1 != selectedIndex;

                if (GetIsSeparatorVisible(tabItem) != isVisible)
                    SetIsSeparatorVisible(tabItem, isVisible);
            }
        }

        private static void SetIsSeparatorVisible(TabItem target, bool value) =>
            target.SetValue(IsSeparatorVisiblePropertyKey, value);

        private sealed class TabChromeState
        {
            public TabChromeState(TabControl tabControl)
            {
                Handler = (sender, e) => ScheduleSeparatorUpdate(tabControl);
            }

            public EventHandler Handler { get; }
        }
    }
}
