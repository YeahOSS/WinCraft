using System;
using System.Windows;
using System.Windows.Controls;

namespace WinCraft.UI
{
    public class SpacingWrapPanel : SpacingPanel
    {
        public static readonly DependencyProperty OrientationProperty =
            WrapPanel.OrientationProperty.AddOwner(
                typeof(SpacingWrapPanel),
                new FrameworkPropertyMetadata(
                    Orientation.Horizontal,
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.AffectsArrange));

        public static readonly DependencyProperty ItemWidthProperty =
            DependencyProperty.Register(
                nameof(ItemWidth),
                typeof(double),
                typeof(SpacingWrapPanel),
                new FrameworkPropertyMetadata(
                    double.NaN,
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.AffectsArrange),
                ValidateItemLength);

        public static readonly DependencyProperty ItemHeightProperty =
            DependencyProperty.Register(
                nameof(ItemHeight),
                typeof(double),
                typeof(SpacingWrapPanel),
                new FrameworkPropertyMetadata(
                    double.NaN,
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.AffectsArrange),
                ValidateItemLength);

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

        protected override Size MeasureOverride(Size availableSize)
        {
            var inner = InnerConstraint(availableSize);
            var isHorizontal = Orientation == Orientation.Horizontal;
            var availablePrimary = isHorizontal ? inner.Width : inner.Height;
            var desiredPrimary = 0.0;
            var desiredSecondary = 0.0;
            var linePrimary = 0.0;
            var lineSecondary = 0.0;
            var hasChildInLine = false;
            var hasLine = false;

            foreach (UIElement child in InternalChildren)
            {
                if (!IsLayoutChild(child))
                    continue;

                child.Measure(GetChildConstraint(inner));
                var childSize = GetChildSize(child.DesiredSize);
                var childPrimary = isHorizontal ? childSize.Width : childSize.Height;
                var childSecondary = isHorizontal ? childSize.Height : childSize.Width;
                var requiredPrimary = childPrimary + (hasChildInLine ? Spacing : 0);

                if (hasChildInLine && !double.IsInfinity(availablePrimary) &&
                    linePrimary + requiredPrimary > availablePrimary)
                {
                    desiredPrimary = Math.Max(desiredPrimary, linePrimary);
                    desiredSecondary += lineSecondary;
                    if (hasLine)
                        desiredSecondary += Spacing;

                    linePrimary = childPrimary;
                    lineSecondary = childSecondary;
                    hasLine = true;
                    continue;
                }

                linePrimary += requiredPrimary;
                lineSecondary = Math.Max(lineSecondary, childSecondary);
                hasChildInLine = true;
            }

            if (hasChildInLine)
            {
                desiredPrimary = Math.Max(desiredPrimary, linePrimary);
                desiredSecondary += lineSecondary;
                if (hasLine)
                    desiredSecondary += Spacing;
            }

            return OuterSize(isHorizontal
                ? new Size(desiredPrimary, desiredSecondary)
                : new Size(desiredSecondary, desiredPrimary));
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var inner = InnerArrangeRect(finalSize);
            var isHorizontal = Orientation == Orientation.Horizontal;
            var availablePrimary = isHorizontal ? inner.Width : inner.Height;
            var primaryOffset = 0.0;
            var secondaryOffset = 0.0;
            var lineSecondary = 0.0;
            var hasChildInLine = false;

            foreach (UIElement child in InternalChildren)
            {
                if (!IsLayoutChild(child))
                {
                    child.Arrange(new Rect());
                    continue;
                }

                var childSize = GetChildSize(child.DesiredSize);
                var childPrimary = isHorizontal ? childSize.Width : childSize.Height;
                var childSecondary = isHorizontal ? childSize.Height : childSize.Width;
                var requiredPrimary = childPrimary + (hasChildInLine ? Spacing : 0);

                if (hasChildInLine && !double.IsInfinity(availablePrimary) &&
                    primaryOffset + requiredPrimary > availablePrimary)
                {
                    secondaryOffset += lineSecondary + Spacing;
                    primaryOffset = 0;
                    lineSecondary = 0;
                    hasChildInLine = false;
                }

                if (hasChildInLine)
                    primaryOffset += Spacing;

                var childRect = isHorizontal
                    ? new Rect(inner.X + primaryOffset, inner.Y + secondaryOffset, childSize.Width, childSize.Height)
                    : new Rect(inner.X + secondaryOffset, inner.Y + primaryOffset, childSize.Width, childSize.Height);
                child.Arrange(childRect);

                primaryOffset += childPrimary;
                lineSecondary = Math.Max(lineSecondary, childSecondary);
                hasChildInLine = true;
            }

            return finalSize;
        }

        private Size GetChildConstraint(Size availableSize)
        {
            return new Size(
                double.IsNaN(ItemWidth) ? availableSize.Width : ItemWidth,
                double.IsNaN(ItemHeight) ? availableSize.Height : ItemHeight);
        }

        private Size GetChildSize(Size desiredSize)
        {
            return new Size(
                double.IsNaN(ItemWidth) ? desiredSize.Width : ItemWidth,
                double.IsNaN(ItemHeight) ? desiredSize.Height : ItemHeight);
        }

        private static bool ValidateItemLength(object value)
        {
            var length = (double)value;
            return (length >= 0 && !double.IsInfinity(length)) || double.IsNaN(length);
        }
    }
}
