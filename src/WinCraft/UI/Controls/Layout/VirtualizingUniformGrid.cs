using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace WinCraft.UI
{
    public sealed class VirtualizingUniformGrid : VirtualizingPanel, IScrollInfo
    {
        private const double LineScrollAmount = 16.0;
        private const double WheelScrollAmount = 48.0;

        public static readonly DependencyProperty ColumnsProperty =
            DependencyProperty.Register(
                nameof(Columns),
                typeof(int),
                typeof(VirtualizingUniformGrid),
                new FrameworkPropertyMetadata(
                    1,
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.AffectsArrange,
                    OnLayoutPropertyChanged),
                ValidateColumns);

        public static readonly DependencyProperty BorderBrushProperty =
            Border.BorderBrushProperty.AddOwner(typeof(VirtualizingUniformGrid));

        public static readonly DependencyProperty BorderThicknessProperty =
            Border.BorderThicknessProperty.AddOwner(
                typeof(VirtualizingUniformGrid),
                new FrameworkPropertyMetadata(new Thickness(), FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty CornerRadiusProperty =
            Border.CornerRadiusProperty.AddOwner(typeof(VirtualizingUniformGrid));

        public static readonly DependencyProperty ItemHeightProperty =
            DependencyProperty.Register(
                nameof(ItemHeight),
                typeof(double),
                typeof(VirtualizingUniformGrid),
                new FrameworkPropertyMetadata(
                    150.0,
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.AffectsArrange,
                    OnLayoutPropertyChanged),
                ValidateItemHeight);

        private Point _offset;
        private Size _extent;
        private Size _viewport;

        public VirtualizingUniformGrid()
        {
            CanHorizontallyScroll = false;
            CanVerticallyScroll = true;
        }

        public int Columns
        {
            get => (int)GetValue(ColumnsProperty);
            set => SetValue(ColumnsProperty, value);
        }

        public double ItemHeight
        {
            get => (double)GetValue(ItemHeightProperty);
            set => SetValue(ItemHeightProperty, value);
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

        public double HorizontalOffset => 0;

        public ScrollViewer ScrollOwner { get; set; }

        public double VerticalOffset => _offset.Y;

        public double ViewportHeight => _viewport.Height;

        public double ViewportWidth => _viewport.Width;

        public void LineDown() => SetVerticalOffset(VerticalOffset + LineScrollAmount);

        public void LineLeft()
        {
        }

        public void LineRight()
        {
        }

        public void LineUp() => SetVerticalOffset(VerticalOffset - LineScrollAmount);

        public Rect MakeVisible(Visual visual, Rect rectangle)
        {
            var container = FindContainer(visual);
            if (container == null)
                return Rect.Empty;

            var childIndex = InternalChildren.IndexOf(container);
            var itemIndex = ItemContainerGenerator.IndexFromGeneratorPosition(
                new GeneratorPosition(childIndex, 0));
            if (itemIndex < 0)
                return Rect.Empty;

            var rowTop = itemIndex / Columns * ItemHeight;
            var rowBottom = rowTop + ItemHeight;
            if (rowTop < VerticalOffset)
                SetVerticalOffset(rowTop);
            else if (rowBottom > VerticalOffset + ViewportHeight)
                SetVerticalOffset(rowBottom - ViewportHeight);

            return new Rect(0, rowTop - VerticalOffset, ViewportWidth, ItemHeight);
        }

        public void MouseWheelDown() => SetVerticalOffset(VerticalOffset + WheelScrollAmount);

        public void MouseWheelLeft()
        {
        }

        public void MouseWheelRight()
        {
        }

        public void MouseWheelUp() => SetVerticalOffset(VerticalOffset - WheelScrollAmount);

        public void PageDown() => SetVerticalOffset(VerticalOffset + ViewportHeight);

        public void PageLeft()
        {
        }

        public void PageRight()
        {
        }

        public void PageUp() => SetVerticalOffset(VerticalOffset - ViewportHeight);

        public void SetHorizontalOffset(double offset)
        {
        }

        public void SetVerticalOffset(double offset)
        {
            if (double.IsNaN(offset) || double.IsInfinity(offset))
                return;

            var maximumOffset = Math.Max(0, ExtentHeight - ViewportHeight);
            var clampedOffset = Math.Max(0, Math.Min(offset, maximumOffset));
            if (Math.Abs(_offset.Y - clampedOffset) < 0.01)
                return;

            _offset.Y = clampedOffset;
            ScrollOwner?.InvalidateScrollInfo();
            InvalidateMeasure();
        }

        protected override void BringIndexIntoView(int index)
        {
            if (index < 0)
                return;

            SetVerticalOffset(index / Columns * ItemHeight);
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

            var cellWidth = finalSize.Width / Columns;
            for (var childIndex = 0; childIndex < InternalChildren.Count; childIndex++)
            {
                var itemIndex = ItemContainerGenerator.IndexFromGeneratorPosition(
                    new GeneratorPosition(childIndex, 0));
                if (itemIndex < 0)
                {
                    InternalChildren[childIndex].Arrange(new Rect());
                    continue;
                }

                var row = itemIndex / Columns;
                var column = itemIndex % Columns;
                InternalChildren[childIndex].Arrange(new Rect(
                    column * cellWidth,
                    row * ItemHeight - VerticalOffset,
                    cellWidth,
                    ItemHeight));
            }

            return finalSize;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var viewport = new Size(
                ResolveViewportLength(availableSize.Width, ActualWidth, ScrollOwner?.ViewportWidth, 1),
                ResolveViewportLength(availableSize.Height, ActualHeight, ScrollOwner?.ViewportHeight, ItemHeight));
            UpdateViewport(viewport);

            var itemCount = ItemsControl.GetItemsOwner(this)?.Items.Count ?? 0;
            var rowCount = (itemCount + Columns - 1) / Columns;
            _extent = new Size(viewport.Width, rowCount * ItemHeight);
            SetVerticalOffset(VerticalOffset);

            var firstRow = Math.Max(0, (int)Math.Floor(VerticalOffset / ItemHeight));
            var visibleRowCount = Math.Max(1, (int)Math.Ceiling(ViewportHeight / ItemHeight) + 1);
            var firstItemIndex = firstRow * Columns;
            var lastItemIndex = Math.Min(itemCount, (firstRow + visibleRowCount) * Columns);

            CleanUpItems(firstItemIndex, lastItemIndex);
            RealizeItems(firstItemIndex, lastItemIndex, viewport.Width / Columns);
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

        private void RealizeItems(int firstItemIndex, int lastItemIndex, double cellWidth)
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

                    child.Measure(new Size(cellWidth, ItemHeight));
                }
            }
        }

        private static void OnLayoutPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
        {
            ((VirtualizingUniformGrid)dependencyObject).InvalidateMeasure();
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
            var maximumOffset = Math.Max(0, ExtentHeight - ViewportHeight);
            _offset.Y = Math.Max(0, Math.Min(VerticalOffset, maximumOffset));
            ScrollOwner?.InvalidateScrollInfo();
            return true;
        }

        private static bool ValidateColumns(object value) => (int)value > 0;

        private static bool ValidateItemHeight(object value)
        {
            var height = (double)value;
            return height > 0 && !double.IsNaN(height) && !double.IsInfinity(height);
        }
    }
}
