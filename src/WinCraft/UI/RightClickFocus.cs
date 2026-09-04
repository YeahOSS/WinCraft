using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WinCraft.UI
{
    /// <summary>
    /// Moves keyboard focus to controls on right-click, matching the Win32 EDIT
    /// and Windows Explorer behavior.  WPF never transfers keyboard focus on the
    /// right mouse button, so a previously focused control keeps its focus border
    /// while the user opens another control's context menu and receives focus
    /// back when the menu closes.
    /// </summary>
    internal static class RightClickFocus
    {
        private static bool _isRegistered;

        public static void Register()
        {
            if (_isRegistered)
                return;

            EventManager.RegisterClassHandler(
                typeof(Control),
                UIElement.PreviewMouseRightButtonDownEvent,
                new MouseButtonEventHandler(OnPreviewMouseRightButtonDown));
            _isRegistered = true;
        }

        private static void OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Control control &&
                control.Focusable &&
                control.IsEnabled &&
                !control.IsKeyboardFocusWithin)
            {
                control.Focus();
            }
        }
    }
}
