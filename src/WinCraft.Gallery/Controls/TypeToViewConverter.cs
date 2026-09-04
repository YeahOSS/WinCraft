using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;

namespace WinCraft.Gallery.Controls
{
    /// <summary>
    /// Creates a <see cref="UserControl"/> from a <see cref="Type"/> via
    /// <see cref="Activator.CreateInstance(Type)"/>.  Returns the value
    /// unchanged when it is not a Type.  Use inline in XAML.
    /// </summary>
    public sealed class TypeToViewConverter : MarkupExtension, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Type type && typeof(UserControl).IsAssignableFrom(type))
                return Activator.CreateInstance(type);

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;

        public override object ProvideValue(IServiceProvider serviceProvider) => this;
    }
}
