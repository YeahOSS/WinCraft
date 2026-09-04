using System;
using System.Windows;
using System.Windows.Documents;

namespace WinCraft.UI
{
    public enum Spacing
    {
        None,
        XSmall,
        Small,
        Normal,
        Large,
        XLarge
    }

    public enum ControlVariant
    {
        Ghost,
        Outline,
        Solid
    }

    public enum VisualRole
    {
        Base,
        Secondary,
        Primary,
        Info,
        Success,
        Warning,
        Error,
        Ghost
    }

    public static class Design
    {
        public static readonly DependencyProperty VisualRoleProperty =
            DependencyProperty.RegisterAttached(
                "VisualRole",
                typeof(VisualRole),
                typeof(Design),
                new FrameworkPropertyMetadata(
                    VisualRole.Base,
                    FrameworkPropertyMetadataOptions.Inherits),
                ValidateVisualRole);

        public static VisualRole GetVisualRole(DependencyObject target) =>
            (VisualRole)target.GetValue(VisualRoleProperty);

        public static void SetVisualRole(DependencyObject target, VisualRole value) =>
            target.SetValue(VisualRoleProperty, value);

        public static readonly DependencyProperty VariantProperty =
            DependencyProperty.RegisterAttached(
                "Variant",
                typeof(ControlVariant),
                typeof(Design),
                new FrameworkPropertyMetadata(
                    ControlVariant.Solid,
                    FrameworkPropertyMetadataOptions.Inherits),
                ValidateControlVariant);

        public static ControlVariant GetVariant(DependencyObject target) =>
            (ControlVariant)target.GetValue(VariantProperty);

        public static void SetVariant(DependencyObject target, ControlVariant value) =>
            target.SetValue(VariantProperty, value);

        public static readonly DependencyProperty IsIconFilledProperty =
            DependencyProperty.RegisterAttached(
                "IsIconFilled",
                typeof(bool),
                typeof(Design),
                new FrameworkPropertyMetadata(
                    false,
                    FrameworkPropertyMetadataOptions.Inherits,
                    OnIsIconFilledChanged));

        public static bool GetIsIconFilled(DependencyObject target) =>
            (bool)target.GetValue(IsIconFilledProperty);

        public static void SetIsIconFilled(DependencyObject target, bool value) =>
            target.SetValue(IsIconFilledProperty, value);

        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.RegisterAttached(
                "CornerRadius",
                typeof(CornerRadius),
                typeof(Design),
                new FrameworkPropertyMetadata(
                    new CornerRadius(),
                    FrameworkPropertyMetadataOptions.Inherits));

        public static CornerRadius GetCornerRadius(DependencyObject target) =>
            (CornerRadius)target.GetValue(CornerRadiusProperty);

        public static void SetCornerRadius(DependencyObject target, CornerRadius value) =>
            target.SetValue(CornerRadiusProperty, value);

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.RegisterAttached(
                "Icon",
                typeof(IconGlyph),
                typeof(Design),
                new FrameworkPropertyMetadata(IconGlyph.None),
                ValidateIconGlyph);

        public static IconGlyph GetIcon(FrameworkElement target) =>
            (IconGlyph)target.GetValue(IconProperty);

        public static void SetIcon(FrameworkElement target, IconGlyph value) =>
            target.SetValue(IconProperty, value);

        public static readonly DependencyProperty CheckedIconProperty =
            DependencyProperty.RegisterAttached(
                "CheckedIcon",
                typeof(IconGlyph),
                typeof(Design),
                new FrameworkPropertyMetadata(IconGlyph.None),
                ValidateIconGlyph);

        public static IconGlyph GetCheckedIcon(FrameworkElement target) =>
            (IconGlyph)target.GetValue(CheckedIconProperty);

        public static void SetCheckedIcon(FrameworkElement target, IconGlyph value) =>
            target.SetValue(CheckedIconProperty, value);

        public static readonly DependencyProperty IsFilledOnCheckedProperty =
            DependencyProperty.RegisterAttached(
                "IsFilledOnChecked",
                typeof(bool),
                typeof(Design),
                new FrameworkPropertyMetadata(true));

        public static bool GetIsFilledOnChecked(DependencyObject target) =>
            (bool)target.GetValue(IsFilledOnCheckedProperty);

        public static void SetIsFilledOnChecked(DependencyObject target, bool value) =>
            target.SetValue(IsFilledOnCheckedProperty, value);

        /// <summary>
        /// Read-only bridge property set by style triggers on <see cref="ToggleButton"/>
        /// and <c>IconToggleButton</c> to signal the control template that the
        /// checked-icon block should be visible.
        /// </summary>
        internal static readonly DependencyProperty HasCheckedIconSwapProperty =
            DependencyProperty.RegisterAttached(
                "HasCheckedIconSwap",
                typeof(bool),
                typeof(Design),
                new FrameworkPropertyMetadata(false));

        internal static bool GetHasCheckedIconSwap(DependencyObject target) =>
            (bool)target.GetValue(HasCheckedIconSwapProperty);

        internal static void SetHasCheckedIconSwap(DependencyObject target, bool value) =>
            target.SetValue(HasCheckedIconSwapProperty, value);

        private static void OnIsIconFilledChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            if (target is IconBlock iconBlock)
                iconBlock.ApplyIsIconFilled((bool)e.NewValue);
        }

        private static bool ValidateVisualRole(object value) =>
            Enum.IsDefined(typeof(VisualRole), value);

        private static bool ValidateControlVariant(object value) =>
            Enum.IsDefined(typeof(ControlVariant), value);

        private static bool ValidateIconGlyph(object value) =>
            Enum.IsDefined(typeof(IconGlyph), value);
    }
}
