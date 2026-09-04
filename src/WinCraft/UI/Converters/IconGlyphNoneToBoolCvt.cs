using System.Globalization;

namespace WinCraft.UI
{
    /// <summary>
    /// Converts an <see cref="IconGlyph"/> value to <see cref="bool"/>:
    /// <c>true</c> when the value is not <see cref="IconGlyph.None"/>.
    /// </summary>
    public sealed class IconGlyphNoneToBoolCvt : ValueConverterBase
    {
        public override object Convert(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            return value is IconGlyph glyph && glyph != IconGlyph.None;
        }
    }
}
