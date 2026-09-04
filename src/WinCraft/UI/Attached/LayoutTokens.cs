using System;
using System.Windows;

namespace WinCraft.UI
{
    public static class LayoutTokens
    {
        public static readonly DependencyProperty SpacingProperty =
            DependencyProperty.RegisterAttached(
                "Spacing",
                typeof(Spacing),
                typeof(LayoutTokens),
                new FrameworkPropertyMetadata(Spacing.None, OnSpacingChanged),
                ValidateSpacing);

        public static Spacing GetSpacing(FrameworkElement target) =>
            (Spacing)target.GetValue(SpacingProperty);

        public static void SetSpacing(FrameworkElement target, Spacing value) =>
            target.SetValue(SpacingProperty, value);

        private static void OnSpacingChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            switch (target)
            {
                case SpacingPanel panel:
                    panel.ApplySpacing((Spacing)e.NewValue);
                    break;
                case SpacingGrid grid:
                    grid.ApplySpacing((Spacing)e.NewValue);
                    break;
                case SpacingItemsControl itemsControl:
                    itemsControl.ApplySpacing((Spacing)e.NewValue);
                    break;
                case Form form:
                    form.ApplySpacing((Spacing)e.NewValue);
                    break;
                case VirtualizingSpacingStackPanel virtualizingStackPanel:
                    virtualizingStackPanel.ApplySpacing((Spacing)e.NewValue);
                    break;
                case VirtualizingSpacingWrapPanel virtualizingWrapPanel:
                    virtualizingWrapPanel.ApplySpacing((Spacing)e.NewValue);
                    break;
            }
        }

        private static bool ValidateSpacing(object value) =>
            Enum.IsDefined(typeof(Spacing), value);
    }
}
