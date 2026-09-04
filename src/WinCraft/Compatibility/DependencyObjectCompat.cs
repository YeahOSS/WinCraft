using System.Windows;

namespace WinCraft.Compatibility
{
    /// <summary>
    /// Sets a dependency property value without overwriting lower-precedence sources
    /// (styles, animations, etc.) on .NET 4.5+, where <see cref="DependencyObject.SetCurrentValue"/>
    /// is available. Falls back to <see cref="DependencyObject.SetValue"/> on earlier targets.
    /// </summary>
    internal static class DependencyObjectCompat
    {
        internal static void SetControlValue(DependencyObject d, DependencyProperty property, object value)
        {
#if NET45
            d.SetCurrentValue(property, value);
#else
            d.SetValue(property, value);
#endif
        }
    }
}
