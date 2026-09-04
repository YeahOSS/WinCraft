using System.Windows;

namespace WinCraft.UI
{
    /// <summary>
    /// Contains the button result and optional checkbox state from a message box.
    /// </summary>
    public sealed class MessageBoxResponse
    {
        internal MessageBoxResponse(MessageBoxResult result, bool isCheckBoxChecked)
        {
            Result = result;
            IsCheckBoxChecked = isCheckBoxChecked;
        }

        public MessageBoxResult Result { get; }

        public bool IsCheckBoxChecked { get; }
    }
}
