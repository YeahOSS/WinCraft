using System;
using System.Windows;
using System.Windows.Media;

namespace WinCraft.UI
{
    public enum FocusOutlineShape
    {
        RoundedRectangle,
        Ellipse
    }

    public static class FocusVisual
    {
        public static readonly DependencyProperty OutlineShapeProperty =
            DependencyProperty.RegisterAttached(
                "OutlineShape",
                typeof(FocusOutlineShape),
                typeof(FocusVisual),
                new FrameworkPropertyMetadata(FocusOutlineShape.RoundedRectangle),
                ValidateOutlineShape);

        public static FocusOutlineShape GetOutlineShape(UIElement target) =>
            (FocusOutlineShape)target.GetValue(OutlineShapeProperty);

        public static void SetOutlineShape(UIElement target, FocusOutlineShape value) =>
            target.SetValue(OutlineShapeProperty, value);

        public static readonly DependencyProperty NormalizedOutlineGeometryProperty =
            DependencyProperty.RegisterAttached(
                "NormalizedOutlineGeometry",
                typeof(Geometry),
                typeof(FocusVisual),
                new FrameworkPropertyMetadata(null));

        public static Geometry GetNormalizedOutlineGeometry(UIElement target) =>
            (Geometry)target.GetValue(NormalizedOutlineGeometryProperty);

        public static void SetNormalizedOutlineGeometry(UIElement target, Geometry value) =>
            target.SetValue(NormalizedOutlineGeometryProperty, value);

        private static bool ValidateOutlineShape(object value) =>
            Enum.IsDefined(typeof(FocusOutlineShape), value);
    }
}
