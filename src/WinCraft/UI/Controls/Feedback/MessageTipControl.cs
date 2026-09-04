using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;

namespace WinCraft.UI
{
    /// <summary>
    /// Non-modal notification control displayed as a popup at the center-top
    /// of the owner window. Auto-dismisses after a configurable delay.
    /// </summary>
    public class MessageTipControl : ContentControl
    {
        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register(
                nameof(Message),
                typeof(string),
                typeof(MessageTipControl),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty VisualRoleProperty =
            DependencyProperty.Register(
                nameof(VisualRole),
                typeof(VisualRole),
                typeof(MessageTipControl),
                new PropertyMetadata(VisualRole.Info));

        public static readonly DependencyProperty AutoHideDelayProperty =
            DependencyProperty.Register(
                nameof(AutoHideDelay),
                typeof(int),
                typeof(MessageTipControl),
                new PropertyMetadata(3000, OnAutoHideDelayChanged));

        public string Message
        {
            get => (string)GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        public VisualRole VisualRole
        {
            get => (VisualRole)GetValue(VisualRoleProperty);
            set => SetValue(VisualRoleProperty, value);
        }

        public int AutoHideDelay
        {
            get => (int)GetValue(AutoHideDelayProperty);
            set => SetValue(AutoHideDelayProperty, value);
        }

        private DispatcherTimer _timer;

        static MessageTipControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(MessageTipControl),
                new FrameworkPropertyMetadata(typeof(MessageTipControl)));
        }

        /// <summary>
        /// Show the notification as a popup positioned at the center-top of <paramref name="owner"/>.
        /// </summary>
        public void Show(Window owner)
        {
            if (string.IsNullOrEmpty(Message) || owner == null)
                return;

            var popup = new Popup
            {
                Child = this,
                AllowsTransparency = true,
                PopupAnimation = PopupAnimation.Fade,
                Placement = PlacementMode.Custom,
                PlacementTarget = owner,
                CustomPopupPlacementCallback = GetOwnerTopCenterPlacement,
                StaysOpen = false,
                IsOpen = true
            };
            TrackOwnerPosition(popup, owner);

            ScheduleAutoHide(popup);
        }

        private static CustomPopupPlacement[] GetOwnerTopCenterPlacement(
            Size popupSize,
            Size targetSize,
            Point offset)
        {
            return
            [
                new CustomPopupPlacement(
                    new Point(
                        (targetSize.Width - popupSize.Width) / 2 + offset.X,
                        targetSize.Height / 8 + offset.Y),
                    PopupPrimaryAxis.Horizontal),
            ];
        }

        private static void TrackOwnerPosition(Popup popup, Window owner)
        {
            bool useRefreshOffset = false;
            void reposition(object s, EventArgs e)
            {
                // Popup exposes no public reposition API; changing the offset
                // causes its placement pipeline to recalculate against the target.
                useRefreshOffset = !useRefreshOffset;
                popup.HorizontalOffset = useRefreshOffset ? double.Epsilon : 0;
            }
            void resize(object s, SizeChangedEventArgs e) => reposition(s, e);
            void closed(object s, EventArgs e)
            {
                owner.LocationChanged -= reposition;
                owner.SizeChanged -= resize;
                popup.Closed -= closed;
            }

            owner.LocationChanged += reposition;
            owner.SizeChanged += resize;
            popup.Closed += closed;
        }

        private void ScheduleAutoHide(Popup popup)
        {
            StopTimer();

            if (AutoHideDelay > 0)
            {
                _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(AutoHideDelay) };
                _timer.Tick += (s, e) =>
                {
                    popup.IsOpen = false;
                    StopTimer();
                };
                _timer.Start();
            }
        }

        private void StopTimer()
        {
            if (_timer == null) return;
            _timer.Stop();
            _timer = null;
        }

        private static void OnAutoHideDelayChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // The value is read when Show() is called; no runtime action needed.
        }
    }
}
