using System;
using System.Globalization;
using System.Windows.Data;

namespace WinCraft.UI
{
    public sealed class StringEqualsToBoolCvt : ValueConverterBase
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.Equals(value?.ToString(), parameter?.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is true ? parameter : Binding.DoNothing;
        }
    }
}
