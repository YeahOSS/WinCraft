using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WinCraft.UI
{
    /// <summary>
    /// ComboBox-style dropdown popup behavior: mouse capture, outside-click
    /// dismissal, window-deactivation closing, Escape-key handling, and
    /// wheel-event isolation while the dropdown is open.
    /// </summary>
    public sealed class DropdownHandler(Control owner, Func<bool> getIsOpen, Action<bool> setIsOpen)
    {
        private readonly Control _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        private readonly Func<bool> _getIsOpen = getIsOpen ?? throw new ArgumentNullException(nameof(getIsOpen));
        private readonly Action<bool> _setIsOpen = setIsOpen ?? throw new ArgumentNullException(nameof(setIsOpen));

        public void OnOpened()
        {
            _owner.AddHandler(
                UIElement.MouseWheelEvent,
                (MouseWheelEventHandler)OnOwnerMouseWheel);
            _owner.AddHandler(
                Mouse.PreviewMouseDownOutsideCapturedElementEvent,
                (MouseButtonEventHandler)OnOutsideClick,
                true);
            _owner.AddHandler(
                UIElement.LostMouseCaptureEvent,
                new MouseEventHandler(OnLostCapture),
                true);
            BeginCapture();
        }

        public void OnClosed()
        {
            _owner.RemoveHandler(
                UIElement.MouseWheelEvent,
                (MouseWheelEventHandler)OnOwnerMouseWheel);
            _owner.RemoveHandler(
                Mouse.PreviewMouseDownOutsideCapturedElementEvent,
                (MouseButtonEventHandler)OnOutsideClick);
            _owner.RemoveHandler(
                UIElement.LostMouseCaptureEvent,
                new MouseEventHandler(OnLostCapture));
            _owner.ReleaseMouseCapture();
        }

        public bool HandleKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape && _getIsOpen())
            {
                _setIsOpen(false);
                e.Handled = true;
                return true;
            }
            return false;
        }

        public void HandleKeyboardFocusChanged(bool isFocusWithin)
        {
            if (_getIsOpen() && !isFocusWithin)
                _setIsOpen(false);
        }

        private void BeginCapture()
        {
            // Defer so any click that opened the dropdown completes first.
            _owner.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_getIsOpen() && Mouse.Captured == null)
                    Mouse.Capture(_owner, CaptureMode.SubTree);
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void OnOutsideClick(object sender, MouseButtonEventArgs e)
        {
            _setIsOpen(false);
        }

        private void OnOwnerMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // While the dropdown is open we hold subtree mouse capture, so WPF
            // reroutes wheel events raised anywhere over the window to the
            // owner instead of the element under the cursor. Swallow them or
            // they bubble from the owner into the page behind the flyout and
            // scroll it. Wheel events consumed inside the flyout are already
            // marked handled and never reach this handler.
            e.Handled = true;
        }

        private void OnLostCapture(object sender, MouseEventArgs e)
        {
            if (!_getIsOpen()) return;

            if (Mouse.Captured != _owner)
            {
                if (e.OriginalSource == _owner)
                {
                    if (Mouse.Captured == null
                        || !_owner.IsDescendant(Mouse.Captured as DependencyObject))
                    {
                        _setIsOpen(false);
                    }
                }
                else if (_owner.IsDescendant(e.OriginalSource as DependencyObject))
                {
                    // A child released capture — re-capture to keep
                    // outside-click detection active.
                    if (Mouse.Captured == null)
                    {
                        BeginCapture();
                        e.Handled = true;
                    }
                }
                else
                {
                    _setIsOpen(false);
                }
            }
        }
    }
}
