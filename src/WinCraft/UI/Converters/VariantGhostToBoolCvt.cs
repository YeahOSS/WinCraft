using System.Globalization;

namespace WinCraft.UI
{
    /// <summary>
    /// Converts a <see cref="ControlVariant"/> value to <see cref="bool"/>:
    /// <c>true</c> when the value is <see cref="ControlVariant.Ghost"/>.
    /// </summary>
    public sealed class VariantGhostToBoolCvt : ValueConverterBase
    {
        public override object Convert(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            return value is ControlVariant variant && variant == ControlVariant.Ghost;
        }
    }
}
