using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WinCraft.UI
{
    public class SuperellipseBorder : Decorator
    {
        private const double DefaultExponent = 4.0;
        private const int CornerSegments = 10;

        private readonly Pen _borderPen = new();

        public static readonly DependencyProperty BackgroundProperty =
            DependencyProperty.Register(
                nameof(Background),
                typeof(Brush),
                typeof(SuperellipseBorder),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public Brush Background
        {
            get => (Brush)GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        public static readonly DependencyProperty BorderBrushProperty =
            DependencyProperty.Register(
                nameof(BorderBrush),
                typeof(Brush),
                typeof(SuperellipseBorder),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public Brush BorderBrush
        {
            get => (Brush)GetValue(BorderBrushProperty);
            set => SetValue(BorderBrushProperty, value);
        }

        public static readonly DependencyProperty BorderThicknessProperty =
            DependencyProperty.Register(
                nameof(BorderThickness),
                typeof(Thickness),
                typeof(SuperellipseBorder),
                new FrameworkPropertyMetadata(new Thickness(), FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public Thickness BorderThickness
        {
            get => (Thickness)GetValue(BorderThicknessProperty);
            set => SetValue(BorderThicknessProperty, value);
        }

        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                nameof(CornerRadius),
                typeof(CornerRadius),
                typeof(SuperellipseBorder),
                new FrameworkPropertyMetadata(new CornerRadius(), FrameworkPropertyMetadataOptions.AffectsRender));

        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        public static readonly DependencyProperty PaddingProperty =
            DependencyProperty.Register(
                nameof(Padding),
                typeof(Thickness),
                typeof(SuperellipseBorder),
                new FrameworkPropertyMetadata(new Thickness(), FrameworkPropertyMetadataOptions.AffectsMeasure));

        public Thickness Padding
        {
            get => (Thickness)GetValue(PaddingProperty);
            set => SetValue(PaddingProperty, value);
        }

        public static readonly DependencyProperty ExponentProperty =
            DependencyProperty.Register(
                nameof(Exponent),
                typeof(double),
                typeof(SuperellipseBorder),
                new FrameworkPropertyMetadata(DefaultExponent, FrameworkPropertyMetadataOptions.AffectsRender));

        public double Exponent
        {
            get => (double)GetValue(ExponentProperty);
            set => SetValue(ExponentProperty, value);
        }

        public static readonly DependencyProperty StrokeDashArrayProperty =
            DependencyProperty.Register(
                nameof(StrokeDashArray),
                typeof(DoubleCollection),
                typeof(SuperellipseBorder),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public DoubleCollection StrokeDashArray
        {
            get => (DoubleCollection)GetValue(StrokeDashArrayProperty);
            set => SetValue(StrokeDashArrayProperty, value);
        }

        public static readonly DependencyProperty StrokeDashOffsetProperty =
            DependencyProperty.Register(
                nameof(StrokeDashOffset),
                typeof(double),
                typeof(SuperellipseBorder),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public double StrokeDashOffset
        {
            get => (double)GetValue(StrokeDashOffsetProperty);
            set => SetValue(StrokeDashOffsetProperty, value);
        }

        protected override Size MeasureOverride(Size constraint)
        {
            var child = Child;
            if (child == null)
                return new Size();

            var reserved = Padding;
            var childConstraint = new Size(
                Math.Max(0, constraint.Width - reserved.Left - reserved.Right),
                Math.Max(0, constraint.Height - reserved.Top - reserved.Bottom));

            child.Measure(childConstraint);
            return new Size(
                child.DesiredSize.Width + reserved.Left + reserved.Right,
                child.DesiredSize.Height + reserved.Top + reserved.Bottom);
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            var child = Child;
            if (child == null)
                return arrangeSize;

            var reserved = Padding;
            child.Arrange(new Rect(
                reserved.Left,
                reserved.Top,
                Math.Max(0, arrangeSize.Width - reserved.Left - reserved.Right),
                Math.Max(0, arrangeSize.Height - reserved.Top - reserved.Bottom)));

            return arrangeSize;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            var bounds = new Rect(0, 0, ActualWidth, ActualHeight);
            if (bounds.Width <= 0 || bounds.Height <= 0)
                return;

            var radii = GetNormalizedCornerRadius(bounds.Size);
            var geometry = CreateSuperellipseGeometry(bounds, radii, Exponent);
            drawingContext.DrawGeometry(Background, null, geometry);

            var borderThickness = GetUniformBorderThickness();
            if (BorderBrush == null || borderThickness <= 0)
                return;

            var inset = borderThickness / 2.0;
            var borderBounds = new Rect(
                inset,
                inset,
                Math.Max(0, bounds.Width - borderThickness),
                Math.Max(0, bounds.Height - borderThickness));

            var borderGeometry = CreateSuperellipseGeometry(
                borderBounds,
                new CornerRadius(
                    Math.Max(0, radii.TopLeft - inset),
                    Math.Max(0, radii.TopRight - inset),
                    Math.Max(0, radii.BottomRight - inset),
                    Math.Max(0, radii.BottomLeft - inset)),
                Exponent);
            _borderPen.Brush = BorderBrush;
            _borderPen.Thickness = borderThickness;
            if (StrokeDashArray != null && StrokeDashArray.Count > 0)
            {
                if (_borderPen.DashStyle == null
                    || _borderPen.DashStyle.Dashes == null
                    || !StrokeDashArray.SequenceEqual(_borderPen.DashStyle.Dashes))
                {
                    _borderPen.DashStyle = new DashStyle(StrokeDashArray, StrokeDashOffset);
                }
                else
                {
                    _borderPen.DashStyle.Offset = StrokeDashOffset;
                }
            }
            else
            {
                _borderPen.DashStyle = null;
            }
            drawingContext.DrawGeometry(null, _borderPen, borderGeometry);
        }

        private double GetUniformBorderThickness()
        {
            var thickness = BorderThickness;
            return Math.Max(
                Math.Max(thickness.Left, thickness.Top),
                Math.Max(thickness.Right, thickness.Bottom));
        }

        private CornerRadius GetNormalizedCornerRadius(Size size)
        {
            var radius = CornerRadius;
            var topLeft = Math.Max(0, radius.TopLeft);
            var topRight = Math.Max(0, radius.TopRight);
            var bottomRight = Math.Max(0, radius.BottomRight);
            var bottomLeft = Math.Max(0, radius.BottomLeft);

            var scale = 1.0;
            scale = MinScale(scale, size.Width, topLeft + topRight);
            scale = MinScale(scale, size.Width, bottomLeft + bottomRight);
            scale = MinScale(scale, size.Height, topLeft + bottomLeft);
            scale = MinScale(scale, size.Height, topRight + bottomRight);

            return new CornerRadius(
                topLeft * scale,
                topRight * scale,
                bottomRight * scale,
                bottomLeft * scale);
        }

        private static double MinScale(double currentScale, double size, double radii)
        {
            if (radii <= 0)
                return currentScale;

            return Math.Min(currentScale, size / radii);
        }

        internal static Geometry CreateSuperellipseGeometry(Rect rect, CornerRadius radius, double exponent)
        {
            if (radius.TopLeft <= 0 &&
                radius.TopRight <= 0 &&
                radius.BottomRight <= 0 &&
                radius.BottomLeft <= 0)
            {
                return new RectangleGeometry(rect);
            }

            exponent = exponent <= 0 ? DefaultExponent : exponent;
            var geometry = new StreamGeometry();

            using (var context = geometry.Open())
            {
                var left = rect.Left;
                var top = rect.Top;
                var right = rect.Right;
                var bottom = rect.Bottom;
                var topLeft = new Point(left + radius.TopLeft, top);

                context.BeginFigure(topLeft, true, true);
                context.LineTo(new Point(right - radius.TopRight, top), true, false);
                AddCorner(context, right - radius.TopRight, top + radius.TopRight, radius.TopRight, -Math.PI / 2.0, 0, exponent);
                context.LineTo(new Point(right, bottom - radius.BottomRight), true, false);
                AddCorner(context, right - radius.BottomRight, bottom - radius.BottomRight, radius.BottomRight, 0, Math.PI / 2.0, exponent);
                context.LineTo(new Point(left + radius.BottomLeft, bottom), true, false);
                AddCorner(context, left + radius.BottomLeft, bottom - radius.BottomLeft, radius.BottomLeft, Math.PI / 2.0, Math.PI, exponent);
                context.LineTo(new Point(left, top + radius.TopLeft), true, false);
                AddCorner(context, left + radius.TopLeft, top + radius.TopLeft, radius.TopLeft, Math.PI, Math.PI * 1.5, exponent);
            }

            if (geometry.CanFreeze)
                geometry.Freeze();

            return geometry;
        }

        internal static void AddCorner(
            StreamGeometryContext context,
            double centerX,
            double centerY,
            double radius,
            double start,
            double end,
            double exponent)
        {
            AddCorner(context, centerX, centerY, radius, radius, start, end, exponent);
        }

        internal static void AddCorner(
            StreamGeometryContext context,
            double centerX,
            double centerY,
            double radiusX,
            double radiusY,
            double start,
            double end,
            double exponent)
        {
            if (radiusX <= 0 || radiusY <= 0)
                return;

            for (var i = 1; i <= CornerSegments; i++)
            {
                var t = start + (end - start) * i / CornerSegments;
                var x = Math.Cos(t);
                var y = Math.Sin(t);
                var power = 2.0 / exponent;
                var point = new Point(
                    centerX + radiusX * Math.Sign(x) * Math.Pow(Math.Abs(x), power),
                    centerY + radiusY * Math.Sign(y) * Math.Pow(Math.Abs(y), power));

                context.LineTo(point, true, false);
            }
        }
    }
}
