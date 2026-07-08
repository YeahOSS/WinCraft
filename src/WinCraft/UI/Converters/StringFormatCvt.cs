using System;
using System.Globalization;
using System.Linq;

namespace WinCraft.UI
{
    public sealed class StringFormatCvt : MultiValueConverterBase
    {
        public override object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is not string format)
                return string.Join(" ", values.Select(v => v?.ToString() ?? "").ToArray());

            return string.Format(culture, format, values);
        }
    }
}
