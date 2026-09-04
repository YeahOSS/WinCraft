using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using WinCraft.Compatibility;

namespace WinCraft.UI
{
    [TemplatePart(Name = ScrollViewerPartName, Type = typeof(ScrollViewer))]
    [TemplatePart(Name = ScrollBackButtonPartName, Type = typeof(ButtonBase))]
    [TemplatePart(Name = ScrollForwardButtonPartName, Type = typeof(ButtonBase))]
    public sealed class WindowTabStrip : ListBox
    {
        private const double TargetMagnetism = 16;
        private const double ScrollEdgeSize = 32;
        private const double ScrollEpsilon = 0.5;
        private const string ScrollViewerPartName = "PART_TabScrollViewer";
        private const string ScrollBackButtonPartName = "PART_ScrollBackButton";
        private const string ScrollForwardButtonPartName = "PART_ScrollForwardButton";
        private static readonly List<WeakReference> ActiveStrips = [];

        public static readonly DependencyProperty TabMinWidthProperty =
            TabWindow.TabMinWidthProperty.AddOwner(typeof(WindowTabStrip));

        public static readonly DependencyProperty TabMaxWidthProperty =
            TabWindow.TabMaxWidthProperty.AddOwner(typeof(WindowTabStrip));

        private static readonly DependencyProperty AnimatedScrollOffsetProperty =
            DependencyProperty.Register(
                nameof(AnimatedScrollOffset),
                typeof(double),
                typeof(WindowTabStrip),
                new PropertyMetadata(0.0, OnAnimatedScrollOffsetChanged));

        private object _mouseDownItem;
        private object _mouseDownSelection;
        private WindowTabItem _mouseDownContainer;
        private Point _mouseDownPoint;
        private double _mouseDownGrabRatio;
        private double _mouseDownGrabY;
        private TabDragSession _dragSession;
        private bool _isEndingDrag;
        private ScrollViewer _scrollViewer;
        private ButtonBase _scrollBackButton;
        private ButtonBase _scrollForwardButton;

        static WindowTabStrip()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(WindowTabStrip),
                new FrameworkPropertyMetadata(typeof(WindowTabStrip)));
        }

        public WindowTabStrip()
        {
            SelectionMode = SelectionMode.Single;
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
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

        private double AnimatedScrollOffset
        {
            get => (double)GetValue(AnimatedScrollOffsetProperty);
            set => SetValue(AnimatedScrollOffsetProperty, value);
        }

        public override void OnApplyTemplate()
        {
            DetachTemplateHandlers();
            base.OnApplyTemplate();

            _scrollViewer = GetTemplateChild(ScrollViewerPartName) as ScrollViewer;
            _scrollBackButton = GetTemplateChild(ScrollBackButtonPartName) as ButtonBase;
            _scrollForwardButton = GetTemplateChild(ScrollForwardButtonPartName) as ButtonBase;

            if (_scrollViewer != null)
                _scrollViewer.ScrollChanged += OnScrollChanged;
            if (_scrollBackButton != null)
                _scrollBackButton.Click += OnScrollBackClick;
            if (_scrollForwardButton != null)
                _scrollForwardButton.Click += OnScrollForwardClick;

            Dispatcher.BeginInvoke(
                DispatcherPriority.ContextIdle,
                new Action(UpdateScrollButtons));
        }

        protected override bool IsItemItsOwnContainerOverride(object item) =>
            item is WindowTabItem;

        protected override DependencyObject GetContainerForItemOverride() =>
            new WindowTabItem();

        protected override void PrepareContainerForItemOverride(
            DependencyObject element,
            object item)
        {
            base.PrepareContainerForItemOverride(element, item);

            if (element is WindowTabItem tabItem)
                tabItem.SetIsHomeTab(ReferenceEquals(item, GetOwner()?.HomeTab));
        }

        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);
            Dispatcher.BeginInvoke(
                DispatcherPriority.ContextIdle,
                new Action(RefreshOverflowState));
            Dispatcher.BeginInvoke(
                DispatcherPriority.ContextIdle,
                new Action(RefreshHomeTabState));
        }

        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);
            Dispatcher.BeginInvoke(
                DispatcherPriority.Loaded,
                new Action(BringSelectedTabIntoView));
        }

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            var source = e.OriginalSource as DependencyObject;
            var container = source as WindowTabItem
                ?? source?.FindVisualAncestor<WindowTabItem>();
            bool isValidTab = source != null
                && !IsInteractiveElement(source)
                && container != null
                && ReferenceEquals(ItemsControlFromItemContainer(container), this);
            object mouseDownItem = isValidTab
                ? ItemContainerGenerator.ItemFromContainer(container)
                : null;
            object selectionBeforeMouseDown = isValidTab
                ? GetOwner()?.GetSelectionBeforeTabDrag(mouseDownItem)
                : null;

            base.OnPreviewMouseLeftButtonDown(e);

            if (!isValidTab)
            {
                ResetMouseDown();
                return;
            }

            _mouseDownItem = mouseDownItem;
            _mouseDownSelection = selectionBeforeMouseDown;
            _mouseDownContainer = container;
            _mouseDownPoint = e.GetPosition(this);
            Point pointInTab = e.GetPosition(container);
            _mouseDownGrabRatio = container.ActualWidth > 0
                ? Math.Max(0, Math.Min(1, pointInTab.X / container.ActualWidth))
                : 0.5;
            _mouseDownGrabY = pointInTab.Y;
            if (!container.IsSelected)
            {
                DependencyObjectCompat.SetControlValue(
                    container,
                    Selector.IsSelectedProperty,
                    true);
            }
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);

            if (e.Handled || e.ChangedButton != MouseButton.Middle)
                return;

            var source = e.OriginalSource as DependencyObject;
            var container = source as WindowTabItem
                ?? source?.FindVisualAncestor<WindowTabItem>();
            if (container == null || !ReferenceEquals(ItemsControlFromItemContainer(container), this))
                return;

            object tab = ItemContainerGenerator.ItemFromContainer(container);
            var owner = GetOwner();
            if (owner == null || !owner.CanCloseTab(tab))
                return;

            owner.CloseTab(tab);
            e.Handled = true;
        }

        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
            if (_dragSession != null)
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    _dragSession.Update(PointToScreen(e.GetPosition(this)));
                    e.Handled = true;
                }
                else
                {
                    EndActiveDrag(false);
                }
                return;
            }

            if (_mouseDownItem != null
                && e.LeftButton == MouseButtonState.Pressed)
            {
                Point moveOnlyPoint = e.GetPosition(this);
                var moveOnlyOwner = GetOwner();
                if (moveOnlyOwner != null
                    && moveOnlyOwner.IsMoveOnlyTabLayout
                    && HasPassedDragThreshold(moveOnlyPoint))
                {
                    ResetMouseDown();
                    e.Handled = true;
                    moveOnlyOwner.BeginWindowMove();
                    return;
                }
            }

            base.OnPreviewMouseMove(e);

            if (_mouseDownItem == null || e.LeftButton != MouseButtonState.Pressed)
                return;

            Point current = e.GetPosition(this);
            if (!HasPassedDragThreshold(current))
                return;

            var owner = GetOwner();
            if (owner == null
                || !owner.CanMoveTab(_mouseDownItem)
                || (!owner.CanReorderTabs && !owner.CanTearOffTabs))
            {
                ResetMouseDown();
                return;
            }

            _dragSession = new TabDragSession(
                this,
                owner,
                _mouseDownItem,
                _mouseDownContainer,
                _mouseDownSelection,
                _mouseDownGrabRatio,
                _mouseDownGrabY);

            if (!Mouse.Capture(this, CaptureMode.SubTree))
            {
                _dragSession.Cancel();
                _dragSession = null;
                ResetMouseDown();
                return;
            }

            _dragSession.Update(PointToScreen(current));
            e.Handled = true;
        }

        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (_dragSession != null)
            {
                _dragSession.Update(PointToScreen(e.GetPosition(this)));
                EndActiveDrag(true);
                e.Handled = true;
                return;
            }

            ResetMouseDown();
            base.OnPreviewMouseLeftButtonUp(e);
        }

        protected override void OnLostMouseCapture(MouseEventArgs e)
        {
            base.OnLostMouseCapture(e);
            if (!_isEndingDrag && _dragSession != null)
                EndActiveDrag(false);
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            var panel = this.FindVisualChild<WindowTabPanel>();
            panel?.InvalidateMeasure();
            panel?.InvalidateArrange();
            UpdateScrollButtons();
        }

        internal TabWindow GetOwner() => Window.GetWindow(this) as TabWindow;

        internal void RefreshHomeTabState()
        {
            TabWindow owner = GetOwner();
            for (int index = 0; index < Items.Count; index++)
            {
                var item = this.GetVisualItem<WindowTabItem>(index);
                item?.SetIsHomeTab(ReferenceEquals(Items[index], owner?.HomeTab));
            }

            var panel = this.FindVisualChild<WindowTabPanel>();
            panel?.InvalidateMeasure();
            panel?.InvalidateArrange();
        }

        internal double GetTabViewportWidth()
        {
            if (_scrollViewer?.ViewportWidth > 0)
                return _scrollViewer.ViewportWidth;
            if (_scrollViewer?.ActualWidth > 0)
                return _scrollViewer.ActualWidth;
            return ActualWidth;
        }

        internal void BeginPreview(
            object tab,
            Point screenPoint,
            double grabRatio,
            int? initialPreviewIndex = null)
        {
            UpdateLayout();
            var owner = GetOwner();
            var item = this.GetVisualItem<WindowTabItem>(tab);
            var panel = this.FindVisualChild<WindowTabPanel>();
            if (owner == null || item == null || panel == null)
                return;

            int index = TabCollectionTransfer.IndexOfReference(owner.Tabs, tab);
            if (index < 0)
                return;

            if (!item.IsSelected)
            {
                DependencyObjectCompat.SetControlValue(
                    item,
                    Selector.IsSelectedProperty,
                    true);
            }
            panel.BeginDrag(
                item,
                index,
                panel.PointFromScreen(screenPoint).X,
                grabRatio,
                initialPreviewIndex);
        }

        internal void UpdatePreview(
            Point screenPoint,
            double grabRatio,
            int? forcedPreviewIndex = null)
        {
            var panel = this.FindVisualChild<WindowTabPanel>();
            if (panel == null)
                return;

            ScrollForDrag(screenPoint);
            panel.UpdateDrag(
                panel.PointFromScreen(screenPoint).X,
                grabRatio,
                forcedPreviewIndex);
        }

        internal int GetPreviewIndex()
        {
            var panel = this.FindVisualChild<WindowTabPanel>();
            return panel?.PreviewIndex ?? -1;
        }

        internal void EndPreview()
        {
            this.FindVisualChild<WindowTabPanel>()?.EndDrag();
        }

        internal int GetExternalInsertionIndex(Point screenPoint)
        {
            var owner = GetOwner();
            double pointerX = PointFromScreen(screenPoint).X;
            for (int index = 0; index < Items.Count; index++)
            {
                var container = this.GetVisualItem<WindowTabItem>(index);
                if (container == null)
                    continue;

                Point origin = container.TranslatePoint(new Point(0, 0), this);
                if (pointerX < origin.X + container.ActualWidth / 2.0)
                    return owner?.NormalizeInsertionIndex(index) ?? index;
            }

            return owner?.NormalizeInsertionIndex(Items.Count) ?? Items.Count;
        }

        internal bool ContainsScreenPoint(Point screenPoint, double verticalExpansion)
        {
            if (!IsVisible || ActualWidth <= 0 || ActualHeight <= 0)
                return false;

            Point topLeft = PointToScreen(new Point(0, 0));
            Point bottomRight = PointToScreen(new Point(ActualWidth, ActualHeight));
            double scaleY = ActualHeight > 0
                ? Math.Abs(bottomRight.Y - topLeft.Y) / ActualHeight
                : 1;

            return screenPoint.X >= Math.Min(topLeft.X, bottomRight.X)
                && screenPoint.X <= Math.Max(topLeft.X, bottomRight.X)
                && screenPoint.Y >= Math.Min(topLeft.Y, bottomRight.Y) - verticalExpansion * scaleY
                && screenPoint.Y <= Math.Max(topLeft.Y, bottomRight.Y) + verticalExpansion * scaleY;
        }

        internal bool IsWithinVerticalBand(Point screenPoint, double verticalExpansion)
        {
            if (!IsVisible || ActualHeight <= 0)
                return false;

            Point topLeft = PointToScreen(new Point(0, 0));
            Point bottomRight = PointToScreen(new Point(ActualWidth, ActualHeight));
            double scaleY = Math.Abs(bottomRight.Y - topLeft.Y) / ActualHeight;
            return screenPoint.Y >= Math.Min(topLeft.Y, bottomRight.Y) - verticalExpansion * scaleY
                && screenPoint.Y <= Math.Max(topLeft.Y, bottomRight.Y) + verticalExpansion * scaleY;
        }

        internal static WindowTabStrip FindTarget(
            Point screenPoint,
            object tab,
            TabWindow excludedWindow)
        {
            PruneRegistry();
            for (int index = ActiveStrips.Count - 1; index >= 0; index--)
            {
                var strip = ActiveStrips[index].Target as WindowTabStrip;
                var owner = strip?.GetOwner();
                if (strip == null
                    || owner == null
                    || ReferenceEquals(owner, excludedWindow)
                    || !owner.CanAcceptTab(tab)
                    || !strip.ContainsScreenPoint(screenPoint, TargetMagnetism))
                {
                    continue;
                }

                return strip;
            }

            return null;
        }

        internal void CancelActiveDrag() => EndActiveDrag(false);

        private void EndActiveDrag(bool complete)
        {
            if (_dragSession == null)
                return;

            _isEndingDrag = true;
            try
            {
                if (complete)
                    _dragSession.Complete();
                else
                    _dragSession.Cancel();

                _dragSession = null;
                if (IsMouseCaptured)
                    ReleaseMouseCapture();
            }
            finally
            {
                _isEndingDrag = false;
                ResetMouseDown();
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            PruneRegistry();
            if (ActiveStrips.Any(reference => ReferenceEquals(reference.Target, this)))
                return;

            ActiveStrips.Add(new WeakReference(this));
            RefreshHomeTabState();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (_dragSession != null)
                EndActiveDrag(false);

            for (int index = ActiveStrips.Count - 1; index >= 0; index--)
            {
                if (!ActiveStrips[index].IsAlive
                    || ReferenceEquals(ActiveStrips[index].Target, this))
                {
                    ActiveStrips.RemoveAt(index);
                }
            }
        }

        private static void OnAnimatedScrollOffsetChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            var strip = (WindowTabStrip)d;
            strip._scrollViewer?.ScrollToHorizontalOffset((double)e.NewValue);
        }

        private void OnScrollChanged(object sender, ScrollChangedEventArgs e) =>
            UpdateScrollButtons();

        private void OnScrollBackClick(object sender, RoutedEventArgs e) =>
            ScrollBy(-GetScrollStep());

        private void OnScrollForwardClick(object sender, RoutedEventArgs e) =>
            ScrollBy(GetScrollStep());

        private void ScrollBy(double delta)
        {
            if (_scrollViewer == null)
                return;

            double target = Math.Max(
                0,
                Math.Min(
                    _scrollViewer.ScrollableWidth,
                    _scrollViewer.HorizontalOffset + delta));
            AnimateScrollTo(target);
        }

        private double GetScrollStep() =>
            Math.Max(TabMinWidth, _scrollViewer?.ViewportWidth / 3.0 ?? TabMinWidth);

        private void AnimateScrollTo(double target)
        {
            if (_scrollViewer == null)
                return;

            target = Math.Max(0, Math.Min(_scrollViewer.ScrollableWidth, target));
            double current = _scrollViewer.HorizontalOffset;
            if (Math.Abs(target - current) <= ScrollEpsilon)
                return;

            AnimatedScrollOffset = current;
            BeginAnimation(AnimatedScrollOffsetProperty, null);
            var animation = new DoubleAnimation
            {
                From = current,
                To = target,
                Duration = new Duration(TimeSpan.FromMilliseconds(140)),
                DecelerationRatio = 0.85,
                FillBehavior = FillBehavior.Stop
            };
            BeginAnimation(
                AnimatedScrollOffsetProperty,
                animation,
                HandoffBehavior.SnapshotAndReplace);
            AnimatedScrollOffset = target;
        }

        private void ScrollForDrag(Point screenPoint)
        {
            if (_scrollViewer == null || _scrollViewer.ScrollableWidth <= ScrollEpsilon)
                return;

            Point point = _scrollViewer.PointFromScreen(screenPoint);
            if (point.X < ScrollEdgeSize)
            {
                _scrollViewer.ScrollToHorizontalOffset(
                    Math.Max(0, _scrollViewer.HorizontalOffset - ScrollEdgeSize / 2));
            }
            else if (point.X > _scrollViewer.ViewportWidth - ScrollEdgeSize)
            {
                _scrollViewer.ScrollToHorizontalOffset(
                    Math.Min(
                        _scrollViewer.ScrollableWidth,
                        _scrollViewer.HorizontalOffset + ScrollEdgeSize / 2));
            }
        }

        private void BringSelectedTabIntoView()
        {
            if (SelectedItem == null)
                return;

            var item = this.GetVisualItem<WindowTabItem>(SelectedItem);
            item?.BringIntoView();
        }

        private void RefreshOverflowState()
        {
            var panel = this.FindVisualChild<WindowTabPanel>();
            panel?.InvalidateMeasure();
            panel?.InvalidateArrange();
            UpdateLayout();
            UpdateScrollButtons();
        }

        private void UpdateScrollButtons()
        {
            if (_scrollViewer == null
                || _scrollBackButton == null
                || _scrollForwardButton == null)
            {
                return;
            }

            bool hasOverflow = _scrollViewer.ExtentWidth
                > _scrollViewer.ViewportWidth + ScrollEpsilon;
            Visibility visibility = hasOverflow
                ? Visibility.Visible
                : Visibility.Collapsed;

            bool visibilityChanged = _scrollBackButton.Visibility != visibility
                || _scrollForwardButton.Visibility != visibility;

            _scrollBackButton.Visibility = visibility;
            _scrollForwardButton.Visibility = visibility;
            _scrollBackButton.IsEnabled = hasOverflow
                && _scrollViewer.HorizontalOffset > ScrollEpsilon;
            _scrollForwardButton.IsEnabled = hasOverflow
                && _scrollViewer.HorizontalOffset
                    < _scrollViewer.ScrollableWidth - ScrollEpsilon;

            if (visibilityChanged)
            {
                var panel = this.FindVisualChild<WindowTabPanel>();
                panel?.InvalidateMeasure();
                panel?.InvalidateArrange();
            }
        }

        private void DetachTemplateHandlers()
        {
            if (_scrollViewer != null)
                _scrollViewer.ScrollChanged -= OnScrollChanged;
            if (_scrollBackButton != null)
                _scrollBackButton.Click -= OnScrollBackClick;
            if (_scrollForwardButton != null)
                _scrollForwardButton.Click -= OnScrollForwardClick;

            _scrollViewer = null;
            _scrollBackButton = null;
            _scrollForwardButton = null;
        }

        private static void PruneRegistry()
        {
            for (int index = ActiveStrips.Count - 1; index >= 0; index--)
            {
                if (!(ActiveStrips[index].Target is WindowTabStrip))
                    ActiveStrips.RemoveAt(index);
            }
        }

        private static bool IsInteractiveElement(DependencyObject source)
        {
            if (source is ButtonBase)
                return true;

            return source.FindVisualAncestor<ButtonBase>() != null;
        }

        private bool HasPassedDragThreshold(Point current) =>
            Math.Abs(current.X - _mouseDownPoint.X) >= SystemParameters.MinimumHorizontalDragDistance
            || Math.Abs(current.Y - _mouseDownPoint.Y) >= SystemParameters.MinimumVerticalDragDistance;

        private void ResetMouseDown()
        {
            _mouseDownItem = null;
            _mouseDownSelection = null;
            _mouseDownContainer = null;
            _mouseDownPoint = default;
            _mouseDownGrabRatio = 0;
            _mouseDownGrabY = 0;
        }
    }
}
