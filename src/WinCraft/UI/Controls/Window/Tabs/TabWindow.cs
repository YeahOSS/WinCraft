using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using WinCraft.Compatibility;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace WinCraft.UI
{
    public class TabWindow : ChromeWindow
    {
        private const double DragParkingMargin = 64;

        public static readonly DependencyProperty TabsProperty =
            DependencyProperty.Register(
                nameof(Tabs),
                typeof(IList),
                typeof(TabWindow),
                new PropertyMetadata(null, OnTabsChanged));

        public static readonly DependencyProperty SelectedTabProperty =
            DependencyProperty.Register(
                nameof(SelectedTab),
                typeof(object),
                typeof(TabWindow),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnSelectedTabChanged));

        public static readonly DependencyProperty TabHeaderTemplateProperty =
            DependencyProperty.Register(
                nameof(TabHeaderTemplate),
                typeof(DataTemplate),
                typeof(TabWindow),
                new PropertyMetadata(null));

        public static readonly DependencyProperty TabContentTemplateProperty =
            DependencyProperty.Register(
                nameof(TabContentTemplate),
                typeof(DataTemplate),
                typeof(TabWindow),
                new PropertyMetadata(null));

        public static readonly DependencyProperty NewTabCommandProperty =
            DependencyProperty.Register(
                nameof(NewTabCommand),
                typeof(ICommand),
                typeof(TabWindow),
                new PropertyMetadata(null));

        public static readonly DependencyProperty ShowNewTabButtonProperty =
            DependencyProperty.Register(
                nameof(ShowNewTabButton),
                typeof(bool),
                typeof(TabWindow),
                new PropertyMetadata(true));

        public static readonly DependencyProperty TitleBarFooterProperty =
            DependencyProperty.Register(
                nameof(TitleBarFooter),
                typeof(object),
                typeof(TabWindow),
                new PropertyMetadata(null));

        public static readonly DependencyProperty HomeTabProperty =
            DependencyProperty.Register(
                nameof(HomeTab),
                typeof(object),
                typeof(TabWindow),
                new PropertyMetadata(null, OnHomeTabChanged));

        public static readonly DependencyProperty CloseWhenEmptyProperty =
            DependencyProperty.Register(
                nameof(CloseWhenEmpty),
                typeof(bool),
                typeof(TabWindow),
                new PropertyMetadata(true));

        public static readonly DependencyProperty CanReorderTabsProperty =
            DependencyProperty.Register(
                nameof(CanReorderTabs),
                typeof(bool),
                typeof(TabWindow),
                new PropertyMetadata(true));

        public static readonly DependencyProperty CanTearOffTabsProperty =
            DependencyProperty.Register(
                nameof(CanTearOffTabs),
                typeof(bool),
                typeof(TabWindow),
                new PropertyMetadata(true));

        public static readonly DependencyProperty TabMinWidthProperty =
            DependencyProperty.Register(
                nameof(TabMinWidth),
                typeof(double),
                typeof(TabWindow),
                new PropertyMetadata(96.0, null, CoerceTabWidth));

        public static readonly DependencyProperty TabMaxWidthProperty =
            DependencyProperty.Register(
                nameof(TabMaxWidth),
                typeof(double),
                typeof(TabWindow),
                new PropertyMetadata(240.0, null, CoerceTabWidth));

        private INotifyCollectionChanged _observableTabs;
        private bool _isSynchronizingHomeTab;
        private bool _isRestoringTabDragSelection;
        private object _previousSelectedTab;

        public TabWindow()
        {
            SetResourceReference(StyleProperty, typeof(TabWindow));
            Tabs = new ObservableCollection<object>();
            CloseTabCommand = new RelayCommand<object>(CloseTab)
            {
                CanExecuteFunc = CanCloseTab
            };
            ShowIcon = false;
            Closed += OnTabWindowClosed;
        }

        public event EventHandler<TabClosingEventArgs> TabClosing;

        public IList Tabs
        {
            get => (IList)GetValue(TabsProperty);
            set => SetValue(TabsProperty, value);
        }

        public object SelectedTab
        {
            get => GetValue(SelectedTabProperty);
            set => SetValue(SelectedTabProperty, value);
        }

        public DataTemplate TabHeaderTemplate
        {
            get => (DataTemplate)GetValue(TabHeaderTemplateProperty);
            set => SetValue(TabHeaderTemplateProperty, value);
        }

        public DataTemplate TabContentTemplate
        {
            get => (DataTemplate)GetValue(TabContentTemplateProperty);
            set => SetValue(TabContentTemplateProperty, value);
        }

        public ICommand NewTabCommand
        {
            get => (ICommand)GetValue(NewTabCommandProperty);
            set => SetValue(NewTabCommandProperty, value);
        }

        public bool ShowNewTabButton
        {
            get => (bool)GetValue(ShowNewTabButtonProperty);
            set => SetValue(ShowNewTabButtonProperty, value);
        }

        public object TitleBarFooter
        {
            get => GetValue(TitleBarFooterProperty);
            set => SetValue(TitleBarFooterProperty, value);
        }

        public object HomeTab
        {
            get => GetValue(HomeTabProperty);
            set => SetValue(HomeTabProperty, value);
        }

        public ICommand CloseTabCommand { get; }

        public bool CloseWhenEmpty
        {
            get => (bool)GetValue(CloseWhenEmptyProperty);
            set => SetValue(CloseWhenEmptyProperty, value);
        }

        public bool CanReorderTabs
        {
            get => (bool)GetValue(CanReorderTabsProperty);
            set => SetValue(CanReorderTabsProperty, value);
        }

        public bool CanTearOffTabs
        {
            get => (bool)GetValue(CanTearOffTabsProperty);
            set => SetValue(CanTearOffTabsProperty, value);
        }

        public double TabMinWidth
        {
            get => (double)GetValue(TabMinWidthProperty);
            set => SetValue(TabMinWidthProperty, value);
        }

        public double TabMaxWidth
        {
            get => (double)GetValue(TabMaxWidthProperty);
            set => SetValue(TabMaxWidthProperty, value);
        }

        public void CloseTab(object tab)
        {
            if (!CanCloseTab(tab))
                return;

            var args = new TabClosingEventArgs(tab);
            TabClosing?.Invoke(this, args);
            if (args.Cancel)
                return;

            int index = TabCollectionTransfer.IndexOfReference(Tabs, tab);
            Tabs.RemoveAt(index);
            SelectAfterRemoval(index);
            ScheduleCloseIfEmpty(null);
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);

            if (!e.Handled && HandleTabShortcut(e.Key, Keyboard.Modifiers))
                e.Handled = true;
        }

        protected virtual TabWindow CreateSiblingWindow()
        {
            if (Activator.CreateInstance(GetType()) is TabWindow sibling)
                return sibling;

            return new TabWindow();
        }

        internal bool ContainsTab(object tab) =>
            tab != null && TabCollectionTransfer.IndexOfReference(Tabs, tab) >= 0;

        public bool CanCloseTab(object tab) => IsRegularTab(tab);

        internal bool CanMoveTab(object tab) => IsRegularTab(tab);

        internal bool IsMoveOnlyTabLayout => HomeTab != null && Tabs.Count <= 2;

        internal bool CanTearOffTab(object tab) =>
            CanTearOffTabs && CanMoveTab(tab) && !IsMoveOnlyTabLayout;

        private bool IsRegularTab(object tab) =>
            ContainsTab(tab) && !IsHomeTab(tab);

        private bool IsHomeTab(object tab) =>
            tab != null && ReferenceEquals(tab, HomeTab);

        internal bool CanAcceptTab(object tab) =>
            tab != null && TabCollectionTransfer.CanModify(Tabs);

        internal int NormalizeInsertionIndex(int insertionIndex) =>
            HomeTab == null ? insertionIndex : Math.Max(1, insertionIndex);

        internal bool HandleTabShortcut(Key key, ModifierKeys modifiers)
        {
            if ((modifiers & ModifierKeys.Control) == 0
                || (modifiers & ~(ModifierKeys.Control | ModifierKeys.Shift)) != 0)
            {
                return false;
            }

            bool hasShift = (modifiers & ModifierKeys.Shift) != 0;
            if (key == Key.T && !hasShift)
            {
                if (NewTabCommand == null || !NewTabCommand.CanExecute(null))
                    return false;

                NewTabCommand.Execute(null);
                return true;
            }

            if (key == Key.W && !hasShift)
            {
                if (!ContainsTab(SelectedTab))
                    return false;

                CloseTab(SelectedTab);
                return true;
            }

            if (key == Key.Tab)
                return SelectAdjacentTab(hasShift ? -1 : 1);

            return !hasShift
                && TryGetTabShortcutIndex(key, out int index)
                && SelectTabAt(index);
        }

        internal bool TransferTabTo(TabWindow target, object tab, int insertionIndex)
        {
            if (target == null || !CanMoveTab(tab) || !target.CanAcceptTab(tab))
                return false;

            bool moved = TabCollectionTransfer.Move(
                Tabs,
                target.Tabs,
                tab,
                target.NormalizeInsertionIndex(insertionIndex));
            if (!moved)
                return ReferenceEquals(this, target);

            if (ReferenceEquals(this, target))
            {
                SetSelectedTab(tab);
                return true;
            }

            target.SetSelectedTab(tab);
            target.Activate();
            ScheduleCloseIfEmpty(target);
            return true;
        }

        internal bool TransferTabForDrag(
            TabWindow target,
            object tab,
            int insertionIndex)
        {
            if (target == null || !CanMoveTab(tab) || !target.CanAcceptTab(tab))
                return false;

            bool moved = TabCollectionTransfer.Move(
                Tabs,
                target.Tabs,
                tab,
                target.NormalizeInsertionIndex(insertionIndex));
            if (!moved)
            {
                return ReferenceEquals(this, target);
            }

            target.SetSelectedTab(tab);
            return true;
        }

        internal bool MoveTabForDrag(object tab, int targetIndex)
        {
            if (!CanMoveTab(tab))
                return false;

            bool moved = TabCollectionTransfer.MoveToIndex(
                Tabs,
                tab,
                NormalizeInsertionIndex(targetIndex));
            SetSelectedTab(tab);
            return moved;
        }

        internal object GetSelectionBeforeTabDrag(object draggedTab)
        {
            if (!ReferenceEquals(SelectedTab, draggedTab) && ContainsTab(SelectedTab))
                return SelectedTab;

            if (!ReferenceEquals(_previousSelectedTab, draggedTab)
                && ContainsTab(_previousSelectedTab))
            {
                return _previousSelectedTab;
            }

            for (int index = (Tabs?.Count ?? 0) - 1; index >= 0; index--)
            {
                object tab = Tabs[index];
                if (!ReferenceEquals(tab, draggedTab) && !IsHomeTab(tab))
                    return tab;
            }

            return HomeTab;
        }
        internal void SelectTabForDrag(object tab)
        {
            if (tab != null && !ContainsTab(tab))
                return;

            SetSelectedTab(tab);
            var strip = this.FindVisualChild<WindowTabStrip>();
            if (strip != null)
            {
                DependencyObjectCompat.SetControlValue(strip, Selector.SelectedItemProperty, tab);
                var item = strip.GetVisualItem<WindowTabItem>(tab);
                if (item != null && !item.IsSelected)
                    DependencyObjectCompat.SetControlValue(item, Selector.IsSelectedProperty, true);
            }
            BindingOperations.GetBindingExpression(this, SelectedTabProperty)?.UpdateSource();
        }

        internal void RestoreSelectionAfterTabDrag(object tab)
        {
            if (!ContainsTab(tab))
                return;

            try
            {
                _isRestoringTabDragSelection = true;
                SelectTabForDrag(tab);
            }
            finally
            {
                _isRestoringTabDragSelection = false;
            }
        }

        internal TabWindow CreateDragSiblingWindow()
        {
            var sibling = CreateSiblingWindow();
            if (sibling == null)
                return null;

            CopyWindowSettings(sibling);
            sibling.WindowStartupLocation = WindowStartupLocation.Manual;
            Rect bounds = WindowState == WindowState.Normal || RestoreBounds.IsEmpty
                ? new Rect(Left, Top, GetTransferWidth(), GetTransferHeight())
                : RestoreBounds;
            sibling.Left = bounds.Left;
            sibling.Top = bounds.Top;
            sibling.ShowActivated = false;
            EnsureSiblingBindings(sibling);
            ClearSiblingTabs(sibling);
            return sibling;
        }

        internal void ShowForTabDrag()
        {
            if (!IsVisible)
                Show();

            UpdateLayout();
            BringToFrontWithoutActivation();
        }

        internal void SetTabDragVisual(object tab, bool isDragging)
        {
            if (tab == null)
                return;

            UpdateLayout();
            var strip = this.FindVisualChild<WindowTabStrip>();
            strip?.GetVisualItem<WindowTabItem>(tab)?.SetIsDragging(isDragging);
        }

        internal void BringToFrontForTabDrag() =>
            BringToFrontWithoutActivation();

        internal void ParkOutsideWorkspaceForTabDrag()
        {
            Left = SystemParameters.VirtualScreenLeft
                - GetTransferWidth()
                - DragParkingMargin;
            Top = SystemParameters.VirtualScreenTop
                - GetTransferHeight()
                - DragParkingMargin;
        }

        internal bool AlignTabToScreenPoint(
            object tab,
            double grabRatio,
            double grabY,
            Point screenPoint)
        {
            UpdateLayout();
            var strip = this.FindVisualChild<WindowTabStrip>();
            var item = strip?.GetVisualItem<WindowTabItem>(tab);
            if (item == null || item.ActualWidth <= 0 || item.ActualHeight <= 0)
                return false;

            Point current = item.PointToScreen(
                new Point(
                    Math.Max(0, Math.Min(item.ActualWidth, item.ActualWidth * grabRatio)),
                    Math.Max(0, Math.Min(item.ActualHeight, grabY))));
            MoveByScreenPixels(screenPoint.X - current.X, screenPoint.Y - current.Y, true);
            return true;
        }

        internal void MoveByScreenPixels(double deltaX, double deltaY, bool bringToFront = false)
        {
            IntPtr handle = new WindowInteropHelper(this).Handle;
            if (handle == IntPtr.Zero
                || !PInvoke.GetWindowRect((HWND)handle, out RECT bounds))
            {
                double scale = GetDpiScale();
                if (scale <= 0)
                    scale = 1;

                Left += deltaX / scale;
                Top += deltaY / scale;
                return;
            }

            var flags = SET_WINDOW_POS_FLAGS.SWP_NOSIZE
                | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE;
            if (!bringToFront)
                flags |= SET_WINDOW_POS_FLAGS.SWP_NOZORDER;

            PInvoke.SetWindowPos(
                (HWND)handle,
                HWND.Null,
                bounds.left + (int)Math.Round(deltaX),
                bounds.top + (int)Math.Round(deltaY),
                0,
                0,
                flags);
        }

        internal void CompleteTabDrag()
        {
            ShowActivated = true;
            if (IsVisible)
                Activate();
        }

        internal void CloseIfEmptyAfterDrag(TabWindow replacement) =>
            ScheduleCloseIfEmpty(replacement);

        internal void TearOffTab(object tab, Point screenPoint)
        {
            if (!CanTearOffTab(tab))
                return;

            if (Tabs.Count == 1)
            {
                MoveSingleTabWindow(screenPoint);
                return;
            }

            var sibling = CreateSiblingWindow();
            if (sibling == null)
                return;

            CopyWindowSettings(sibling);
            PositionSibling(sibling, screenPoint);
            EnsureSiblingBindings(sibling);
            ClearSiblingTabs(sibling);

            if (!TabCollectionTransfer.Move(
                    Tabs,
                    sibling.Tabs,
                    tab,
                    sibling.NormalizeInsertionIndex(0)))
                return;

            sibling.SetSelectedTab(tab);
            sibling.Show();
            sibling.Activate();
            ScheduleCloseIfEmpty(sibling);
        }

        private static object CoerceTabWidth(DependencyObject d, object baseValue)
        {
            double value = (double)baseValue;
            return double.IsNaN(value) || double.IsInfinity(value) || value < 0
                ? 0.0
                : value;
        }

        private static void OnTabsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var window = (TabWindow)d;
            if (window._observableTabs != null)
                window._observableTabs.CollectionChanged -= window.OnTabsCollectionChanged;

            window._observableTabs = e.NewValue as INotifyCollectionChanged;
            if (window._observableTabs != null)
                window._observableTabs.CollectionChanged += window.OnTabsCollectionChanged;

            window.SynchronizeHomeTab();
            window.RefreshHomeTabState();
            window.EnsureSelection();
            CommandManager.InvalidateRequerySuggested();
        }

        private static void OnHomeTabChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var window = (TabWindow)d;
            window.SynchronizeHomeTab();
            window.RefreshHomeTabState();
            window.EnsureSelection();
            CommandManager.InvalidateRequerySuggested();
        }

        private static void OnSelectedTabChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            var window = (TabWindow)d;
            if (!window._isRestoringTabDragSelection
                && e.OldValue != null
                && !ReferenceEquals(e.OldValue, e.NewValue)
                && window.ContainsTab(e.OldValue))
            {
                window._previousSelectedTab = e.OldValue;
            }
        }

        private void OnTabsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            SynchronizeHomeTab();
            EnsureSelection();
            CommandManager.InvalidateRequerySuggested();
        }

        private void OnTabWindowClosed(object sender, EventArgs e)
        {
            if (_observableTabs != null)
                _observableTabs.CollectionChanged -= OnTabsCollectionChanged;
        }

        private void EnsureSelection()
        {
            if (Tabs == null || Tabs.Count == 0)
            {
                SetSelectedTab(null);
                return;
            }

            if (!ContainsTab(SelectedTab))
                SetSelectedTab(Tabs[0]);
        }

        private void SelectAfterRemoval(int removedIndex)
        {
            if (Tabs.Count == 0)
            {
                SetSelectedTab(null);
                return;
            }

            SetSelectedTab(Tabs[Math.Min(removedIndex, Tabs.Count - 1)]);
        }

        private void SetSelectedTab(object tab)
        {
            DependencyObjectCompat.SetControlValue(this, SelectedTabProperty, tab);
        }

        private static void ClearSiblingTabs(TabWindow sibling)
        {
            sibling.Tabs ??= new ObservableCollection<object>();

            if (!TabCollectionTransfer.CanModify(sibling.Tabs))
                throw new InvalidOperationException("The sibling TabWindow must expose a mutable Tabs collection.");

            sibling.ClearValue(HomeTabProperty);
            for (int index = sibling.Tabs.Count - 1; index >= 0; index--)
                sibling.Tabs.RemoveAt(index);
        }

        private void CopyWindowSettings(TabWindow sibling)
        {
            sibling.Width = GetTransferWidth();
            sibling.Height = GetTransferHeight();
            sibling.MinWidth = MinWidth;
            sibling.MinHeight = MinHeight;
            sibling.MaxWidth = MaxWidth;
            sibling.MaxHeight = MaxHeight;
            sibling.ResizeMode = ResizeMode;
            sibling.Topmost = Topmost;
            if (!BindingOperations.IsDataBound(sibling, TitleProperty))
                sibling.Title = Title;
            sibling.ShowMinimize = ShowMinimize;
            sibling.ShowMaximize = ShowMaximize;
            sibling.ShowClose = ShowClose;
            sibling.IsResizeFrameInClient = IsResizeFrameInClient;
            sibling.ResizeFrameThickness = ResizeFrameThickness;
            sibling.TabMinWidth = TabMinWidth;
            sibling.TabMaxWidth = TabMaxWidth;
            sibling.ShowNewTabButton = ShowNewTabButton;
            sibling.CanReorderTabs = CanReorderTabs;
            sibling.CanTearOffTabs = CanTearOffTabs;
            sibling.CloseWhenEmpty = CloseWhenEmpty;
            sibling.BackdropType = BackdropType;
            sibling.DwmCornerPreference = DwmCornerPreference;
            sibling.DwmUseDarkMode = DwmUseDarkMode;
            sibling.DwmBorderColorMode = DwmBorderColorMode;
            sibling.DwmBorderColor = DwmBorderColor;
            sibling.KeepActiveVisuals = KeepActiveVisuals;

            sibling.TabHeaderTemplate ??= TabHeaderTemplate;
            sibling.TabContentTemplate ??= TabContentTemplate;
            if (!BindingOperations.IsDataBound(sibling, NewTabCommandProperty)
                && sibling.NewTabCommand == null)
                sibling.NewTabCommand = NewTabCommand;
        }

        private void SynchronizeHomeTab()
        {
            if (_isSynchronizingHomeTab
                || HomeTab == null
                || !TabCollectionTransfer.CanModify(Tabs))
            {
                return;
            }

            try
            {
                _isSynchronizingHomeTab = true;
                int index = TabCollectionTransfer.IndexOfReference(Tabs, HomeTab);
                if (index < 0)
                    Tabs.Insert(0, HomeTab);
                else if (index > 0)
                    TabCollectionTransfer.MoveToIndex(Tabs, HomeTab, 0);
            }
            finally
            {
                _isSynchronizingHomeTab = false;
            }
        }

        private void RefreshHomeTabState() =>
            this.FindVisualChild<WindowTabStrip>()?.RefreshHomeTabState();

        private bool SelectAdjacentTab(int offset)
        {
            if (Tabs == null || Tabs.Count == 0)
                return false;

            int index = TabCollectionTransfer.IndexOfReference(Tabs, SelectedTab);
            if (index < 0)
                index = 0;

            index = (index + offset + Tabs.Count) % Tabs.Count;
            SetSelectedTab(Tabs[index]);
            return true;
        }

        private bool SelectTabAt(int index)
        {
            if (Tabs == null || Tabs.Count == 0)
                return false;

            if (index == int.MaxValue)
                index = Tabs.Count - 1;
            if (index < 0 || index >= Tabs.Count)
                return false;

            SetSelectedTab(Tabs[index]);
            return true;
        }

        private static bool TryGetTabShortcutIndex(Key key, out int index)
        {
            switch (key)
            {
                case Key.D1:
                case Key.NumPad1:
                    index = 0;
                    return true;
                case Key.D2:
                case Key.NumPad2:
                    index = 1;
                    return true;
                case Key.D3:
                case Key.NumPad3:
                    index = 2;
                    return true;
                case Key.D4:
                case Key.NumPad4:
                    index = 3;
                    return true;
                case Key.D5:
                case Key.NumPad5:
                    index = 4;
                    return true;
                case Key.D6:
                case Key.NumPad6:
                    index = 5;
                    return true;
                case Key.D7:
                case Key.NumPad7:
                    index = 6;
                    return true;
                case Key.D8:
                case Key.NumPad8:
                    index = 7;
                    return true;
                case Key.D9:
                case Key.NumPad9:
                    index = int.MaxValue;
                    return true;
                default:
                    index = -1;
                    return false;
            }
        }

        private static void EnsureSiblingBindings(TabWindow sibling)
        {
            if (sibling.IsLoaded
                || (!BindingOperations.IsDataBound(sibling, TabsProperty)
                    && !BindingOperations.IsDataBound(sibling, SelectedTabProperty)
                    && !BindingOperations.IsDataBound(sibling, NewTabCommandProperty)))
            {
                return;
            }

            bool showActivated = sibling.ShowActivated;
            bool showInTaskbar = sibling.ShowInTaskbar;
            double opacity = sibling.Opacity;
            sibling.ShowActivated = false;
            sibling.ShowInTaskbar = false;
            sibling.Opacity = 0;

            try
            {
                sibling.Show();
                sibling.UpdateLayout();
                sibling.Hide();
            }
            finally
            {
                sibling.Opacity = opacity;
                sibling.ShowInTaskbar = showInTaskbar;
                sibling.ShowActivated = showActivated;
            }
        }

        private void PositionSibling(TabWindow sibling, Point screenPoint)
        {
            double scale = GetDpiScale();
            if (scale <= 0)
                scale = 1;

            sibling.WindowStartupLocation = WindowStartupLocation.Manual;
            sibling.Left = screenPoint.X / scale - Math.Min(sibling.Width * 0.25, 180);
            sibling.Top = screenPoint.Y / scale - sibling.GetTitleBarHeight() / 2.0;
        }

        private void MoveSingleTabWindow(Point screenPoint)
        {
            if (WindowState != WindowState.Normal)
                WindowState = WindowState.Normal;

            double scale = GetDpiScale();
            if (scale <= 0)
                scale = 1;

            Left = screenPoint.X / scale - Math.Min(ActualWidth * 0.25, 180);
            Top = screenPoint.Y / scale - GetTitleBarHeight() / 2.0;
            Activate();
        }

        private void BringToFrontWithoutActivation()
        {
            IntPtr handle = new WindowInteropHelper(this).Handle;
            if (handle == IntPtr.Zero
                || !PInvoke.GetWindowRect((HWND)handle, out RECT bounds))
            {
                return;
            }

            PInvoke.SetWindowPos(
                (HWND)handle,
                HWND.Null,
                bounds.left,
                bounds.top,
                0,
                0,
                SET_WINDOW_POS_FLAGS.SWP_NOSIZE
                | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE
                | SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW);
        }

        private double GetTitleBarHeight()
        {
            var titleBar = this.FindVisualChild<TitleBar>();
            return titleBar?.ActualHeight > 0 ? titleBar.ActualHeight : 32.0;
        }

        private double GetTransferWidth()
        {
            if (WindowState != WindowState.Normal && !RestoreBounds.IsEmpty)
                return RestoreBounds.Width;

            return ActualWidth > 0 ? ActualWidth : Width;
        }

        private double GetTransferHeight()
        {
            if (WindowState != WindowState.Normal && !RestoreBounds.IsEmpty)
                return RestoreBounds.Height;

            return ActualHeight > 0 ? ActualHeight : Height;
        }

        private void ScheduleCloseIfEmpty(TabWindow replacement)
        {
            if (!CloseWhenEmpty || Tabs == null || Tabs.Count != 0)
                return;

            Dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new Action(() =>
                {
                    if (Tabs == null || Tabs.Count != 0)
                        return;

                    if (replacement != null
                        && Application.Current != null
                        && ReferenceEquals(Application.Current.MainWindow, this))
                    {
                        Application.Current.MainWindow = replacement;
                    }

                    Close();
                }));
        }
    }
}
