using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Markup;

namespace WinCraft.UI
{
    public abstract class MultiValueConverterBase : MarkupExtension, IMultiValueConverter
    {
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        public abstract object Convert(object[] values, Type targetType, object parameter, CultureInfo culture);

        public virtual object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return targetTypes.Select(_ => Binding.DoNothing).ToArray();
        }
    }
}
