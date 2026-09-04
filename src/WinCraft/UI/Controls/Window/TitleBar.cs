using System.Windows;
using System.Windows.Controls;

namespace WinCraft.UI
{
    /// <summary>
    /// Standalone title bar hosted by the <see cref="ChromeWindow"/> template.
    /// Icon, window title, and caption buttons are fixed template parts;
    /// <see cref="Header"/>, <see cref="Content"/>, and <see cref="Footer"/>
    /// are empty extension slots.  When <see cref="Header"/> is null the
    /// window title is shown in its place.
    /// </summary>
    public class TitleBar : Control
    {
        /// <summary>Slot after the icon.  Replaces the default window title when set.</summary>
        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register(nameof(Header), typeof(object), typeof(TitleBar),
                new PropertyMetadata(null));

        /// <summary>Slot centered relative to the full title-bar width.</summary>
        public static readonly DependencyProperty ContentProperty =
            DependencyProperty.Register(nameof(Content), typeof(object), typeof(TitleBar),
                new PropertyMetadata(null));

        /// <summary>Slot before the caption buttons.</summary>
        public static readonly DependencyProperty FooterProperty =
            DependencyProperty.Register(nameof(Footer), typeof(object), typeof(TitleBar),
                new PropertyMetadata(null));

        public object Header
        {
            get => GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        public object Content
        {
            get => GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }

        public object Footer
        {
            get => GetValue(FooterProperty);
            set => SetValue(FooterProperty, value);
        }

        static TitleBar()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(TitleBar),
                new FrameworkPropertyMetadata(typeof(TitleBar)));
        }
    }
}
