using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WinCraft.UI
{
    public abstract class SpacingPanel : Panel
    {

        public static readonly DependencyProperty SpacingProperty =
            DependencyProperty.Register(
                nameof(Spacing),
                typeof(double),
                typeof(SpacingPanel),
                new FrameworkPropertyMetadata(
                    0.0,
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.AffectsArrange),
                ValidateSpacing);

        public double Spacing
        {
            get => (double)GetValue(SpacingProperty);
            set => SetValue(SpacingProperty, value);
        }

        public static readonly DependencyProperty BorderBrushProperty =
            Border.BorderBrushProperty.AddOwner(
                typeof(SpacingPanel),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public Brush BorderBrush
        {
            get => (Brush)GetValue(BorderBrushProperty);
            set => SetValue(BorderBrushProperty, value);
        }

        public static readonly DependencyProperty BorderThicknessProperty =
            Border.BorderThicknessProperty.AddOwner(
                typeof(SpacingPanel),
                new FrameworkPropertyMetadata(new Thickness(), FrameworkPropertyMetadataOptions.AffectsRender));

        public Thickness BorderThickness
        {
            get => (Thickness)GetValue(BorderThicknessProperty);
            set => SetValue(BorderThicknessProperty, value);
        }

        public static readonly DependencyProperty PaddingProperty =
            Border.PaddingProperty.AddOwner(
                typeof(SpacingPanel),
                new FrameworkPropertyMetadata(new Thickness(), FrameworkPropertyMetadataOptions.AffectsMeasure));

        public Thickness Padding
        {
            get => (Thickness)GetValue(PaddingProperty);
            set => SetValue(PaddingProperty, value);
        }

        public static readonly DependencyProperty CornerRadiusProperty =
            Border.CornerRadiusProperty.AddOwner(
                typeof(SpacingPanel),
                new FrameworkPropertyMetadata(new CornerRadius(), FrameworkPropertyMetadataOptions.AffectsRender));

        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        protected override void OnRender(DrawingContext dc)
        {
            RenderPanelBackground(dc, ActualWidth, ActualHeight, Background, CornerRadius, BorderBrush, BorderThickness);
        }

        internal static void RenderPanelBackground(
            DrawingContext dc,
            double actualWidth,
            double actualHeight,
            Brush background,
            CornerRadius cornerRadius,
            Brush borderBrush,
            Thickness borderThickness)
        {
            if (actualWidth <= 0 || actualHeight <= 0)
                return;

            if (background == null && (borderBrush == null || IsZeroThickness(borderThickness)))
                return;

            var bounds = new Rect(0, 0, actualWidth, actualHeight);
            var geometry = SuperellipseBorder.CreateSuperellipseGeometry(bounds, cornerRadius, exponent: 4.0);

            if (background != null)
                dc.DrawGeometry(background, null, geometry);

            if (borderBrush != null && !IsZeroThickness(borderThickness))
            {
                var innerBounds = new Rect(
                    borderThickness.Left,
                    borderThickness.Top,
                    Math.Max(0, bounds.Width - borderThickness.Left - borderThickness.Right),
                    Math.Max(0, bounds.Height - borderThickness.Top - borderThickness.Bottom));

                var borderGeometry = geometry;
                if (innerBounds.Width > 0 && innerBounds.Height > 0)
                {
                    var innerRadii = new CornerRadius(
                        Math.Max(0, cornerRadius.TopLeft - Math.Max(borderThickness.Left, borderThickness.Top)),
                        Math.Max(0, cornerRadius.TopRight - Math.Max(borderThickness.Right, borderThickness.Top)),
                        Math.Max(0, cornerRadius.BottomRight - Math.Max(borderThickness.Right, borderThickness.Bottom)),
                        Math.Max(0, cornerRadius.BottomLeft - Math.Max(borderThickness.Left, borderThickness.Bottom)));
                    var innerGeometry = SuperellipseBorder.CreateSuperellipseGeometry(innerBounds, innerRadii, exponent: 4.0);
                    borderGeometry = Geometry.Combine(geometry, innerGeometry, GeometryCombineMode.Exclude, Transform.Identity);
                }

                dc.DrawGeometry(borderBrush, null, borderGeometry);
            }
        }

        private static bool IsZeroThickness(Thickness t) =>
            t.Left <= 0 && t.Top <= 0 && t.Right <= 0 && t.Bottom <= 0;

        internal static Size InnerConstraint(Thickness padding, Size outer)
        {
            return new Size(
                Math.Max(0, outer.Width - padding.Left - padding.Right),
                Math.Max(0, outer.Height - padding.Top - padding.Bottom));
        }

        internal static Size OuterSize(Thickness padding, Size inner)
        {
            return new Size(
                inner.Width + padding.Left + padding.Right,
                inner.Height + padding.Top + padding.Bottom);
        }

        internal static Rect InnerArrangeRect(Thickness padding, Size finalSize)
        {
            return new Rect(
                padding.Left,
                padding.Top,
                Math.Max(0, finalSize.Width - padding.Left - padding.Right),
                Math.Max(0, finalSize.Height - padding.Top - padding.Bottom));
        }

        protected Size InnerConstraint(Size outer)
            => InnerConstraint(Padding, outer);

        protected Size OuterSize(Size inner)
            => OuterSize(Padding, inner);

        protected Rect InnerArrangeRect(Size finalSize)
            => InnerArrangeRect(Padding, finalSize);

        protected static bool IsLayoutChild(UIElement child) =>
            child.Visibility != Visibility.Collapsed;

        protected int GetLastLayoutChildIndex()
        {
            for (var childIndex = InternalChildren.Count - 1; childIndex >= 0; childIndex--)
            {
                if (IsLayoutChild(InternalChildren[childIndex]))
                    return childIndex;
            }

            return -1;
        }

        protected static double ClampToAvailable(double desired, double available) =>
            Math.Max(0, Math.Min(desired, available));

        internal void ApplySpacing(Spacing spacing)
        {
            SpacingToken.Apply(this, spacing, SpacingProperty);
        }

        private static bool ValidateSpacing(object value)
        {
            var spacing = (double)value;
            return spacing >= 0 && !double.IsNaN(spacing) && !double.IsInfinity(spacing);
        }
    }
}
