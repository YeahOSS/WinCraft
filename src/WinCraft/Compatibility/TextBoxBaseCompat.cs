using System.Windows;
using System.Reflection;
using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using System.Windows.Media;
using WinCraft.Infrastructure;

namespace WinCraft.Compatibility
{
    /// <summary>
    /// Applies text-input visual brushes when the runtime exposes the corresponding WPF properties.
    /// </summary>
    public static class TextBoxBaseCompat
    {
        private static readonly PropertyInfo TextBoxBaseCaretBrushProperty =
            typeof(TextBoxBase).GetProperty("CaretBrush");

        private static readonly PropertyInfo TextBoxBaseSelectionBrushProperty =
            typeof(TextBoxBase).GetProperty("SelectionBrush");

        private static readonly PropertyInfo TextBoxBaseSelectionOpacityProperty =
            typeof(TextBoxBase).GetProperty("SelectionOpacity");

        private static readonly PropertyInfo TextBoxBaseSelectionTextBrushProperty =
            typeof(TextBoxBase).GetProperty("SelectionTextBrush");

        private static readonly PropertyInfo PasswordBoxCaretBrushProperty =
            typeof(PasswordBox).GetProperty("CaretBrush");

        private static readonly PropertyInfo PasswordBoxSelectionBrushProperty =
            typeof(PasswordBox).GetProperty("SelectionBrush");

        private static readonly PropertyInfo PasswordBoxSelectionOpacityProperty =
            typeof(PasswordBox).GetProperty("SelectionOpacity");

        private static readonly PropertyInfo PasswordBoxSelectionTextBrushProperty =
            typeof(PasswordBox).GetProperty("SelectionTextBrush");

        public static void EnableNonAdornerSelectionRendering()
        {
            typeof(object).Assembly.TryInvokeStaticMethod(
                "System.AppContext", "SetSwitch",
                "Switch.System.Windows.Controls.Text.UseAdornerForTextboxSelectionRendering", false);
        }

        public static bool SetTextInputBrushes(
            DependencyObject textInput,
            Brush caretBrush,
            Brush selectionBrush,
            Brush selectionTextBrush)
        {
            if (textInput is TextBoxBase)
            {
                return SetBrushes(
                    textInput,
                    TextBoxBaseCaretBrushProperty,
                    TextBoxBaseSelectionBrushProperty,
                    TextBoxBaseSelectionOpacityProperty,
                    TextBoxBaseSelectionTextBrushProperty,
                    caretBrush,
                    selectionBrush,
                    selectionTextBrush);
            }

            if (textInput is PasswordBox)
            {
                return SetBrushes(
                    textInput,
                    PasswordBoxCaretBrushProperty,
                    PasswordBoxSelectionBrushProperty,
                    PasswordBoxSelectionOpacityProperty,
                    PasswordBoxSelectionTextBrushProperty,
                    caretBrush,
                    selectionBrush,
                    selectionTextBrush);
            }

            return false;
        }

        private static bool SetBrushes(
            DependencyObject textInput,
            PropertyInfo caretBrushProperty,
            PropertyInfo selectionBrushProperty,
            PropertyInfo selectionOpacityProperty,
            PropertyInfo selectionTextBrushProperty,
            Brush caretBrush,
            Brush selectionBrush,
            Brush selectionTextBrush)
        {
            if (caretBrushProperty == null)
                return false;

            caretBrushProperty.SetValue(textInput, caretBrush, null);
            selectionBrushProperty?.SetValue(textInput, selectionBrush, null);
            selectionOpacityProperty?.SetValue(textInput, 1.0, null);
            selectionTextBrushProperty?.SetValue(textInput, selectionTextBrush, null);
            return true;
        }
    }
}
