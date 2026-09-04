using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WinCraft.UI
{
    public class SpacingGrid : Grid
    {
        public static readonly DependencyProperty ColumnsProperty =
            DependencyProperty.Register(
                nameof(Columns),
                typeof(string),
                typeof(SpacingGrid),
                new FrameworkPropertyMetadata(
                    string.Empty,
                    FrameworkPropertyMetadataOptions.AffectsMeasure,
                    OnColumnsChanged));

        public static readonly DependencyProperty RowsProperty =
            DependencyProperty.Register(
                nameof(Rows),
                typeof(string),
                typeof(SpacingGrid),
                new FrameworkPropertyMetadata(
                    string.Empty,
                    FrameworkPropertyMetadataOptions.AffectsMeasure,
                    OnRowsChanged));

        public static readonly DependencyProperty RowSpacingProperty =
            DependencyProperty.Register(
                nameof(RowSpacing),
                typeof(double),
                typeof(SpacingGrid),
                new FrameworkPropertyMetadata(
                    0.0,
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.AffectsArrange),
                ValidateSpacing);

        public static readonly DependencyProperty ColumnSpacingProperty =
            DependencyProperty.Register(
                nameof(ColumnSpacing),
                typeof(double),
                typeof(SpacingGrid),
                new FrameworkPropertyMetadata(
                    0.0,
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.AffectsArrange),
                ValidateSpacing);

        public string Columns
        {
            get => (string)GetValue(ColumnsProperty);
            set => SetValue(ColumnsProperty, value);
        }

        public string Rows
        {
            get => (string)GetValue(RowsProperty);
            set => SetValue(RowsProperty, value);
        }

        public static readonly DependencyProperty PaddingProperty =
            Border.PaddingProperty.AddOwner(
                typeof(SpacingGrid),
                new FrameworkPropertyMetadata(new Thickness(), FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static readonly DependencyProperty BorderBrushProperty =
            Border.BorderBrushProperty.AddOwner(
                typeof(SpacingGrid),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty BorderThicknessProperty =
            Border.BorderThicknessProperty.AddOwner(
                typeof(SpacingGrid),
                new FrameworkPropertyMetadata(new Thickness(), FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty CornerRadiusProperty =
            Border.CornerRadiusProperty.AddOwner(
                typeof(SpacingGrid),
                new FrameworkPropertyMetadata(new CornerRadius(), FrameworkPropertyMetadataOptions.AffectsRender));

        public double RowSpacing
        {
            get => (double)GetValue(RowSpacingProperty);
            set => SetValue(RowSpacingProperty, value);
        }

        public double ColumnSpacing
        {
            get => (double)GetValue(ColumnSpacingProperty);
            set => SetValue(ColumnSpacingProperty, value);
        }

        public Thickness Padding
        {
            get => (Thickness)GetValue(PaddingProperty);
            set => SetValue(PaddingProperty, value);
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

        internal void ApplySpacing(Spacing spacing)
        {
            SpacingToken.Apply(this, spacing, RowSpacingProperty, ColumnSpacingProperty);
        }

        protected override Size MeasureOverride(Size constraint)
        {
            var inner = SpacingPanel.InnerConstraint(Padding, constraint);
            var gridConstraint = new Size(
                Math.Max(0, inner.Width - TotalColumnSpacing()),
                Math.Max(0, inner.Height - TotalRowSpacing()));
            var result = base.MeasureOverride(gridConstraint);
            return SpacingPanel.OuterSize(Padding, new Size(
                result.Width + TotalColumnSpacing(),
                result.Height + TotalRowSpacing()));
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            var inner = SpacingPanel.InnerArrangeRect(Padding, arrangeSize);
            var columnWidths = ComputeColumnWidths(inner.Width);
            var rowHeights = ComputeRowHeights(inner.Height);

            for (var index = 0; index < InternalChildren.Count; index++)
            {
                var child = InternalChildren[index];
                if (child.Visibility == Visibility.Collapsed) continue;

                var col = GetColumn(child);
                var row = GetRow(child);
                var colSpan = GetColumnSpan(child);
                var rowSpan = GetRowSpan(child);

                col = col >= 0 && col < columnWidths.Length ? col : 0;
                row = row >= 0 && row < rowHeights.Length ? row : 0;
                colSpan = Math.Min(Math.Max(1, colSpan), columnWidths.Length - col);
                rowSpan = Math.Min(Math.Max(1, rowSpan), rowHeights.Length - row);

                var x = columnWidths[col].Position;
                var y = rowHeights[row].Position;
                var width = 0.0;
                for (var c = col; c < col + colSpan; c++)
                    width += columnWidths[c].Width + (c < col + colSpan - 1 ? ColumnSpacing : 0);
                var height = 0.0;
                for (var r = row; r < row + rowSpan; r++)
                    height += rowHeights[r].Height + (r < row + rowSpan - 1 ? RowSpacing : 0);

                child.Arrange(new Rect(inner.X + x, inner.Y + y,
                    Math.Max(0, width),
                    Math.Max(0, height)));
            }

            return arrangeSize;
        }

        protected override void OnRender(DrawingContext dc)
        {
            SpacingPanel.RenderPanelBackground(
                dc, ActualWidth, ActualHeight, Background, CornerRadius, BorderBrush, BorderThickness);
        }

        private struct ColumnResult
        {
            public double Position;
            public double Width;
        }

        private struct RowResult
        {
            public double Position;
            public double Height;
        }

        private ColumnResult[] ComputeColumnWidths(double availableWidth)
        {
            var count = ColumnDefinitions.Count;
            if (count == 0)
                return new[] { new ColumnResult { Width = availableWidth } };

            var results = new ColumnResult[count];
            var starColumns = new List<int>();
            var usedWidth = 0.0;
            var totalStarWeight = 0.0;

            for (var i = 0; i < count; i++)
                usedWidth += i > 0 ? ColumnSpacing : 0;

            // First pass: Absolute and Auto columns
            for (var i = 0; i < count; i++)
            {
                var def = ColumnDefinitions[i];
                if (def.Width.IsAbsolute)
                {
                    results[i].Width = def.Width.Value;
                }
                else if (def.Width.IsStar)
                {
                    starColumns.Add(i);
                    totalStarWeight += def.Width.Value;
                }
                else
                {
                    // Auto: use max child desired width in this column
                    results[i].Width = MeasureColumnAutoWidth(i);
                }

                usedWidth += results[i].Width;
            }

            // Second pass: distribute remaining space to Star columns
            if (starColumns.Count > 0 && totalStarWeight > 0)
            {
                var remaining = Math.Max(0, availableWidth - usedWidth);
                for (var i = 0; i < starColumns.Count; i++)
                {
                    var col = starColumns[i];
                    var weight = ColumnDefinitions[col].Width.Value;
                    results[col].Width = i == starColumns.Count - 1
                        ? remaining
                        : Math.Max(0, remaining * (weight / totalStarWeight));
                    remaining -= results[col].Width;
                    totalStarWeight -= weight;
                }
            }

            // Compute positions
            var offset = 0.0;
            for (var i = 0; i < count; i++)
            {
                results[i].Position = offset;
                offset += results[i].Width + ColumnSpacing;
            }

            return results;
        }

        private RowResult[] ComputeRowHeights(double availableHeight)
        {
            var count = RowDefinitions.Count;
            if (count == 0)
                return new[] { new RowResult { Height = availableHeight } };

            var results = new RowResult[count];
            var starRows = new List<int>();
            var usedHeight = 0.0;
            var totalStarWeight = 0.0;

            for (var i = 0; i < count; i++)
                usedHeight += i > 0 ? RowSpacing : 0;

            // First pass: Absolute and Auto rows
            for (var i = 0; i < count; i++)
            {
                var def = RowDefinitions[i];
                if (def.Height.IsAbsolute)
                {
                    results[i].Height = def.Height.Value;
                }
                else if (def.Height.IsStar)
                {
                    starRows.Add(i);
                    totalStarWeight += def.Height.Value;
                }
                else
                {
                    results[i].Height = MeasureRowAutoHeight(i);
                }

                usedHeight += results[i].Height;
            }

            // Second pass: distribute remaining space to Star rows
            if (starRows.Count > 0 && totalStarWeight > 0)
            {
                var remaining = Math.Max(0, availableHeight - usedHeight);
                for (var i = 0; i < starRows.Count; i++)
                {
                    var row = starRows[i];
                    var weight = RowDefinitions[row].Height.Value;
                    results[row].Height = i == starRows.Count - 1
                        ? remaining
                        : Math.Max(0, remaining * (weight / totalStarWeight));
                    remaining -= results[row].Height;
                    totalStarWeight -= weight;
                }
            }

            // Compute positions
            var offset = 0.0;
            for (var i = 0; i < count; i++)
            {
                results[i].Position = offset;
                offset += results[i].Height + RowSpacing;
            }

            return results;
        }

        private double MeasureColumnAutoWidth(int column)
        {
            var maxWidth = 0.0;
            for (var i = 0; i < InternalChildren.Count; i++)
            {
                var child = InternalChildren[i];
                if (child.Visibility == Visibility.Collapsed)
                    continue;

                var col = GetColumn(child);
                var colSpan = GetColumnSpan(child);
                if (col != column || colSpan != 1)
                    continue;

                maxWidth = Math.Max(maxWidth, child.DesiredSize.Width);
            }

            return maxWidth;
        }

        private double MeasureRowAutoHeight(int row)
        {
            var maxHeight = 0.0;
            for (var i = 0; i < InternalChildren.Count; i++)
            {
                var child = InternalChildren[i];
                if (child.Visibility == Visibility.Collapsed)
                    continue;

                var r = GetRow(child);
                var rowSpan = GetRowSpan(child);
                if (r != row || rowSpan != 1)
                    continue;

                maxHeight = Math.Max(maxHeight, child.DesiredSize.Height);
            }

            return maxHeight;
        }

        private double TotalColumnSpacing()
        {
            var cols = ColumnDefinitions.Count;
            return cols > 1 ? ColumnSpacing * (cols - 1) : 0;
        }

        private double TotalRowSpacing()
        {
            var rows = RowDefinitions.Count;
            return rows > 1 ? RowSpacing * (rows - 1) : 0;
        }


        private static bool ValidateSpacing(object value)
        {
            var spacing = (double)value;
            return spacing >= 0 && !double.IsNaN(spacing) && !double.IsInfinity(spacing);
        }

        private static void OnColumnsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var grid = (SpacingGrid)d;
            grid.ColumnDefinitions.Clear();
            var value = (string)e.NewValue;
            if (!string.IsNullOrEmpty(value))
            {
                foreach (var part in value.Split(','))
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = ParseGridLength(part.Trim()) });
            }
        }

        private static void OnRowsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var grid = (SpacingGrid)d;
            grid.RowDefinitions.Clear();
            var value = (string)e.NewValue;
            if (!string.IsNullOrEmpty(value))
            {
                foreach (var part in value.Split(','))
                    grid.RowDefinitions.Add(new RowDefinition { Height = ParseGridLength(part.Trim()) });
            }
        }

        private static GridLength ParseGridLength(string text)
        {
            if (string.IsNullOrEmpty(text))
                return GridLength.Auto;

            if (string.Equals(text, "Auto", StringComparison.OrdinalIgnoreCase))
                return GridLength.Auto;

            if (text == "*")
                return new GridLength(1, GridUnitType.Star);

            if (text.EndsWith("*", StringComparison.Ordinal))
            {
                var weightStr = text.Substring(0, text.Length - 1);
                if (double.TryParse(weightStr, out var weight) && weight > 0)
                    return new GridLength(weight, GridUnitType.Star);
            }

            // Pixel value, optionally with "px" suffix
            var pixelStr = text.EndsWith("px", StringComparison.OrdinalIgnoreCase)
                ? text.Substring(0, text.Length - 2)
                : text;

            if (double.TryParse(pixelStr, out var pixels) && pixels >= 0)
                return new GridLength(pixels);

            return GridLength.Auto;
        }
    }
}
