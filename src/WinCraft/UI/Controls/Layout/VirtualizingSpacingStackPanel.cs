using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace WinCraft.UI
{
    public sealed class VirtualizingSpacingStackPanel : VirtualizingPanel, IScrollInfo
    {
        private const double LineScrollAmount = 16.0;
        private const double WheelScrollAmount = 48.0;

        public static readonly DependencyProperty OrientationProperty =
            StackPanel.OrientationProperty.AddOwner(
                typeof(VirtualizingSpacingStackPanel),
                new FrameworkPropertyMetadata(
                    Orientation.Vertical,
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.AffectsArrange,
                    OnLayoutPropertyChanged));

        public static readonly DependencyProperty ItemWidthProperty =
            DependencyProperty.Register(
                nameof(ItemWidth),
                typeof(double),
                typeof(VirtualizingSpacingStackPanel),
                new FrameworkPropertyMetadata(
                    120.0,
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.AffectsArrange,
                    OnLayoutPropertyChanged),
                ValidateItemLength);

        public static readonly DependencyProperty ItemHeightProperty =
            DependencyProperty.Register(
                nameof(ItemHeight),
                typeof(double),
                typeof(VirtualizingSpacingStackPanel),
                new FrameworkPropertyMetadata(
                    32.0,
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.AffectsArrange,
                    OnLayoutPropertyChanged),
                ValidateItemLength);

        public static readonly DependencyProperty SpacingProperty =
            SpacingPanel.SpacingProperty.AddOwner(
                typeof(VirtualizingSpacingStackPanel),
                new FrameworkPropertyMetadata(
                    0.0,
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.AffectsArrange,
                    OnLayoutPropertyChanged));

        public static readonly DependencyProperty BorderBrushProperty =
            Border.BorderBrushProperty.AddOwner(typeof(VirtualizingSpacingStackPanel));

        public static readonly DependencyProperty BorderThicknessProperty =
            Border.BorderThicknessProperty.AddOwner(
                typeof(VirtualizingSpacingStackPanel),
                new FrameworkPropertyMetadata(new Thickness(), FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty CornerRadiusProperty =
            Border.CornerRadiusProperty.AddOwner(typeof(VirtualizingSpacingStackPanel));

        private Point _offset;
        private Size _extent;
        private Size _viewport;

        public VirtualizingSpacingStackPanel()
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

            var itemLength = GetPrimaryItemLength();
            var itemStart = itemIndex * (itemLength + Spacing);
            var itemEnd = itemStart + itemLength;
            var offset = GetPrimaryOffset();
            var viewport = GetPrimaryViewportLength();
            if (itemStart < offset)
                SetPrimaryOffset(itemStart);
            else if (itemEnd > offset + viewport)
                SetPrimaryOffset(itemEnd - viewport);

            return Orientation == Orientation.Horizontal
                ? new Rect(itemStart - GetPrimaryOffset(), 0, itemLength, ViewportHeight)
                : new Rect(0, itemStart - GetPrimaryOffset(), ViewportWidth, itemLength);
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
            if (Orientation == Orientation.Horizontal)
                SetPrimaryOffset(offset);
        }

        public void SetVerticalOffset(double offset)
        {
            if (Orientation == Orientation.Vertical)
                SetPrimaryOffset(offset);
        }

        protected override void BringIndexIntoView(int index)
        {
            if (index >= 0)
                SetPrimaryOffset(index * (GetPrimaryItemLength() + Spacing));
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

            var itemLength = GetPrimaryItemLength();
            for (var childIndex = 0; childIndex < InternalChildren.Count; childIndex++)
            {
                var itemIndex = ItemContainerGenerator.IndexFromGeneratorPosition(
                    new GeneratorPosition(childIndex, 0));
                if (itemIndex < 0)
                {
                    InternalChildren[childIndex].Arrange(new Rect());
                    continue;
                }

                var primaryOffset = itemIndex * (itemLength + Spacing) - GetPrimaryOffset();
                var childRect = Orientation == Orientation.Horizontal
                    ? new Rect(primaryOffset, 0, ItemWidth, finalSize.Height)
                    : new Rect(0, primaryOffset, finalSize.Width, ItemHeight);
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
            var itemLength = GetPrimaryItemLength();
            var extentLength = itemCount == 0
                ? 0
                : itemCount * itemLength + (itemCount - 1) * Spacing;
            _extent = Orientation == Orientation.Horizontal
                ? new Size(extentLength, viewport.Height)
                : new Size(viewport.Width, extentLength);
            SetPrimaryOffset(GetPrimaryOffset());

            var itemStep = itemLength + Spacing;
            var firstItemIndex = Math.Max(0, (int)Math.Floor(GetPrimaryOffset() / itemStep));
            var visibleItemCount = Math.Max(
                1,
                (int)Math.Ceiling(GetPrimaryViewportLength() / itemStep) + 1);
            var lastItemIndex = Math.Min(itemCount, firstItemIndex + visibleItemCount);

            CleanUpItems(firstItemIndex, lastItemIndex);
            RealizeItems(firstItemIndex, lastItemIndex, viewport);
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

        private double GetPrimaryOffset() =>
            Orientation == Orientation.Horizontal ? HorizontalOffset : VerticalOffset;

        private double GetPrimaryViewportLength() =>
            Orientation == Orientation.Horizontal ? ViewportWidth : ViewportHeight;

        private int GetItemIndex(Visual visual)
        {
            var container = FindContainer(visual);
            if (container == null)
                return -1;

            return ItemContainerGenerator.IndexFromGeneratorPosition(
                new GeneratorPosition(InternalChildren.IndexOf(container), 0));
        }

        private void RealizeItems(int firstItemIndex, int lastItemIndex, Size viewport)
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

                    child.Measure(Orientation == Orientation.Horizontal
                        ? new Size(ItemWidth, viewport.Height)
                        : new Size(viewport.Width, ItemHeight));
                }
            }
        }

        private void SetPrimaryOffset(double offset)
        {
            if (double.IsNaN(offset) || double.IsInfinity(offset))
                return;

            var maximumOffset = Math.Max(0, GetPrimaryExtentLength() - GetPrimaryViewportLength());
            var clampedOffset = Math.Max(0, Math.Min(offset, maximumOffset));
            if (Math.Abs(GetPrimaryOffset() - clampedOffset) < 0.01)
                return;

            if (Orientation == Orientation.Horizontal)
                _offset.X = clampedOffset;
            else
                _offset.Y = clampedOffset;

            ScrollOwner?.InvalidateScrollInfo();
            InvalidateMeasure();
        }

        private static void OnLayoutPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
        {
            var panel = (VirtualizingSpacingStackPanel)dependencyObject;
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
            CanHorizontallyScroll = Orientation == Orientation.Horizontal;
            CanVerticallyScroll = Orientation == Orientation.Vertical;
        }

        private double GetPrimaryExtentLength() =>
            Orientation == Orientation.Horizontal ? ExtentWidth : ExtentHeight;

        private static bool ValidateItemLength(object value)
        {
            var length = (double)value;
            return length > 0 && !double.IsNaN(length) && !double.IsInfinity(length);
        }
    }
}
