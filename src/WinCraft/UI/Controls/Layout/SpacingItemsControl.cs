using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace WinCraft.UI
{
    public class SpacingItemsControl : ItemsControl
    {
        public static readonly DependencyProperty SpacingProperty =
            SpacingPanel.SpacingProperty.AddOwner(
                typeof(SpacingItemsControl),
                new FrameworkPropertyMetadata(0.0));

        public static readonly DependencyProperty OrientationProperty =
            StackPanel.OrientationProperty.AddOwner(
                typeof(SpacingItemsControl),
                new FrameworkPropertyMetadata(Orientation.Vertical));

        public SpacingItemsControl()
        {
            ItemsPanel = CreateItemsPanel();
        }

        public double Spacing
        {
            get => (double)GetValue(SpacingProperty);
            set => SetValue(SpacingProperty, value);
        }

        public Orientation Orientation
        {
            get => (Orientation)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        internal void ApplySpacing(Spacing spacing)
        {
            SpacingToken.Apply(this, spacing, SpacingProperty);
        }

        private static ItemsPanelTemplate CreateItemsPanel()
        {
            var panel = new FrameworkElementFactory(typeof(SpacingStackPanel));
            panel.SetBinding(
                SpacingPanel.SpacingProperty,
                CreateOwnerBinding(nameof(Spacing)));
            panel.SetBinding(
                SpacingStackPanel.OrientationProperty,
                CreateOwnerBinding(nameof(Orientation)));
            return new ItemsPanelTemplate(panel);
        }

        private static Binding CreateOwnerBinding(string propertyName)
        {
            return new Binding(propertyName)
            {
                RelativeSource = new RelativeSource(
                    RelativeSourceMode.FindAncestor,
                    typeof(SpacingItemsControl),
                    1),
            };
        }
    }
}
