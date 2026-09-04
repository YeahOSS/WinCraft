using System;
using System.Windows;
using System.Windows.Controls;

namespace WinCraft.UI
{
    /// <summary>
    /// Icon-only button.  Automatically square with a centered icon;
    /// set <see cref="IsCircular"/> to render a perfect circle.
    /// </summary>
    public class IconButton : Button
    {
        public static readonly DependencyProperty IsCircularProperty =
            DependencyProperty.Register(
                nameof(IsCircular),
                typeof(bool),
                typeof(IconButton),
                new PropertyMetadata(false));

        /// <summary>
        /// When <c>true</c> the button shrinks to 24×24 (chrome-friendly);
        /// the default <c>false</c> keeps the standard 32×32 size.
        /// </summary>
        public static readonly DependencyProperty IsSmallProperty =
            DependencyProperty.Register(
                nameof(IsSmall),
                typeof(bool),
                typeof(IconButton),
                new PropertyMetadata(false));

        public static readonly DependencyProperty IconProperty =
            Design.IconProperty.AddOwner(
                typeof(IconButton),
                new FrameworkPropertyMetadata(IconGlyph.None));

        public bool IsCircular
        {
            get => (bool)GetValue(IsCircularProperty);
            set => SetValue(IsCircularProperty, value);
        }

        public bool IsSmall
        {
            get => (bool)GetValue(IsSmallProperty);
            set => SetValue(IsSmallProperty, value);
        }

        public IconGlyph Icon
        {
            get => (IconGlyph)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        // ── Hidden at compile time via [Obsolete(error:true)] ──

        [Obsolete("IconButton does not support Content.  Use the Icon property instead.", true)]
        public new object Content
        {
            get => base.Content;
            set => base.Content = value;
        }

        [Obsolete("IconButton does not support ContentTemplate.  Use the Icon property instead.", true)]
        public new DataTemplate ContentTemplate
        {
            get => base.ContentTemplate;
            set => base.ContentTemplate = value;
        }

        [Obsolete("IconButton does not support ContentTemplateSelector.", true)]
        public new DataTemplateSelector ContentTemplateSelector
        {
            get => base.ContentTemplateSelector;
            set => base.ContentTemplateSelector = value;
        }

        [Obsolete("IconButton does not support ContentStringFormat.", true)]
        public new string ContentStringFormat
        {
            get => base.ContentStringFormat;
            set => base.ContentStringFormat = value;
        }

        static IconButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(IconButton),
                new FrameworkPropertyMetadata(typeof(IconButton)));
        }
    }
}
