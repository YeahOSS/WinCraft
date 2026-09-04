using System.Windows;
using System.Windows.Controls;

namespace WinCraft.UI
{
    public static class SeparatorLayout
    {
        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.RegisterAttached(
                nameof(Orientation),
                typeof(Orientation),
                typeof(SeparatorLayout),
                new FrameworkPropertyMetadata(
                    Orientation.Horizontal,
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.AffectsArrange));

        public static Orientation GetOrientation(Separator target) =>
            (Orientation)target.GetValue(OrientationProperty);

        public static void SetOrientation(Separator target, Orientation value) =>
            target.SetValue(OrientationProperty, value);
    }
}
