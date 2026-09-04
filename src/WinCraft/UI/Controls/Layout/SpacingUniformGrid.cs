using System;
using System.Windows;
using System.Windows.Controls;

namespace WinCraft.UI
{
    public class SpacingUniformGrid : SpacingPanel
    {
        public static readonly DependencyProperty RowsProperty =
            DependencyProperty.Register(
                nameof(Rows),
                typeof(int),
                typeof(SpacingUniformGrid),
                new FrameworkPropertyMetadata(
                    0,
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.AffectsArrange),
                ValidateNonNegativeInteger);

        public static readonly DependencyProperty ColumnsProperty =
            DependencyProperty.Register(
                nameof(Columns),
                typeof(int),
                typeof(SpacingUniformGrid),
                new FrameworkPropertyMetadata(
                    0,
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.AffectsArrange),
                ValidateNonNegativeInteger);

        public static readonly DependencyProperty FirstColumnProperty =
            DependencyProperty.Register(
                nameof(FirstColumn),
                typeof(int),
                typeof(SpacingUniformGrid),
                new FrameworkPropertyMetadata(
                    0,
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.AffectsArrange),
                ValidateNonNegativeInteger);

        public int Rows
        {
            get => (int)GetValue(RowsProperty);
            set => SetValue(RowsProperty, value);
        }

        public int Columns
        {
            get => (int)GetValue(ColumnsProperty);
            set => SetValue(ColumnsProperty, value);
        }

        public int FirstColumn
        {
            get => (int)GetValue(FirstColumnProperty);
            set => SetValue(FirstColumnProperty, value);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            GetLayoutDimensions(out var rows, out var columns, out _);
            if (rows == 0 || columns == 0)
                return new Size();

            var maxWidth = 0.0;
            var maxHeight = 0.0;
            for (var i = 0; i < InternalChildren.Count; i++)
            {
                var child = InternalChildren[i];
                if (!IsLayoutChild(child)) continue;
                child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                maxWidth = Math.Max(maxWidth, child.DesiredSize.Width);
                maxHeight = Math.Max(maxHeight, child.DesiredSize.Height);
            }

            return OuterSize(new Size(
                columns * maxWidth + (columns - 1) * Spacing,
                rows * maxHeight + (rows - 1) * Spacing));
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            GetLayoutDimensions(out var rows, out var columns, out var firstColumn);
            if (rows == 0 || columns == 0)
                return finalSize;

            var layoutSize = HorizontalAlignment == HorizontalAlignment.Stretch
                ? finalSize
                : new Size(
                    Math.Min(DesiredSize.Width, finalSize.Width),
                    Math.Min(DesiredSize.Height, finalSize.Height));
            var inner = InnerArrangeRect(layoutSize);
            var horizontalSpacing = GetEffectiveSpacing(inner.Width, columns);
            var verticalSpacing = GetEffectiveSpacing(inner.Height, rows);
            var cellWidth = Math.Max(0, (inner.Width - horizontalSpacing * (columns - 1)) / columns);
            var cellHeight = Math.Max(0, (inner.Height - verticalSpacing * (rows - 1)) / rows);

            var visibleIndex = 0;
            for (var i = 0; i < InternalChildren.Count; i++)
            {
                var child = InternalChildren[i];
                if (!IsLayoutChild(child)) continue;
                var cellIndex = visibleIndex + firstColumn;
                var row = cellIndex / columns;
                var column = cellIndex % columns;
                child.Arrange(new Rect(
                    inner.X + column * (cellWidth + horizontalSpacing),
                    inner.Y + row * (cellHeight + verticalSpacing),
                    cellWidth,
                    cellHeight));
                visibleIndex++;
            }

            return finalSize;
        }

        private void GetLayoutDimensions(out int rows, out int columns, out int firstColumn)
        {
            var childCount = 0;
            for (var i = 0; i < InternalChildren.Count; i++)
            {
                if (IsLayoutChild(InternalChildren[i]))
                    childCount++;
            }
            rows = Rows;
            columns = Columns;
            firstColumn = FirstColumn;

            if (childCount == 0)
            {
                rows = 0;
                columns = 0;
                firstColumn = 0;
                return;
            }

            if (columns == 0 && rows == 0)
            {
                columns = (int)Math.Ceiling(Math.Sqrt(childCount));
                rows = (childCount + columns - 1) / columns;
            }
            else if (columns == 0)
            {
                columns = (childCount + rows - 1) / rows;
            }

            if (firstColumn >= columns)
                firstColumn = 0;

            if (rows == 0)
                rows = (childCount + firstColumn + columns - 1) / columns;

            var requiredCells = childCount + firstColumn;
            if (rows * columns < requiredCells)
                rows = (requiredCells + columns - 1) / columns;
        }

        private double GetEffectiveSpacing(double availableLength, int cellCount)
        {
            if (cellCount <= 1 || double.IsInfinity(availableLength))
                return Spacing;

            return Math.Min(Spacing, availableLength / (cellCount - 1));
        }

        private static bool ValidateNonNegativeInteger(object value) => (int)value >= 0;
    }
}
