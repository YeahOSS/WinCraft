using System.Windows;
using System.Windows.Controls;

namespace WinCraft.UI
{
    /// <summary>
    /// A single row in a <see cref="Form"/> layout, pairing a header label
    /// with an input control.  Use <see cref="HeaderedContentControl.Header"/>
    /// (inherited) for the label text and <see cref="ContentControl.Content"/>
    /// for the control.
    /// </summary>
    public class FormItem : HeaderedContentControl
    {
        public static readonly DependencyProperty IsRequiredProperty =
            DependencyProperty.Register(
                nameof(IsRequired),
                typeof(bool),
                typeof(FormItem),
                new PropertyMetadata(false));

        public bool IsRequired
        {
            get => (bool)GetValue(IsRequiredProperty);
            set => SetValue(IsRequiredProperty, value);
        }

        static FormItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(FormItem),
                new FrameworkPropertyMetadata(typeof(FormItem)));
        }
    }
}
