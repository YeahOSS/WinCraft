using System;
using System.Windows;
using System.Windows.Media;

namespace WinCraft.UI
{
    public class TabShoulderShape : FrameworkElement
    {
        private const double DefaultExponent = 4.0;

        public static readonly DependencyProperty FillProperty =
            DependencyProperty.Register(
                nameof(Fill),
                typeof(Brush),
                typeof(TabShoulderShape),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public Brush Fill
        {
            get => (Brush)GetValue(FillProperty);
            set => SetValue(FillProperty, value);
        }

        public static readonly DependencyProperty StrokeProperty =
            DependencyProperty.Register(
                nameof(Stroke),
                typeof(Brush),
                typeof(TabShoulderShape),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public Brush Stroke
        {
            get => (Brush)GetValue(StrokeProperty);
            set => SetValue(StrokeProperty, value);
        }

        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register(
                nameof(StrokeThickness),
                typeof(double),
                typeof(TabShoulderShape),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public double StrokeThickness
        {
            get => (double)GetValue(StrokeThicknessProperty);
            set => SetValue(StrokeThicknessProperty, value);
        }

        public static readonly DependencyProperty StrokeDashArrayProperty =
            DependencyProperty.Register(
                nameof(StrokeDashArray),
                typeof(DoubleCollection),
                typeof(TabShoulderShape),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public DoubleCollection StrokeDashArray
        {
            get => (DoubleCollection)GetValue(StrokeDashArrayProperty);
            set => SetValue(StrokeDashArrayProperty, value);
        }

        public static readonly DependencyProperty TopRadiusProperty =
            DependencyProperty.Register(
                nameof(TopRadius),
                typeof(double),
                typeof(TabShoulderShape),
                new FrameworkPropertyMetadata(8.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public double TopRadius
        {
            get => (double)GetValue(TopRadiusProperty);
            set => SetValue(TopRadiusProperty, value);
        }

        public static readonly DependencyProperty ShoulderWidthProperty =
            DependencyProperty.Register(
                nameof(ShoulderWidth),
                typeof(double),
                typeof(TabShoulderShape),
                new FrameworkPropertyMetadata(14.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public double ShoulderWidth
        {
            get => (double)GetValue(ShoulderWidthProperty);
            set => SetValue(ShoulderWidthProperty, value);
        }

        public static readonly DependencyProperty ShoulderHeightProperty =
            DependencyProperty.Register(
                nameof(ShoulderHeight),
                typeof(double),
                typeof(TabShoulderShape),
                new FrameworkPropertyMetadata(7.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public double ShoulderHeight
        {
            get => (double)GetValue(ShoulderHeightProperty);
            set => SetValue(ShoulderHeightProperty, value);
        }

        public static readonly DependencyProperty ExponentProperty =
            DependencyProperty.Register(
                nameof(Exponent),
                typeof(double),
                typeof(TabShoulderShape),
                new FrameworkPropertyMetadata(DefaultExponent, FrameworkPropertyMetadataOptions.AffectsRender));

        public double Exponent
        {
            get => (double)GetValue(ExponentProperty);
            set => SetValue(ExponentProperty, value);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            var bounds = new Rect(0, 0, ActualWidth, ActualHeight);
            if (bounds.Width <= 0 || bounds.Height <= 0)
                return;

            var geometry = CreateGeometry(bounds);
            var pen = CreatePen();
            drawingContext.DrawGeometry(Fill, pen, geometry);
        }

        private Pen CreatePen()
        {
            var stroke = Stroke;
            var thickness = StrokeThickness;
            if (stroke == null || thickness <= 0)
                return null;

            var pen = new Pen(stroke, thickness);
            if (StrokeDashArray != null && StrokeDashArray.Count > 0)
                pen.DashStyle = new DashStyle(StrokeDashArray, 0);
            return pen;
        }

        private Geometry CreateGeometry(Rect rect)
        {
            var exponent = Exponent <= 0 ? DefaultExponent : Exponent;
            var shoulderWidth = Math.Max(0, Math.Min(ShoulderWidth, rect.Width / 3.0));
            var shoulderHeight = Math.Max(0, Math.Min(ShoulderHeight, rect.Height));
            var bodyLeft = rect.Left + shoulderWidth;
            var bodyRight = rect.Right - shoulderWidth;
            var maxTopRadius = Math.Max(0, Math.Min((bodyRight - bodyLeft) / 2.0, rect.Height - shoulderHeight));
            var topRadius = Math.Max(0, Math.Min(TopRadius, maxTopRadius));
            var shoulderTop = rect.Bottom - shoulderHeight;

            var geometry = new StreamGeometry();
            using (var context = geometry.Open())
            {
                context.BeginFigure(new Point(rect.Left, rect.Bottom), true, true);
                SuperellipseBorder.AddCorner(
                    context,
                    rect.Left,
                    shoulderTop,
                    shoulderWidth,
                    shoulderHeight,
                    Math.PI / 2.0,
                    0,
                    exponent);

                context.LineTo(new Point(bodyLeft, rect.Top + topRadius), true, false);
                SuperellipseBorder.AddCorner(
                    context,
                    bodyLeft + topRadius,
                    rect.Top + topRadius,
                    topRadius,
                    Math.PI,
                    Math.PI * 1.5,
                    exponent);

                context.LineTo(new Point(bodyRight - topRadius, rect.Top), true, false);
                SuperellipseBorder.AddCorner(
                    context,
                    bodyRight - topRadius,
                    rect.Top + topRadius,
                    topRadius,
                    -Math.PI / 2.0,
                    0,
                    exponent);

                context.LineTo(new Point(bodyRight, shoulderTop), true, false);
                SuperellipseBorder.AddCorner(
                    context,
                    rect.Right,
                    shoulderTop,
                    shoulderWidth,
                    shoulderHeight,
                    Math.PI,
                    Math.PI / 2.0,
                    exponent);
            }

            if (geometry.CanFreeze)
                geometry.Freeze();

            return geometry;
        }
    }
}
