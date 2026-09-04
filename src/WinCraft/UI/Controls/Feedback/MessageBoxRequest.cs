using System.Windows;

namespace WinCraft.UI
{
    /// <summary>
    /// Describes a message box with optional supplemental content and a checkbox.
    /// </summary>
    public sealed class MessageBoxRequest
    {
        public string Message { get; set; } = string.Empty;

        public string Title { get; set; }

        public MessageBoxButton Button { get; set; } = MessageBoxButton.OK;

        public MessageBoxImage Icon { get; set; } = MessageBoxImage.None;

        public VisualRole VisualRole { get; set; } = VisualRole.Info;

        public object AdditionalContent { get; set; }

        public DataTemplate AdditionalContentTemplate { get; set; }

        public string CheckBoxText { get; set; }

        public bool IsCheckBoxChecked { get; set; }
    }
}
