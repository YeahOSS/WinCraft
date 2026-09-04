using System;
using System.Windows;
using System.Windows.Controls;

namespace WinCraft.UI
{
    public class SpacingDockPanel : SpacingPanel
    {
        public static readonly DependencyProperty DockProperty =
            DockPanel.DockProperty.AddOwner(typeof(SpacingDockPanel));

        public static readonly DependencyProperty LastChildFillProperty =
            DockPanel.LastChildFillProperty.AddOwner(
                typeof(SpacingDockPanel),
                new FrameworkPropertyMetadata(
                    true,
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.AffectsArrange));

        public bool LastChildFill
        {
            get => (bool)GetValue(LastChildFillProperty);
            set => SetValue(LastChildFillProperty, value);
        }

        public static Dock GetDock(UIElement element) => DockPanel.GetDock(element);

        public static void SetDock(UIElement element, Dock value) => DockPanel.SetDock(element, value);

        protected override Size MeasureOverride(Size availableSize)
        {
            var inner = InnerConstraint(availableSize);
            var accumulatedWidth = 0.0;
            var accumulatedHeight = 0.0;
            var desiredWidth = 0.0;
            var desiredHeight = 0.0;
            var lastLayoutChildIndex = GetLastLayoutChildIndex();

            for (var index = 0; index < InternalChildren.Count; index++)
            {
                var child = InternalChildren[index];
                if (!IsLayoutChild(child))
                    continue;

                child.Measure(new Size(
                    Math.Max(0, inner.Width - accumulatedWidth),
                    Math.Max(0, inner.Height - accumulatedHeight)));

                var desiredSize = child.DesiredSize;
                var spacing = index != lastLayoutChildIndex ? Spacing : 0;
                switch (GetDock(child))
                {
                    case Dock.Left:
                    case Dock.Right:
                        desiredHeight = Math.Max(desiredHeight, accumulatedHeight + desiredSize.Height);
                        accumulatedWidth += desiredSize.Width + spacing;
                        break;

                    case Dock.Top:
                    case Dock.Bottom:
                        desiredWidth = Math.Max(desiredWidth, accumulatedWidth + desiredSize.Width);
                        accumulatedHeight += desiredSize.Height + spacing;
                        break;
                }
            }

            return OuterSize(new Size(
                Math.Max(desiredWidth, accumulatedWidth),
                Math.Max(desiredHeight, accumulatedHeight)));
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var remaining = InnerArrangeRect(finalSize);
            var lastLayoutChildIndex = GetLastLayoutChildIndex();
            var fillChildIndex = LastChildFill ? lastLayoutChildIndex : -1;

            for (var index = 0; index < InternalChildren.Count; index++)
            {
                var child = InternalChildren[index];
                if (!IsLayoutChild(child))
                {
                    child.Arrange(new Rect());
                    continue;
                }

                if (index == fillChildIndex)
                {
                    child.Arrange(remaining);
                    continue;
                }

                var childRect = remaining;
                var reserveSpacing = index != lastLayoutChildIndex;
                switch (GetDock(child))
                {
                    case Dock.Left:
                        childRect.Width = ClampToAvailable(child.DesiredSize.Width, remaining.Width);
                        remaining.X += childRect.Width;
                        remaining.Width -= childRect.Width;
                        if (reserveSpacing)
                            ReserveHorizontalSpacing(ref remaining, false);
                        break;

                    case Dock.Right:
                        childRect.Width = ClampToAvailable(child.DesiredSize.Width, remaining.Width);
                        childRect.X = remaining.Right - childRect.Width;
                        remaining.Width -= childRect.Width;
                        if (reserveSpacing)
                            ReserveHorizontalSpacing(ref remaining, true);
                        break;

                    case Dock.Top:
                        childRect.Height = ClampToAvailable(child.DesiredSize.Height, remaining.Height);
                        remaining.Y += childRect.Height;
                        remaining.Height -= childRect.Height;
                        if (reserveSpacing)
                            ReserveVerticalSpacing(ref remaining, false);
                        break;

                    case Dock.Bottom:
                        childRect.Height = ClampToAvailable(child.DesiredSize.Height, remaining.Height);
                        childRect.Y = remaining.Bottom - childRect.Height;
                        remaining.Height -= childRect.Height;
                        if (reserveSpacing)
                            ReserveVerticalSpacing(ref remaining, true);
                        break;
                }

                child.Arrange(childRect);
            }

            return finalSize;
        }

        private void ReserveHorizontalSpacing(ref Rect remaining, bool fromRight)
        {
            var spacing = ClampToAvailable(Spacing, remaining.Width);
            if (!fromRight)
                remaining.X += spacing;

            remaining.Width -= spacing;
        }

        private void ReserveVerticalSpacing(ref Rect remaining, bool fromBottom)
        {
            var spacing = ClampToAvailable(Spacing, remaining.Height);
            if (!fromBottom)
                remaining.Y += spacing;

            remaining.Height -= spacing;
        }
    }
}
