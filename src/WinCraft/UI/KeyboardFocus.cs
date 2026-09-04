using System.Windows;
using System.Windows.Input;

namespace WinCraft.UI
{
    public static class KeyboardFocus
    {
        public static bool LastInputWasKeyboard { get; private set; }

        static KeyboardFocus()
        {
            EventManager.RegisterClassHandler(
                typeof(UIElement),
                UIElement.PreviewKeyDownEvent,
                new KeyEventHandler((s, e) => { LastInputWasKeyboard = true; }));
            EventManager.RegisterClassHandler(
                typeof(UIElement),
                UIElement.PreviewMouseDownEvent,
                new MouseButtonEventHandler((s, e) => { LastInputWasKeyboard = false; }));
            EventManager.RegisterClassHandler(
                typeof(UIElement),
                UIElement.GotKeyboardFocusEvent,
                new KeyboardFocusChangedEventHandler((s, e) =>
                {
                    SetIsFocusVisible((DependencyObject)s, LastInputWasKeyboard);
                }));
            EventManager.RegisterClassHandler(
                typeof(UIElement),
                UIElement.LostKeyboardFocusEvent,
                new KeyboardFocusChangedEventHandler((s, e) =>
                {
                    SetIsFocusVisible((DependencyObject)s, false);
                }));
        }

        public static readonly DependencyProperty IsFocusVisibleProperty =
            DependencyProperty.RegisterAttached(
                "IsFocusVisible",
                typeof(bool),
                typeof(KeyboardFocus),
                new FrameworkPropertyMetadata(false));

        public static bool GetIsFocusVisible(DependencyObject obj) =>
            (bool)obj.GetValue(IsFocusVisibleProperty);

        public static void SetIsFocusVisible(DependencyObject obj, bool value) =>
            obj.SetValue(IsFocusVisibleProperty, value);
    }
}
