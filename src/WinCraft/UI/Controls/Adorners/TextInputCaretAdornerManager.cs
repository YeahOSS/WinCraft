using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Threading;

namespace WinCraft.UI
{
    internal static class TextInputCaretAdornerManager
    {
        private static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(TextInputCaretAdornerManager),
                new PropertyMetadata(false));

        private static readonly DependencyProperty AdornerProperty =
            DependencyProperty.RegisterAttached(
                nameof(Adorner),
                typeof(TextInputCaretAdorner),
                typeof(TextInputCaretAdornerManager),
                new PropertyMetadata(null));

        public static void SetEnabled(Control textInput, bool isEnabled)
        {
            if (!isEnabled)
            {
                textInput.Loaded -= OnTextInputLoaded;
                textInput.Unloaded -= OnTextInputUnloaded;
                textInput.SetValue(IsEnabledProperty, false);
                RemoveAdorner(textInput);
                return;
            }

            if (!(bool)textInput.GetValue(IsEnabledProperty))
            {
                textInput.SetValue(IsEnabledProperty, true);
                textInput.Loaded += OnTextInputLoaded;
                textInput.Unloaded += OnTextInputUnloaded;
            }

            AttachAdorner(textInput);
        }

        private static void OnTextInputLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is Control textInput && (bool)textInput.GetValue(IsEnabledProperty))
                AttachAdorner(textInput);
        }

        private static void OnTextInputUnloaded(object sender, RoutedEventArgs e)
        {
            if (sender is Control textInput)
                RemoveAdorner(textInput);
        }

        private static void AttachAdorner(Control textInput)
        {
            if (!textInput.IsLoaded ||
                !(bool)textInput.GetValue(IsEnabledProperty) ||
                GetAdorner(textInput) != null)
                return;

            var layer = AdornerLayer.GetAdornerLayer(textInput);
            if (layer == null)
            {
                textInput.Dispatcher.BeginInvoke(
                    DispatcherPriority.Loaded,
                    new Action(() => AttachAdorner(textInput)));
                return;
            }

            var adorner = new TextInputCaretAdorner(textInput);
            adorner.SetResourceReference(TextInputCaretAdorner.CaretBrushProperty, "TextDefault");
            SetAdorner(textInput, adorner);
            layer.Add(adorner);
        }

        private static void RemoveAdorner(Control textInput)
        {
            var adorner = GetAdorner(textInput);
            if (adorner == null)
                return;

            AdornerLayer.GetAdornerLayer(textInput)?.Remove(adorner);
            adorner.Detach();
            SetAdorner(textInput, null);
        }

        private static TextInputCaretAdorner GetAdorner(DependencyObject obj) =>
            (TextInputCaretAdorner)obj.GetValue(AdornerProperty);

        private static void SetAdorner(DependencyObject obj, TextInputCaretAdorner value) =>
            obj.SetValue(AdornerProperty, value);
    }
}
