using System.Windows;
using System.Windows.Controls;

namespace WinCraft.UI
{
    /// <summary>
    /// Attached properties that track whether a <see cref="ContextMenu"/> is
    /// open on any descendant of the element.  ContextMenu is a separate popup
    /// window with no visual-tree relationship to its owner, so standard
    /// properties like <c>IsMouseOver</c>, <c>IsMouseCaptureWithin</c>, and
    /// <c>IsKeyboardFocused</c> all report <c>false</c> the moment the menu
    /// opens.  Set <see cref="TrackContextMenuProperty"/> to <c>true</c> and
    /// then use <see cref="IsContextMenuOpenWithinProperty"/> in a
    /// <c>Trigger</c> to keep focus / hover visuals active while the menu is
    /// shown.
    /// </summary>
    public static class ContextMenuTracker
    {
        /// <summary>
        /// Read-only attached property: true while a ContextMenu is open on
        /// any descendant of the element.
        /// </summary>
        public static readonly DependencyProperty IsContextMenuOpenWithinProperty =
            DependencyProperty.RegisterAttached(
                "IsContextMenuOpenWithin",
                typeof(bool),
                typeof(ContextMenuTracker),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        public static bool GetIsContextMenuOpenWithin(DependencyObject d) =>
            (bool)d.GetValue(IsContextMenuOpenWithinProperty);

        public static void SetIsContextMenuOpenWithin(DependencyObject d, bool value) =>
            d.SetValue(IsContextMenuOpenWithinProperty, value);

        /// <summary>
        /// Set to <c>true</c> on a <see cref="UIElement"/> to track context
        /// menu open/close events bubbling from its descendants.  Enables
        /// <see cref="IsContextMenuOpenWithinProperty"/>.
        /// </summary>
        public static readonly DependencyProperty TrackContextMenuProperty =
            DependencyProperty.RegisterAttached(
                "TrackContextMenu",
                typeof(bool),
                typeof(ContextMenuTracker),
                new PropertyMetadata(false, OnTrackContextMenuChanged));

        public static bool GetTrackContextMenu(DependencyObject d) =>
            (bool)d.GetValue(TrackContextMenuProperty);

        public static void SetTrackContextMenu(DependencyObject d, bool value) =>
            d.SetValue(TrackContextMenuProperty, value);

        private static readonly ContextMenuEventHandler OpeningHandler = OnContextMenuOpening;
        private static readonly ContextMenuEventHandler ClosingHandler = OnContextMenuClosing;

        private static void OnTrackContextMenuChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not UIElement element) return;

            if ((bool)e.NewValue)
            {
                element.AddHandler(
                    FrameworkElement.ContextMenuOpeningEvent,
                    OpeningHandler,
                    handledEventsToo: true);
                element.AddHandler(
                    FrameworkElement.ContextMenuClosingEvent,
                    ClosingHandler,
                    handledEventsToo: true);
            }
            else
            {
                element.RemoveHandler(
                    FrameworkElement.ContextMenuOpeningEvent,
                    OpeningHandler);
                element.RemoveHandler(
                    FrameworkElement.ContextMenuClosingEvent,
                    ClosingHandler);
            }
        }

        private static void OnContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            SetIsContextMenuOpenWithin((DependencyObject)sender, true);
        }

        private static void OnContextMenuClosing(object sender, ContextMenuEventArgs e)
        {
            SetIsContextMenuOpenWithin((DependencyObject)sender, false);
        }
    }
}
