using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace WinCraft.UI
{
    /// <summary>
    /// Attaches to a <see cref="Window"/> and manages a single
    /// <see cref="FocusAdorner"/> that follows the keyboard-focused element.
    /// Replaces per-template focus-ring markup with one window-level decorator.
    /// </summary>
    public static class FocusAdornerManager
    {
        /// <summary>
        /// Enable window-level focus adornment for <paramref name="window"/>.
        /// Call once per window (typically from the constructor or OnLoaded).
        /// </summary>
        public static void Attach(Window window)
        {
            window.AddHandler(
                UIElement.GotKeyboardFocusEvent,
                (KeyboardFocusChangedEventHandler)OnGotKeyboardFocus,
                handledEventsToo: true);

            window.AddHandler(
                UIElement.LostKeyboardFocusEvent,
                (KeyboardFocusChangedEventHandler)OnLostKeyboardFocus,
                handledEventsToo: true);
        }

        private static readonly DependencyProperty AdornerProperty =
            DependencyProperty.RegisterAttached(
                nameof(Adorner),
                typeof(FocusAdorner),
                typeof(FocusAdornerManager),
                new PropertyMetadata(null));

        private static void OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (!KeyboardFocus.LastInputWasKeyboard)
                return;

            if (e.NewFocus is UIElement focused && focused.IsVisible)
            {
                RemoveExisting(focused);
                if (!HasSelfBorderingFocus(focused))
                    AttachAdorner(focused);
            }
        }

        private static void OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (e.OldFocus is UIElement oldFocus)
                RemoveExisting(oldFocus);
        }

        private static void AttachAdorner(UIElement element)
        {
            var layer = AdornerLayer.GetAdornerLayer(element);
            if (layer == null)
                return;

            var adorner = new FocusAdorner(element);
            adorner.SetBrushKey(GetBrushKey(element));
            SetAdorner(element, adorner);
            layer.Add(adorner);
        }

        /// <summary>
        /// Choose a contrasting brush so the focus ring stays visible:
        ///   Accent-filled or primary linear controls → FocusRingOnAccent (white)
        ///   Everything else → FocusRing (semi-transparent accent)
        /// </summary>
        internal static string GetBrushKey(UIElement element)
        {
            var role = Design.GetVisualRole(element);
            var variant = Design.GetVariant(element);

            // Accent-filled controls and primary linear controls already use the
            // accent as their surface or outline, so use a contrasting ring.
            if (variant == ControlVariant.Solid
                || (variant == ControlVariant.Outline && role == VisualRole.Primary))
            {
                return "FocusRingOnAccent";
            }

            return "FocusRing";
        }

        /// <summary>
        /// Returns true when <paramref name="element"/> (or a visual-tree
        /// ancestor) already draws its own focus border, making the adorner
        /// redundant.
        /// </summary>
        private static bool HasSelfBorderingFocus(DependencyObject element)
        {
            for (var current = element; current != null; current = VisualTreeHelper.GetParent(current))
            {
                // CopyTextBlock has no border template — it needs the adorner.
                if (current is CopyTextBlock)
                    return false;

                if (current is TextBox
                    || current is NumericBox
                    || current is PasswordBox
                    || current is ComboBox
                    || current is ColorPicker)
                    return true;
            }

            return false;
        }

        private static void RemoveExisting(UIElement element)
        {
            var adorner = GetAdorner(element);
            if (adorner == null)
                return;

            var layer = AdornerLayer.GetAdornerLayer(element);
            layer?.Remove(adorner);
            adorner.Detach();
            SetAdorner(element, null);
        }

        private static FocusAdorner GetAdorner(DependencyObject obj) =>
            (FocusAdorner)obj.GetValue(AdornerProperty);

        private static void SetAdorner(DependencyObject obj, FocusAdorner value) =>
            obj.SetValue(AdornerProperty, value);
    }
}
