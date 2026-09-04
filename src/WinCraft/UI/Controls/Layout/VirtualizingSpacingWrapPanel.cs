using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace WinCraft.UI
{
    public sealed class VirtualizingSpacingWrapPanel : VirtualizingPanel, IScrollInfo
    {
        private const double LineScrollAmount = 16.0;
        private const double WheelScrollAmount = 48.0;

        public static readonly DependencyProperty OrientationProperty =
            WrapPanel.OrientationProperty.AddOwner(
                typeof(VirtualizingSpacingWrapPanel),
                new FrameworkPropertyMetadata(
                    Orientation.Horizontal,
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.AffectsArrange,
                    OnLayoutPropertyChanged));

        public static readonly DependencyProperty ItemWidthProperty =
            DependencyProperty.Register(
                nameof(ItemWidth),
                typeof(double),
                typeof(VirtualizingSpacingWrapPanel),
                new FrameworkPropertyMetadata(
                    100.0,
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.AffectsArrange,
                    OnLayoutPropertyChanged),
                ValidateItemLength);

        public static readonly DependencyProperty ItemHeightProperty =
            DependencyProperty.Register(
                nameof(ItemHeight),
                typeof(double),
                typeof(VirtualizingSpacingWrapPanel),
                new FrameworkPropertyMetadata(
                    100.0,
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.AffectsArrange,
                    OnLayoutPropertyChanged),
                ValidateItemLength);

        public static readonly DependencyProperty SpacingProperty =
            SpacingPanel.SpacingProperty.AddOwner(
                typeof(VirtualizingSpacingWrapPanel),
                new FrameworkPropertyMetadata(
                    0.0,
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.AffectsArrange,
                    OnLayoutPropertyChanged));

        public static readonly DependencyProperty BorderBrushProperty =
            Border.BorderBrushProperty.AddOwner(typeof(VirtualizingSpacingWrapPanel));

        public static readonly DependencyProperty BorderThicknessProperty =
            Border.BorderThicknessProperty.AddOwner(
                typeof(VirtualizingSpacingWrapPanel),
                new FrameworkPropertyMetadata(new Thickness(), FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty CornerRadiusProperty =
            Border.CornerRadiusProperty.AddOwner(typeof(VirtualizingSpacingWrapPanel));

        private Point _offset;
        private Size _extent;
        private Size _viewport;

        public VirtualizingSpacingWrapPanel()
        {
            UpdateScrollCapabilities();
        }

        public Orientation Orientation
        {
            get => (Orientation)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        public double ItemWidth
        {
            get => (double)GetValue(ItemWidthProperty);
            set => SetValue(ItemWidthProperty, value);
        }

        public double ItemHeight
        {
            get => (double)GetValue(ItemHeightProperty);
            set => SetValue(ItemHeightProperty, value);
        }

        public double Spacing
        {
            get => (double)GetValue(SpacingProperty);
            set => SetValue(SpacingProperty, value);
        }

        public Brush BorderBrush
        {
            get => (Brush)GetValue(BorderBrushProperty);
            set => SetValue(BorderBrushProperty, value);
        }

        public Thickness BorderThickness
        {
            get => (Thickness)GetValue(BorderThicknessProperty);
            set => SetValue(BorderThicknessProperty, value);
        }

        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        public bool CanHorizontallyScroll { get; set; }

        public bool CanVerticallyScroll { get; set; }

        public double ExtentHeight => _extent.Height;

        public double ExtentWidth => _extent.Width;

        public double HorizontalOffset => _offset.X;

        public ScrollViewer ScrollOwner { get; set; }

        public double VerticalOffset => _offset.Y;

        public double ViewportHeight => _viewport.Height;

        public double ViewportWidth => _viewport.Width;

        public void LineDown() => SetVerticalOffset(VerticalOffset + LineScrollAmount);

        public void LineLeft() => SetHorizontalOffset(HorizontalOffset - LineScrollAmount);

        public void LineRight() => SetHorizontalOffset(HorizontalOffset + LineScrollAmount);

        public void LineUp() => SetVerticalOffset(VerticalOffset - LineScrollAmount);

        public Rect MakeVisible(Visual visual, Rect rectangle)
        {
            var itemIndex = GetItemIndex(visual);
            if (itemIndex < 0)
                return Rect.Empty;

            var itemsPerLine = GetItemsPerLine(_viewport);
            var itemLength = GetSecondaryItemLength();
            var lineIndex = itemIndex / itemsPerLine;
            var itemStart = lineIndex * (itemLength + Spacing);
            var itemEnd = itemStart + itemLength;
            var offset = GetSecondaryOffset();
            var viewport = GetSecondaryViewportLength();
            if (itemStart < offset)
                SetSecondaryOffset(itemStart);
            else if (itemEnd > offset + viewport)
                SetSecondaryOffset(itemEnd - viewport);

            return Orientation == Orientation.Horizontal
                ? new Rect(0, itemStart - GetSecondaryOffset(), ViewportWidth, itemLength)
                : new Rect(itemStart - GetSecondaryOffset(), 0, itemLength, ViewportHeight);
        }

        public void MouseWheelDown() => SetVerticalOffset(VerticalOffset + WheelScrollAmount);

        public void MouseWheelLeft() => SetHorizontalOffset(HorizontalOffset - WheelScrollAmount);

        public void MouseWheelRight() => SetHorizontalOffset(HorizontalOffset + WheelScrollAmount);

        public void MouseWheelUp() => SetVerticalOffset(VerticalOffset - WheelScrollAmount);

        public void PageDown() => SetVerticalOffset(VerticalOffset + ViewportHeight);

        public void PageLeft() => SetHorizontalOffset(HorizontalOffset - ViewportWidth);

        public void PageRight() => SetHorizontalOffset(HorizontalOffset + ViewportWidth);

        public void PageUp() => SetVerticalOffset(VerticalOffset - ViewportHeight);

        public void SetHorizontalOffset(double offset)
        {
            if (Orientation == Orientation.Vertical)
                SetSecondaryOffset(offset);
        }

        public void SetVerticalOffset(double offset)
        {
            if (Orientation == Orientation.Horizontal)
                SetSecondaryOffset(offset);
        }

        protected override void BringIndexIntoView(int index)
        {
            if (index < 0)
                return;

            var itemsPerLine = GetItemsPerLine(_viewport);
            SetSecondaryOffset(index / itemsPerLine * (GetSecondaryItemLength() + Spacing));
        }

        protected override void OnRender(DrawingContext dc)
        {
            SpacingPanel.RenderPanelBackground(
                dc, ActualWidth, ActualHeight, Background, CornerRadius, BorderBrush, BorderThickness);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (UpdateViewport(finalSize))
                InvalidateMeasure();

            var itemsPerLine = GetItemsPerLine(finalSize);
            var primaryItemLength = GetPrimaryItemLength();
            var secondaryItemLength = GetSecondaryItemLength();
            for (var childIndex = 0; childIndex < InternalChildren.Count; childIndex++)
            {
                var itemIndex = ItemContainerGenerator.IndexFromGeneratorPosition(
                    new GeneratorPosition(childIndex, 0));
                if (itemIndex < 0)
                {
                    InternalChildren[childIndex].Arrange(new Rect());
                    continue;
                }

                var lineIndex = itemIndex / itemsPerLine;
                var itemIndexInLine = itemIndex % itemsPerLine;
                var primaryOffset = itemIndexInLine * (primaryItemLength + Spacing);
                var secondaryOffset = lineIndex * (secondaryItemLength + Spacing) - GetSecondaryOffset();
                var childRect = Orientation == Orientation.Horizontal
                    ? new Rect(primaryOffset, secondaryOffset, ItemWidth, ItemHeight)
                    : new Rect(secondaryOffset, primaryOffset, ItemWidth, ItemHeight);
                InternalChildren[childIndex].Arrange(childRect);
            }

            return finalSize;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var viewport = new Size(
                ResolveViewportLength(availableSize.Width, ActualWidth, ScrollOwner?.ViewportWidth, ItemWidth),
                ResolveViewportLength(availableSize.Height, ActualHeight, ScrollOwner?.ViewportHeight, ItemHeight));
            UpdateViewport(viewport);

            var itemCount = ItemsControl.GetItemsOwner(this)?.Items.Count ?? 0;
            var itemsPerLine = GetItemsPerLine(viewport);
            var lineCount = (itemCount + itemsPerLine - 1) / itemsPerLine;
            var extentLength = lineCount == 0
                ? 0
                : lineCount * GetSecondaryItemLength() + (lineCount - 1) * Spacing;
            _extent = Orientation == Orientation.Horizontal
                ? new Size(viewport.Width, extentLength)
                : new Size(extentLength, viewport.Height);
            SetSecondaryOffset(GetSecondaryOffset());

            var lineStep = GetSecondaryItemLength() + Spacing;
            var firstLine = Math.Max(0, (int)Math.Floor(GetSecondaryOffset() / lineStep));
            var visibleLineCount = Math.Max(
                1,
                (int)Math.Ceiling(GetSecondaryViewportLength() / lineStep) + 1);
            var firstItemIndex = firstLine * itemsPerLine;
            var lastItemIndex = Math.Min(itemCount, (firstLine + visibleLineCount) * itemsPerLine);

            CleanUpItems(firstItemIndex, lastItemIndex);
            RealizeItems(firstItemIndex, lastItemIndex);
            ScrollOwner?.InvalidateScrollInfo();
            return viewport;
        }

        protected override void OnItemsChanged(object sender, ItemsChangedEventArgs args)
        {
            base.OnItemsChanged(sender, args);
            ItemContainerGenerator.RemoveAll();
            if (InternalChildren.Count > 0)
                RemoveInternalChildRange(0, InternalChildren.Count);

            InvalidateMeasure();
        }

        internal void ApplySpacing(Spacing spacing)
        {
            SpacingToken.Apply(this, spacing, SpacingProperty);
        }

        private void CleanUpItems(int firstItemIndex, int lastItemIndex)
        {
            for (var childIndex = InternalChildren.Count - 1; childIndex >= 0; childIndex--)
            {
                var position = new GeneratorPosition(childIndex, 0);
                var itemIndex = ItemContainerGenerator.IndexFromGeneratorPosition(position);
                if (itemIndex >= firstItemIndex && itemIndex < lastItemIndex)
                    continue;

                ItemContainerGenerator.Remove(position, 1);
                RemoveInternalChildRange(childIndex, 1);
            }
        }

        private UIElement FindContainer(Visual visual)
        {
            DependencyObject current = visual;
            while (current != null)
            {
                if (current is UIElement element && InternalChildren.Contains(element))
                    return element;

                current = VisualTreeHelper.GetParent(current);
            }

            return null;
        }

        private double GetPrimaryItemLength() =>
            Orientation == Orientation.Horizontal ? ItemWidth : ItemHeight;

        private int GetItemsPerLine(Size viewport)
        {
            var viewportLength = Orientation == Orientation.Horizontal
                ? viewport.Width
                : viewport.Height;
            var itemStep = GetPrimaryItemLength() + Spacing;
            return Math.Max(1, (int)Math.Floor((viewportLength + Spacing) / itemStep));
        }

        private int GetItemIndex(Visual visual)
        {
            var container = FindContainer(visual);
            if (container == null)
                return -1;

            return ItemContainerGenerator.IndexFromGeneratorPosition(
                new GeneratorPosition(InternalChildren.IndexOf(container), 0));
        }

        private double GetSecondaryExtentLength() =>
            Orientation == Orientation.Horizontal ? ExtentHeight : ExtentWidth;

        private double GetSecondaryItemLength() =>
            Orientation == Orientation.Horizontal ? ItemHeight : ItemWidth;

        private double GetSecondaryOffset() =>
            Orientation == Orientation.Horizontal ? VerticalOffset : HorizontalOffset;

        private double GetSecondaryViewportLength() =>
            Orientation == Orientation.Horizontal ? ViewportHeight : ViewportWidth;

        private void RealizeItems(int firstItemIndex, int lastItemIndex)
        {
            if (firstItemIndex >= lastItemIndex)
                return;

            var startPosition = ItemContainerGenerator.GeneratorPositionFromIndex(firstItemIndex);
            var childIndex = startPosition.Offset == 0 ? startPosition.Index : startPosition.Index + 1;
            childIndex = Math.Max(0, childIndex);

            using (ItemContainerGenerator.StartAt(startPosition, GeneratorDirection.Forward, true))
            {
                for (var itemIndex = firstItemIndex; itemIndex < lastItemIndex; itemIndex++, childIndex++)
                {
                    var child = ItemContainerGenerator.GenerateNext(out var newlyRealized) as UIElement;
                    if (child == null)
                        continue;

                    if (newlyRealized)
                    {
                        if (childIndex >= InternalChildren.Count)
                            AddInternalChild(child);
                        else
                            InsertInternalChild(childIndex, child);

                        ItemContainerGenerator.PrepareItemContainer(child);
                    }

                    child.Measure(new Size(ItemWidth, ItemHeight));
                }
            }
        }

        private void SetSecondaryOffset(double offset)
        {
            if (double.IsNaN(offset) || double.IsInfinity(offset))
                return;

            var maximumOffset = Math.Max(0, GetSecondaryExtentLength() - GetSecondaryViewportLength());
            var clampedOffset = Math.Max(0, Math.Min(offset, maximumOffset));
            if (Math.Abs(GetSecondaryOffset() - clampedOffset) < 0.01)
                return;

            if (Orientation == Orientation.Horizontal)
                _offset.Y = clampedOffset;
            else
                _offset.X = clampedOffset;

            ScrollOwner?.InvalidateScrollInfo();
            InvalidateMeasure();
        }

        private static void OnLayoutPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
        {
            var panel = (VirtualizingSpacingWrapPanel)dependencyObject;
            panel.UpdateScrollCapabilities();
            panel.InvalidateMeasure();
        }

        private static double ResolveViewportLength(double available, double actual, double? ownerViewport, double fallback)
        {
            if (!double.IsInfinity(available) && available > 0)
                return available;
            if (ownerViewport.HasValue && ownerViewport.Value > 0)
                return ownerViewport.Value;
            if (actual > 0)
                return actual;
            return fallback;
        }

        private bool UpdateViewport(Size viewport)
        {
            if (_viewport == viewport)
                return false;

            _viewport = viewport;
            ScrollOwner?.InvalidateScrollInfo();
            return true;
        }

        private void UpdateScrollCapabilities()
        {
            CanHorizontallyScroll = Orientation == Orientation.Vertical;
            CanVerticallyScroll = Orientation == Orientation.Horizontal;
        }

        private static bool ValidateItemLength(object value)
        {
            var length = (double)value;
            return length > 0 && !double.IsNaN(length) && !double.IsInfinity(length);
        }
    }
}
