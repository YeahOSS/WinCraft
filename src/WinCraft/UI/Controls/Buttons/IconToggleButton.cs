using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace WinCraft.UI
{
    /// <summary>
    /// Icon-only toggle button. Supports two toggle modes:
    /// <list type="bullet">
    /// <item>Set <see cref="CheckedIcon"/> to swap to a different icon when checked
    /// (e.g. Eye → EyeOff for password reveal).</item>
    /// <item>Leave <see cref="CheckedIcon"/> unset and use <see cref="IsFilledOnChecked"/>
    /// (default <c>true</c>) to switch between Regular and Filled glyph variants
    /// (e.g. Heart outline → Heart filled for favorites).</item>
    /// </list>
    /// </summary>
    public class IconToggleButton : ToggleButton
    {
        public static readonly DependencyProperty IsCircularProperty =
            DependencyProperty.Register(
                nameof(IsCircular),
                typeof(bool),
                typeof(IconToggleButton),
                new PropertyMetadata(false));

        /// <summary>
        /// When <c>true</c> the button shrinks to 24×24 (chrome-friendly);
        /// the default <c>false</c> keeps the standard 32×32 size.
        /// </summary>
        public static readonly DependencyProperty IsSmallProperty =
            DependencyProperty.Register(
                nameof(IsSmall),
                typeof(bool),
                typeof(IconToggleButton),
                new PropertyMetadata(false));

        public static readonly DependencyProperty IconProperty =
            Design.IconProperty.AddOwner(
                typeof(IconToggleButton),
                new FrameworkPropertyMetadata(IconGlyph.None));

        /// <summary>
        /// The icon displayed when <see cref="ToggleButton.IsChecked"/> is <c>true</c>.
        /// When <see cref="IconGlyph.None"/> (the default), the control falls back to
        /// <see cref="IsFilledOnChecked"/> to determine the checked icon appearance.
        /// </summary>
        public static readonly DependencyProperty CheckedIconProperty =
            DependencyProperty.Register(
                nameof(CheckedIcon),
                typeof(IconGlyph),
                typeof(IconToggleButton),
                new FrameworkPropertyMetadata(IconGlyph.None));

        /// <summary>
        /// When <c>true</c> and <see cref="CheckedIcon"/> is <see cref="IconGlyph.None"/>,
        /// the icon switches to its Filled glyph variant when checked.
        /// Default is <c>true</c>.
        /// </summary>
        public static readonly DependencyProperty IsFilledOnCheckedProperty =
            DependencyProperty.Register(
                nameof(IsFilledOnChecked),
                typeof(bool),
                typeof(IconToggleButton),
                new PropertyMetadata(true));

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

        public IconGlyph CheckedIcon
        {
            get => (IconGlyph)GetValue(CheckedIconProperty);
            set => SetValue(CheckedIconProperty, value);
        }

        public bool IsFilledOnChecked
        {
            get => (bool)GetValue(IsFilledOnCheckedProperty);
            set => SetValue(IsFilledOnCheckedProperty, value);
        }

        // ── Hidden at compile time via [Obsolete(error:true)] ──

        [Obsolete("IconToggleButton does not support Content.  Use the Icon property instead.", true)]
        public new object Content
        {
            get => base.Content;
            set => base.Content = value;
        }

        [Obsolete("IconToggleButton does not support ContentTemplate.  Use the Icon property instead.", true)]
        public new DataTemplate ContentTemplate
        {
            get => base.ContentTemplate;
            set => base.ContentTemplate = value;
        }

        [Obsolete("IconToggleButton does not support ContentTemplateSelector.", true)]
        public new DataTemplateSelector ContentTemplateSelector
        {
            get => base.ContentTemplateSelector;
            set => base.ContentTemplateSelector = value;
        }

        [Obsolete("IconToggleButton does not support ContentStringFormat.", true)]
        public new string ContentStringFormat
        {
            get => base.ContentStringFormat;
            set => base.ContentStringFormat = value;
        }

        static IconToggleButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(IconToggleButton),
                new FrameworkPropertyMetadata(typeof(IconToggleButton)));
        }
    }
}
