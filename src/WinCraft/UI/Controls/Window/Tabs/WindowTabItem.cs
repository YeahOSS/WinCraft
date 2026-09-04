using System.Windows;
using System.Windows.Controls;

namespace WinCraft.UI
{
    public sealed class WindowTabItem : ListBoxItem
    {
        private static readonly DependencyPropertyKey IsDraggingPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(IsDragging),
                typeof(bool),
                typeof(WindowTabItem),
                new PropertyMetadata(false));

        public static readonly DependencyProperty IsDraggingProperty =
            IsDraggingPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey IsCompactPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(IsCompact),
                typeof(bool),
                typeof(WindowTabItem),
                new PropertyMetadata(false));

        public static readonly DependencyProperty IsCompactProperty =
            IsCompactPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey IsHomeTabPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(IsHomeTab),
                typeof(bool),
                typeof(WindowTabItem),
                new PropertyMetadata(false));

        public static readonly DependencyProperty IsHomeTabProperty =
            IsHomeTabPropertyKey.DependencyProperty;

        static WindowTabItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(WindowTabItem),
                new FrameworkPropertyMetadata(typeof(WindowTabItem)));
        }

        public bool IsDragging => (bool)GetValue(IsDraggingProperty);

        public bool IsCompact => (bool)GetValue(IsCompactProperty);

        public bool IsHomeTab => (bool)GetValue(IsHomeTabProperty);

        internal void SetIsDragging(bool value) =>
            SetValue(IsDraggingPropertyKey, value);

        internal void SetIsCompact(bool value) =>
            SetValue(IsCompactPropertyKey, value);

        internal void SetIsHomeTab(bool value) =>
            SetValue(IsHomeTabPropertyKey, value);
    }
}
