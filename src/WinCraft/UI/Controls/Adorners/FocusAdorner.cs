using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace WinCraft.UI
{
    /// <summary>
    /// Window-level adorner that draws a shape-aware dashed focus ring with
    /// marching-ants animation around the keyboard-focused element.
    /// </summary>
    internal sealed class FocusAdorner : Adorner
    {
        private static readonly DoubleAnimation DashAnimation = new(0, -4,
            TimeSpan.FromSeconds(0.8)) { RepeatBehavior = RepeatBehavior.Forever };

        public static readonly DependencyProperty FocusBrushProperty =
            DependencyProperty.Register(
                nameof(FocusBrush),
                typeof(Brush),
                typeof(FocusAdorner),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public Brush FocusBrush
        {
            get => (Brush)GetValue(FocusBrushProperty);
            set => SetValue(FocusBrushProperty, value);
        }

        private readonly Pen _pen;
        private readonly DashStyle _dashStyle;

        public FocusAdorner(UIElement adornedElement) : base(adornedElement)
        {
            IsHitTestVisible = false;
            SnapsToDevicePixels = true;

            _dashStyle = new DashStyle(new double[] { 2, 2 }, 0);
            _pen = new Pen(Brushes.Transparent, 1) { DashStyle = _dashStyle };

            _dashStyle.BeginAnimation(DashStyle.OffsetProperty, DashAnimation);
        }

        /// <summary>Switch the focus-ring brush to a different theme resource.</summary>
        public void SetBrushKey(string key)
        {
            SetResourceReference(FocusBrushProperty, key);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            var element = AdornedElement;
            if (element == null) return;

            var size = element.RenderSize;
            if (size.Width <= 0 || size.Height <= 0) return;

            _pen.Brush = FocusBrush ?? Brushes.Transparent;

            var geometry = CreateFocusOutlineGeometry(element, size);
            drawingContext.DrawGeometry(null, _pen, geometry);
        }

        internal static Geometry CreateFocusOutlineGeometry(UIElement element, Size size)
        {
            var customGeometry = FocusVisual.GetNormalizedOutlineGeometry(element);
            if (customGeometry != null)
            {
                var geometry = new GeometryGroup();
                geometry.Children.Add(customGeometry);
                geometry.Transform = new ScaleTransform(size.Width, size.Height);
                return geometry;
            }

            var rect = new Rect(0, 0, size.Width, size.Height);
            if (FocusVisual.GetOutlineShape(element) == FocusOutlineShape.Ellipse)
                return new EllipseGeometry(rect);

            var cornerRadius = Design.GetCornerRadius(element);
            return SuperellipseBorder.CreateSuperellipseGeometry(rect, cornerRadius, exponent: 4.0);
        }

        public void Detach()
        {
            _dashStyle.BeginAnimation(DashStyle.OffsetProperty, null);
        }
    }
}
