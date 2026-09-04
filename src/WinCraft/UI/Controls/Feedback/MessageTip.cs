namespace WinCraft.UI
{
    /// <summary>
    /// Non-modal toast notification displayed at center-top of the main window,
    /// auto-dismissed after a configurable delay.
    /// </summary>
    public static class MessageTip
    {
        private const int DefaultDelay = 3000;

        /// <summary>
        /// Show a notification message with the default <see cref="VisualRole.Info"/> style.
        /// </summary>
        public static void Show(string message)
        {
            Show(message, VisualRole.Info, DefaultDelay);
        }

        /// <summary>
        /// Show a notification message with a role-based color.
        /// Uses the default 3-second auto-dismiss.
        /// </summary>
        public static void Show(string message, VisualRole role)
        {
            Show(message, role, DefaultDelay);
        }

        /// <summary>
        /// Show a notification message.
        /// </summary>
        /// <param name="message">Message text.</param>
        /// <param name="role">Design role controlling foreground and border color.</param>
        /// <param name="autoHideDelay">
        /// Milliseconds before auto-dismiss. Use 0 to keep visible until clicked,
        /// or a negative value to keep visible indefinitely.
        /// </param>
        public static void Show(string message, VisualRole role, int autoHideDelay)
        {
            var window = UIHelper.GetBestOwner();
            if (window == null || string.IsNullOrEmpty(message))
                return;

            var control = new MessageTipControl
            {
                Message = message,
                VisualRole = role,
                AutoHideDelay = autoHideDelay,
            };
            Design.SetIcon(control, GetIconGlyph(role));
            Design.SetIsIconFilled(control, true);
            control.Show(window);
        }

        internal static IconGlyph GetIconGlyph(VisualRole role)
        {
            return role switch
            {
                VisualRole.Success => IconGlyph.CheckmarkCircle24,
                VisualRole.Warning => IconGlyph.Warning24,
                VisualRole.Error => IconGlyph.ErrorCircle24,
                _ => IconGlyph.Info24,
            };
        }
    }
}
