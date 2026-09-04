using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace WinCraft.UI
{
    public sealed class WindowTabPanel : Panel
    {
        private const double CompactContentThreshold = 96;
        private const double HomeTabWidth = 40;

        private readonly Dictionary<UIElement, Rect> _previousSlots = [];
        private bool _hasArranged;
        private WindowTabItem _draggedItem;
        private int _draggedIndex = -1;
        private int _previewIndex = -1;
        private double _draggedLeft;

        internal int PreviewIndex => _previewIndex;

        internal void BeginDrag(
            WindowTabItem item,
            int itemIndex,
            double pointerX,
            double grabRatio,
            int? initialPreviewIndex = null)
        {
            _draggedItem = item;
            _draggedIndex = itemIndex;
            _previewIndex = Math.Max(
                0,
                Math.Min(initialPreviewIndex ?? itemIndex, InternalChildren.Count - 1));
            Panel.SetZIndex(item, 10);
            item.SetIsDragging(true);
            UpdateDrag(pointerX, grabRatio, _previewIndex);
        }

        internal void UpdateDrag(
            double pointerX,
            double grabRatio,
            int? forcedPreviewIndex = null)
        {
            if (_draggedItem == null || InternalChildren.Count == 0)
                return;

            GetTabWidths(
                RenderSize.Width,
                InternalChildren.Count,
                out double activeWidth,
                out double inactiveWidth);
            int activeIndex = GetActiveIndex();
            double draggedWidth = GetChildWidth(
                _draggedItem,
                _draggedIndex,
                activeIndex,
                activeWidth,
                inactiveWidth);
            double minLeft = GetMinimumDragLeft(activeIndex, activeWidth, inactiveWidth);
            _draggedLeft = Math.Max(
                minLeft,
                Math.Min(
                    pointerX - draggedWidth * grabRatio,
                    Math.Max(minLeft, RenderSize.Width - draggedWidth)));

            int previewIndex;
            if (forcedPreviewIndex.HasValue)
            {
                previewIndex = Math.Max(
                    GetMinimumPreviewIndex(),
                    Math.Min(forcedPreviewIndex.Value, InternalChildren.Count - 1));
            }
            else
            {
                previewIndex = Math.Max(
                    GetMinimumPreviewIndex(),
                    GetPreviewIndex(_draggedLeft, activeIndex, activeWidth, inactiveWidth));
            }

            if (_previewIndex != previewIndex)
                _previewIndex = previewIndex;

            InvalidateArrange();
        }

        internal void EndDrag()
        {
            if (_draggedItem != null)
            {
                _draggedItem.SetIsDragging(false);
                _draggedItem.ClearValue(Panel.ZIndexProperty);
            }

            _draggedItem = null;
            _draggedIndex = -1;
            _previewIndex = -1;
            InvalidateArrange();
        }

        internal static int GetVisualIndex(int itemIndex, int draggedIndex, int previewIndex)
        {
            if (itemIndex == draggedIndex)
                return previewIndex;

            if (previewIndex > draggedIndex
                && itemIndex > draggedIndex
                && itemIndex <= previewIndex)
            {
                return itemIndex - 1;
            }

            if (previewIndex < draggedIndex
                && itemIndex >= previewIndex
                && itemIndex < draggedIndex)
            {
                return itemIndex + 1;
            }

            return itemIndex;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            double availableWidth = GetAvailableWidth(availableSize.Width);
            int activeIndex = GetActiveIndex();
            GetTabWidths(
                availableWidth,
                InternalChildren.Count,
                out double activeWidth,
                out double inactiveWidth);
            double maxHeight = 0;
            double totalWidth = 0;

            for (int index = 0; index < InternalChildren.Count; index++)
            {
                UIElement child = InternalChildren[index];
                if (child == null)
                    continue;

                double childWidth = GetChildWidth(
                    child,
                    index,
                    activeIndex,
                    activeWidth,
                    inactiveWidth);
                SetElementWidth(child, childWidth);
                child.Measure(new Size(childWidth, availableSize.Height));
                maxHeight = Math.Max(maxHeight, child.DesiredSize.Height);
                totalWidth += childWidth;
            }

            return new Size(totalWidth, maxHeight);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            double availableWidth = GetAvailableWidth(finalSize.Width);
            int count = InternalChildren.Count;
            if (count == 0)
            {
                _previousSlots.Clear();
                return new Size(availableWidth, finalSize.Height);
            }

            GetTabWidths(
                availableWidth,
                count,
                out double activeWidth,
                out double inactiveWidth);
            int activeIndex = GetActiveIndex();
            var newSlots = new Dictionary<UIElement, Rect>();
            for (int itemIndex = 0; itemIndex < InternalChildren.Count; itemIndex++)
            {
                UIElement child = InternalChildren[itemIndex];
                if (child == null)
                    continue;

                int visualIndex = _draggedItem == null
                    ? itemIndex
                    : GetVisualIndex(itemIndex, _draggedIndex, _previewIndex);
                double itemWidth = GetChildWidth(
                    child,
                    itemIndex,
                    activeIndex,
                    activeWidth,
                    inactiveWidth);
                double x = ReferenceEquals(child, _draggedItem)
                    ? _draggedLeft
                    : GetSlotOffset(
                        visualIndex,
                        activeIndex,
                        activeWidth,
                        inactiveWidth);
                var slot = new Rect(x, 0, itemWidth, finalSize.Height);
                SetElementWidth(child, itemWidth);
                child.Arrange(slot);
                if (child is WindowTabItem tabItem)
                {
                    tabItem.SetIsCompact(
                        itemWidth + 0.5 < CompactContentThreshold);
                }

                newSlots[child] = slot;

                if (!ReferenceEquals(child, _draggedItem)
                    && _hasArranged
                    && _previousSlots.TryGetValue(child, out Rect previous)
                    && Math.Abs(previous.X - slot.X) > 0.5)
                {
                    AnimateToSlot(child, previous, slot);
                }
            }

            _previousSlots.Clear();
            foreach (var pair in newSlots)
                _previousSlots[pair.Key] = pair.Value;

            _hasArranged = true;
            return new Size(availableWidth, finalSize.Height);
        }

        private void GetTabWidths(
            double availableWidth,
            int count,
            out double activeWidth,
            out double inactiveWidth)
        {
            var owner = this.FindVisualAncestor<WindowTabStrip>();
            int homeTabCount = 0;
            for (int index = 0; index < InternalChildren.Count; index++)
            {
                if (InternalChildren[index] is WindowTabItem item && item.IsHomeTab)
                    homeTabCount++;
            }

            int normalTabCount = Math.Max(0, count - homeTabCount);
            if (normalTabCount == 0)
            {
                activeWidth = 0;
                inactiveWidth = 0;
                return;
            }

            double minWidth = owner?.TabMinWidth ?? 96;
            double maxWidth = Math.Max(minWidth, owner?.TabMaxWidth ?? 240);
            double equalWidth = double.IsInfinity(availableWidth)
                ? maxWidth
                : Math.Max(
                    minWidth,
                    Math.Min(
                        maxWidth,
                        Math.Max(0, availableWidth - homeTabCount * HomeTabWidth)
                            / normalTabCount));

            activeWidth = equalWidth;
            inactiveWidth = equalWidth;
        }

        private int GetActiveIndex()
        {
            for (int index = 0; index < InternalChildren.Count; index++)
            {
                if (InternalChildren[index] is WindowTabItem item
                    && item.IsSelected)
                {
                    return index;
                }
            }

            return 0;
        }

        private double GetAvailableWidth(double proposedWidth)
        {
            if (!double.IsInfinity(proposedWidth))
                return Math.Max(0, proposedWidth);

            var owner = this.FindVisualAncestor<WindowTabStrip>();
            double viewportWidth = owner?.GetTabViewportWidth() ?? 0;
            return viewportWidth > 0
                ? viewportWidth
                : proposedWidth;
        }

        private double GetSlotOffset(
            int visualIndex,
            int activeIndex,
            double activeWidth,
            double inactiveWidth)
        {
            double offset = 0;
            for (int index = 0; index < InternalChildren.Count; index++)
            {
                int childVisualIndex = _draggedItem == null
                    ? index
                    : GetVisualIndex(index, _draggedIndex, _previewIndex);
                if (childVisualIndex < visualIndex)
                {
                    offset += GetChildWidth(
                        InternalChildren[index],
                        index,
                        activeIndex,
                        activeWidth,
                        inactiveWidth);
                }
            }

            return offset;
        }

        private static double GetChildWidth(
            UIElement child,
            int index,
            int activeIndex,
            double activeWidth,
            double inactiveWidth)
        {
            if (child is WindowTabItem item && item.IsHomeTab)
                return HomeTabWidth;

            return index == activeIndex ? activeWidth : inactiveWidth;
        }

        private int GetMinimumPreviewIndex() =>
            InternalChildren.Count > 0
            && InternalChildren[0] is WindowTabItem item
            && item.IsHomeTab
                ? 1
                : 0;

        private double GetMinimumDragLeft(
            int activeIndex,
            double activeWidth,
            double inactiveWidth)
        {
            double offset = 0;
            for (int index = 0; index < GetMinimumPreviewIndex(); index++)
            {
                offset += GetChildWidth(
                    InternalChildren[index],
                    index,
                    activeIndex,
                    activeWidth,
                    inactiveWidth);
            }

            return offset;
        }

        private int GetPreviewIndex(
            double left,
            int activeIndex,
            double activeWidth,
            double inactiveWidth)
        {
            double offset = 0;
            for (int index = 0; index < InternalChildren.Count; index++)
            {
                double width = GetChildWidth(
                    InternalChildren[index],
                    index,
                    activeIndex,
                    activeWidth,
                    inactiveWidth);
                if (left < offset + width / 2.0)
                    return index;

                offset += width;
            }

            return InternalChildren.Count - 1;
        }

        private static void SetElementWidth(UIElement child, double slotWidth)
        {
            if (!(child is FrameworkElement element))
                return;

            double width = Math.Max(
                0,
                slotWidth - element.Margin.Left - element.Margin.Right);
            if (double.IsNaN(element.Width)
                || Math.Abs(element.Width - width) > 0.01)
            {
                element.Width = width;
            }
        }

        private static void AnimateToSlot(UIElement child, Rect previous, Rect current)
        {
            var transform = child.RenderTransform as TranslateTransform;
            if (transform == null)
            {
                transform = new TranslateTransform();
                child.RenderTransform = transform;
            }

            double visualX = previous.X + transform.X;
            transform.BeginAnimation(TranslateTransform.XProperty, null);
            double offset = visualX - current.X;
            transform.X = 0;
            transform.BeginAnimation(
                TranslateTransform.XProperty,
                new DoubleAnimation
                {
                    From = offset,
                    To = 0,
                    Duration = new Duration(TimeSpan.FromMilliseconds(140)),
                    DecelerationRatio = 0.85,
                    FillBehavior = FillBehavior.Stop
                },
                HandoffBehavior.SnapshotAndReplace);
        }
    }
}
