using System;
using System.Windows;
using System.Windows.Controls;

namespace WinCraft.UI
{
    public class SpacingStackPanel : SpacingPanel
    {
        public static readonly DependencyProperty OrientationProperty =
            StackPanel.OrientationProperty.AddOwner(
                typeof(SpacingStackPanel),
                new FrameworkPropertyMetadata(
                    Orientation.Vertical,
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.AffectsArrange));

        public Orientation Orientation
        {
            get => (Orientation)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var inner = InnerConstraint(availableSize);
            var isHorizontal = Orientation == Orientation.Horizontal;
            var primary = 0.0;
            var secondary = 0.0;
            var childCount = 0;
            var childConstraint = isHorizontal
                ? new Size(double.PositiveInfinity, inner.Height)
                : new Size(inner.Width, double.PositiveInfinity);

            foreach (UIElement child in InternalChildren)
            {
                if (!IsLayoutChild(child))
                    continue;

                child.Measure(childConstraint);
                var desiredSize = child.DesiredSize;
                primary += isHorizontal ? desiredSize.Width : desiredSize.Height;
                secondary = Math.Max(
                    secondary,
                    isHorizontal ? desiredSize.Height : desiredSize.Width);
                childCount++;
            }

            if (childCount > 1)
                primary += Spacing * (childCount - 1);

            return OuterSize(isHorizontal
                ? new Size(primary, secondary)
                : new Size(secondary, primary));
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var inner = InnerArrangeRect(finalSize);
            var isHorizontal = Orientation == Orientation.Horizontal;
            var offset = 0.0;
            var lastLayoutChildIndex = GetLastLayoutChildIndex();

            for (var index = 0; index < InternalChildren.Count; index++)
            {
                var child = InternalChildren[index];
                if (!IsLayoutChild(child))
                {
                    child.Arrange(new Rect());
                    continue;
                }

                var desired = child.DesiredSize;
                var crossAvailable = isHorizontal ? inner.Height : inner.Width;
                var crossDesired = isHorizontal ? desired.Height : desired.Width;
                var fe = child as FrameworkElement;
                bool crossStretch;
                bool? crossEnd;
                if (isHorizontal)
                {
                    switch (fe?.VerticalAlignment ?? VerticalAlignment.Stretch)
                    {
                        case VerticalAlignment.Top: crossStretch = false; crossEnd = false; break;
                        case VerticalAlignment.Bottom: crossStretch = false; crossEnd = true; break;
                        case VerticalAlignment.Center: crossStretch = false; crossEnd = null; break;
                        default: crossStretch = true; crossEnd = false; break;
                    }
                }
                else
                {
                    switch (fe?.HorizontalAlignment ?? HorizontalAlignment.Stretch)
                    {
                        case HorizontalAlignment.Left: crossStretch = false; crossEnd = false; break;
                        case HorizontalAlignment.Right: crossStretch = false; crossEnd = true; break;
                        case HorizontalAlignment.Center: crossStretch = false; crossEnd = null; break;
                        default: crossStretch = true; crossEnd = false; break;
                    }
                }
                GetCrossAxisLayout(crossDesired, crossAvailable, crossStretch, crossEnd,
                    out var crossSize, out var crossPos);

                var childRect = isHorizontal
                    ? new Rect(inner.X + offset, inner.Y + crossPos, desired.Width, crossSize)
                    : new Rect(inner.X + crossPos, inner.Y + offset, crossSize, desired.Height);

                child.Arrange(childRect);
                offset += isHorizontal ? desired.Width : desired.Height;
                if (index != lastLayoutChildIndex)
                    offset += Spacing;
            }

            return finalSize;
        }

        private static void GetCrossAxisLayout(
            double desired, double available,
            bool isStretch, bool? isEnd,
            out double size, out double position)
        {
            if (isStretch)
            {
                size = Math.Max(desired, available);
                position = 0;
            }
            else
            {
                size = Math.Min(desired, available);
                if (isEnd == true)
                    position = available - size;
                else if (isEnd == null)
                    position = (available - size) / 2;
                else
                    position = 0;
            }
        }
    }
}
