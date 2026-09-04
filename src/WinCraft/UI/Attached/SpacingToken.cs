using System;
using System.Windows;

namespace WinCraft.UI
{
    internal static class SpacingToken
    {
        public static void Apply(
            FrameworkElement target,
            Spacing spacing,
            params DependencyProperty[] properties)
        {
            var resourceKey = spacing switch
            {
                Spacing.None => null,
                Spacing.XSmall => "SpacingXSmall",
                Spacing.Small => "SpacingSmall",
                Spacing.Normal => "SpacingNormal",
                Spacing.Large => "SpacingLarge",
                Spacing.XLarge => "SpacingXLarge",
                _ => throw new ArgumentOutOfRangeException(nameof(spacing))
            };

            if (resourceKey == null)
            {
                foreach (var property in properties)
                    target.ClearValue(property);
            }
            else
            {
                foreach (var property in properties)
                    target.SetResourceReference(property, resourceKey);
            }
        }
    }
}
