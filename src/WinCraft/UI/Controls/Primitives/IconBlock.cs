using System.Windows;
using System.Windows.Controls;
using WinCraft.Compatibility;

namespace WinCraft.UI
{
    /// <summary>
    /// A <see cref="TextBlock"/> that always uses the icon font family,
    /// for rendering icon glyphs without setting <c>FontFamily</c> manually.
    /// </summary>
    public class IconBlock : TextBlock
    {
        private IconGlyph _icon = IconGlyph.None;
        private bool _isFilled;

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(
                nameof(Icon),
                typeof(IconGlyph),
                typeof(IconBlock),
                new FrameworkPropertyMetadata(IconGlyph.None, OnIconChanged));

        static IconBlock()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(IconBlock),
                new FrameworkPropertyMetadata(typeof(IconBlock)));
        }

        public IconBlock()
        {
            TextAlignment = TextAlignment.Center;
            TextOptionsCompat.ApplyIdealTextRendering(this);
            ApplyIsIconFilled(Design.GetIsIconFilled(this));
        }

        public IconGlyph Icon
        {
            get => (IconGlyph)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        internal void ApplyIsIconFilled(bool isFilled)
        {
            _isFilled = isFilled;
            UpdateGlyphText();
        }

        private static void OnIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var iconBlock = (IconBlock)d;
            iconBlock._icon = (IconGlyph)e.NewValue;
            iconBlock.UpdateGlyphText();
        }

        private void UpdateGlyphText()
        {
            var codePoint = (int)_icon;
            var fontFamilyKey = "IconFontFamily";
            if (_isFilled &&
                IconGlyphMap.RegularToFilled.TryGetValue(codePoint, out int filledCodePoint))
            {
                codePoint = filledCodePoint;
                fontFamilyKey = "IconFontFamilyFilled";
            }

            SetResourceReference(FontFamilyProperty, fontFamilyKey);
            Text = _icon == IconGlyph.None ? string.Empty : char.ConvertFromUtf32(codePoint);
        }
    }
}
