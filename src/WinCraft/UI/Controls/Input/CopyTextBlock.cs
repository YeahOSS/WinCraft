using System.Windows;
using System.Windows.Controls;

namespace WinCraft.UI
{
    /// <summary>
    /// A borderless, selectable text control based on <see cref="TextBox"/>.
    /// Designed for display-only text that users can select and copy.
    /// Uses the implicit <c>CopyTextBlock</c> style by default and is excluded
    /// from the self-bordering focus check so the global focus adorner applies.
    /// </summary>
    public class CopyTextBlock : TextBox
    {
        static CopyTextBlock()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(CopyTextBlock),
                new FrameworkPropertyMetadata(typeof(CopyTextBlock)));
        }
    }
}
