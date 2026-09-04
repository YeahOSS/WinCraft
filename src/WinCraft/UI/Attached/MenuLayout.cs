using System.Windows;
using System.Windows.Controls;

namespace WinCraft.UI
{
    public static class MenuLayout
    {
        public static readonly DependencyProperty IsIconOnlyProperty =
            DependencyProperty.RegisterAttached(
                "IsIconOnly",
                typeof(bool),
                typeof(MenuLayout),
                new FrameworkPropertyMetadata(
                    false,
                    FrameworkPropertyMetadataOptions.Inherits |
                    FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static bool GetIsIconOnly(Menu target) =>
            (bool)target.GetValue(IsIconOnlyProperty);

        public static void SetIsIconOnly(Menu target, bool value) =>
            target.SetValue(IsIconOnlyProperty, value);
    }
}
