using System.Windows;

#if NET45
using System.Windows.Media;
#endif

namespace WinCraft.Compatibility
{
    /// <summary>
    /// Applies text rendering settings available only on .NET 4.5+.
    /// </summary>
    internal static class TextOptionsCompat
    {
        internal static void ApplyIdealTextRendering(DependencyObject element)
        {
#if NET45
            TextOptions.SetTextFormattingMode(element, TextFormattingMode.Ideal);
            TextOptions.SetTextRenderingMode(element, TextRenderingMode.Grayscale);
#endif
        }
    }
}
