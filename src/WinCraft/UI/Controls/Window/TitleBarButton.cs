using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WinCraft.UI
{
    /// <summary>Button used by a <see cref="TitleBar"/> template.</summary>
    public class TitleBarButton : Button
    {
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(nameof(Icon), typeof(IconGlyph), typeof(TitleBarButton),
                new PropertyMetadata(IconGlyph.Subtract16));

        public IconGlyph Icon
        {
            get => (IconGlyph)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        static TitleBarButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(TitleBarButton),
                new FrameworkPropertyMetadata(typeof(TitleBarButton)));
        }
    }
}
