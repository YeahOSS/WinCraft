using System;
using System.Globalization;
using System.Windows.Data;

namespace WinCraft.UI
{
    public sealed class ConditionalCvt : MultiValueConverterBase
    {
        public override object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 3)
                return Binding.DoNothing;

            return values[0] is true ? values[1] : values[2];
        }
    }
}
