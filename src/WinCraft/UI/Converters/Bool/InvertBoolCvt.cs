using System;
using System.Globalization;
using System.Windows.Data;

namespace WinCraft.UI
{
    public sealed class InvertBoolCvt : ValueConverterBase
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool b ? !b : Binding.DoNothing;
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Convert(value, targetType, parameter, culture);
        }
    }
}
