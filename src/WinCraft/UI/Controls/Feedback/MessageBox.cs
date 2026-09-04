using System;
using System.Windows;

namespace WinCraft.UI
{
    /// <summary>
    /// Themed modal dialog that follows the application light/dark theme.
    /// Displays a message with optional title, icon, and configurable buttons.
    /// </summary>
    public static class MessageBox
    {
        /// <summary>Show a message with an OK button.</summary>
        public static MessageBoxResult Show(string message)
        {
            return Show(message, null, MessageBoxButton.OK, MessageBoxImage.None, VisualRole.Info);
        }

        /// <summary>Show a message with a title and OK button.</summary>
        public static MessageBoxResult Show(string message, string title)
        {
            return Show(message, title, MessageBoxButton.OK, MessageBoxImage.None, VisualRole.Info);
        }

        /// <summary>Show a message with custom buttons.</summary>
        public static MessageBoxResult Show(string message, string title, MessageBoxButton button)
        {
            return Show(message, title, button, MessageBoxImage.None, VisualRole.Info);
        }

        /// <summary>Show a message with custom buttons and an icon.</summary>
        public static MessageBoxResult Show(string message, string title, MessageBoxButton button, MessageBoxImage icon)
        {
            return Show(message, title, button, icon, VisualRole.Info);
        }

        /// <summary>
        /// Show a themed message box.
        /// </summary>
        /// <param name="message">Body text of the message.</param>
        /// <param name="title">Dialog title. Falls back to the application name.</param>
        /// <param name="button">Which buttons to display.</param>
        /// <param name="icon">Icon category that also sets the accent color.</param>
        /// <param name="role">
        /// Accent color for the primary button. Ignored when
        /// <paramref name="icon"/> is not <see cref="MessageBoxImage.None"/>.
        /// </param>
        public static MessageBoxResult Show(
            string message,
            string title,
            MessageBoxButton button,
            MessageBoxImage icon,
            VisualRole role)
        {
            return ShowCore(message, title, button, icon, role, null, null, null, false).Result;
        }

        /// <summary>
        /// Shows a themed message box with supplemental content or a checkbox.
        /// </summary>
        public static MessageBoxResponse ShowWithResponse(MessageBoxRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return ShowCore(
                request.Message,
                request.Title,
                request.Button,
                request.Icon,
                request.VisualRole,
                request.AdditionalContent,
                request.AdditionalContentTemplate,
                request.CheckBoxText,
                request.IsCheckBoxChecked);
        }

        private static MessageBoxResponse ShowCore(
            string message,
            string title,
            MessageBoxButton button,
            MessageBoxImage icon,
            VisualRole role,
            object additionalContent,
            DataTemplate additionalContentTemplate,
            string checkBoxText,
            bool isCheckBoxChecked)
        {
            var control = new MessageBoxControl
            {
                Message = message,
                AdditionalContent = additionalContent,
                AdditionalContentTemplate = additionalContentTemplate,
                CheckBoxText = checkBoxText,
                IsCheckBoxChecked = isCheckBoxChecked,
            };
            control.Configure(button, icon, role);

            var window = new ChromeWindow
            {
                Title = title ?? Application.Current?.MainWindow?.Title ?? string.Empty,
                Width = 420,
                MinWidth = 320,
                MaxWidth = 560,
                ShowInTaskbar = false,
                ResizeMode = ResizeMode.NoResize,
                SizeToContent = SizeToContent.Height,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = UIHelper.GetBestOwner(),
                Content = control,
            };

            control.RequestClose += window.Close;
            window.ShowDialog();
            return new MessageBoxResponse(control.Result, control.IsCheckBoxChecked);
        }
    }
}
