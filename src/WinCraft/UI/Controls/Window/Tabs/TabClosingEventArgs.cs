using System.ComponentModel;

namespace WinCraft.UI
{
    public sealed class TabClosingEventArgs(object tab) : CancelEventArgs
    {
        public object Tab { get; } = tab;
    }
}
