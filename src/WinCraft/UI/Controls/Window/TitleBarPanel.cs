using System;
using System.Windows;
using System.Windows.Controls;

namespace WinCraft.UI
{
    /// <summary>
    /// Title-bar layout panel for exactly three children: left group,
    /// center group, right group.  The center child is centered relative
    /// to the panel's full width, clamped so it never overlaps neighbors.
    /// </summary>
    public class TitleBarPanel : Panel
    {
        protected override Size MeasureOverride(Size availableSize)
        {
            UIElement left = GetChildOrNull(0);
            UIElement center = GetChildOrNull(1);
            UIElement right = GetChildOrNull(2);
            var unconstrained = new Size(double.PositiveInfinity, availableSize.Height);

            right?.Measure(unconstrained);
            double leftWidth = double.IsInfinity(availableSize.Width)
                ? double.PositiveInfinity
                : Math.Max(0, availableSize.Width - (right?.DesiredSize.Width ?? 0));
            left?.Measure(new Size(leftWidth, availableSize.Height));
            center?.Measure(unconstrained);

            double maxHeight = Math.Max(
                left?.DesiredSize.Height ?? 0,
                Math.Max(
                    center?.DesiredSize.Height ?? 0,
                    right?.DesiredSize.Height ?? 0));

            double width = double.IsInfinity(availableSize.Width)
                ? GetChildrenDesiredWidth()
                : availableSize.Width;
            return new Size(width, maxHeight);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            UIElement left = GetChildOrNull(0);
            UIElement center = GetChildOrNull(1);
            UIElement right = GetChildOrNull(2);

            // Right group (caption buttons) has priority; the left group is
            // clipped when a long title would otherwise overlap it.
            double rightWidth = Math.Min(right?.DesiredSize.Width ?? 0, finalSize.Width);
            double maximumLeftWidth = Math.Max(0, finalSize.Width - rightWidth);
            double leftWidth = (center?.DesiredSize.Width ?? 0) <= 0
                ? maximumLeftWidth
                : Math.Min(left?.DesiredSize.Width ?? 0, maximumLeftWidth);

            left?.Arrange(new Rect(0, 0, leftWidth, finalSize.Height));
            right?.Arrange(new Rect(
                Math.Max(0, finalSize.Width - rightWidth), 0, rightWidth, finalSize.Height));

            if (center != null)
            {
                ArrangeSlots(
                    finalSize.Width, leftWidth, center.DesiredSize.Width, rightWidth,
                    out double centerLeft, out double centerWidth);
                center.Arrange(new Rect(centerLeft, 0, centerWidth, finalSize.Height));
            }

            return finalSize;
        }

        /// <summary>
        /// Positions the center slot at the panel's horizontal midpoint,
        /// clamped between the left and right groups; shrinks when the gap
        /// is smaller than its desired width.
        /// </summary>
        internal static void ArrangeSlots(
            double totalWidth, double leftWidth, double centerDesired, double rightWidth,
            out double centerLeft, out double centerWidth)
        {
            double gap = Math.Max(0, totalWidth - leftWidth - rightWidth);
            centerWidth = Math.Min(centerDesired, gap);
            double ideal = (totalWidth - centerWidth) / 2.0;
            centerLeft = Math.Max(leftWidth,
                Math.Min(ideal, totalWidth - rightWidth - centerWidth));
        }

        private UIElement GetChildOrNull(int index)
        {
            return index < InternalChildren.Count ? InternalChildren[index] : null;
        }

        private double GetChildrenDesiredWidth()
        {
            double width = 0;
            foreach (UIElement child in InternalChildren)
            {
                if (child != null)
                    width += child.DesiredSize.Width;
            }
            return width;
        }
    }
}
