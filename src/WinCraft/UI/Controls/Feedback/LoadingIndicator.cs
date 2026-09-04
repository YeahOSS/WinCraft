using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace WinCraft.UI
{
    public class LoadingIndicator : Control
    {
        private const double DefaultSize = 24.0;
        private bool _isAnimating;

        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register(
                nameof(IsActive),
                typeof(bool),
                typeof(LoadingIndicator),
                new FrameworkPropertyMetadata(true, OnIsActiveChanged));

        public bool IsActive
        {
            get => (bool)GetValue(IsActiveProperty);
            set => SetValue(IsActiveProperty, value);
        }

        public static readonly DependencyProperty DotCountProperty =
            DependencyProperty.Register(
                nameof(DotCount),
                typeof(int),
                typeof(LoadingIndicator),
                new FrameworkPropertyMetadata(8, FrameworkPropertyMetadataOptions.AffectsRender));

        public int DotCount
        {
            get => (int)GetValue(DotCountProperty);
            set => SetValue(DotCountProperty, value);
        }

        public static readonly DependencyProperty DotSizeRatioProperty =
            DependencyProperty.Register(
                nameof(DotSizeRatio),
                typeof(double),
                typeof(LoadingIndicator),
                new FrameworkPropertyMetadata(0.13, FrameworkPropertyMetadataOptions.AffectsRender));

        public double DotSizeRatio
        {
            get => (double)GetValue(DotSizeRatioProperty);
            set => SetValue(DotSizeRatioProperty, value);
        }

        private static readonly DependencyProperty AngleProperty =
            DependencyProperty.Register(
                nameof(Angle),
                typeof(double),
                typeof(LoadingIndicator),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

        private double Angle
        {
            get => (double)GetValue(AngleProperty);
            set => SetValue(AngleProperty, value);
        }

        public LoadingIndicator()
        {
            Focusable = false;
            IsHitTestVisible = false;
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            IsVisibleChanged += OnIsVisibleChanged;
        }

        protected override Size MeasureOverride(Size constraint)
        {
            return new Size(
                GetDesiredLength(Width, constraint.Width),
                GetDesiredLength(Height, constraint.Height));
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            var brush = Foreground;
            if (brush == null)
                return;

            var size = Math.Min(ActualWidth, ActualHeight);
            if (size <= 0)
                return;

            var count = Math.Max(3, DotCount);
            var dotDiameter = GetClampedDotDiameter(size);
            var dotRadius = dotDiameter / 2.0;
            var orbitRadius = Math.Max(0, (size - dotDiameter) * 0.39);
            var center = new Point(ActualWidth / 2.0, ActualHeight / 2.0);
            var step = 360.0 / count;

            for (var i = 0; i < count; i++)
            {
                var angle = (Angle - 90.0 + step * i) * Math.PI / 180.0;
                var opacity = 0.22 + 0.78 * (i + 1) / count;
                var point = new Point(
                    center.X + orbitRadius * Math.Cos(angle),
                    center.Y + orbitRadius * Math.Sin(angle));

                drawingContext.PushOpacity(opacity);
                drawingContext.DrawEllipse(brush, null, point, dotRadius, dotRadius);
                drawingContext.Pop();
            }
        }

        private static void OnIsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((LoadingIndicator)d).UpdateAnimation();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateAnimation();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            StopAnimation();
        }

        private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            UpdateAnimation();
        }

        private void UpdateAnimation()
        {
            if (IsLoaded && IsVisible && IsActive)
            {
                StartAnimation();
                return;
            }

            StopAnimation();
        }

        private void StartAnimation()
        {
            if (_isAnimating)
                return;

            _isAnimating = true;
            var animation = new DoubleAnimation(0, 360, new Duration(TimeSpan.FromMilliseconds(900)))
            {
                RepeatBehavior = RepeatBehavior.Forever
            };

            BeginAnimation(AngleProperty, animation, HandoffBehavior.SnapshotAndReplace);
        }

        private void StopAnimation()
        {
            if (!_isAnimating)
                return;

            _isAnimating = false;
            BeginAnimation(AngleProperty, null);
            Angle = 0;
        }

        private double GetClampedDotDiameter(double size)
        {
            var ratio = DotSizeRatio;
            if (double.IsNaN(ratio) || double.IsInfinity(ratio))
                ratio = 0.13;

            ratio = Math.Max(0.06, Math.Min(0.22, ratio));
            return Math.Max(1.0, size * ratio);
        }

        private static double GetDesiredLength(double explicitLength, double constraintLength)
        {
            var desired = double.IsNaN(explicitLength) ? DefaultSize : explicitLength;
            if (!double.IsInfinity(constraintLength))
                return Math.Min(desired, constraintLength);

            return desired;
        }
    }
}
