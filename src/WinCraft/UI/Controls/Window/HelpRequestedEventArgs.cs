using System;
using System.Windows;

namespace WinCraft.UI
{
    /// <summary>
    /// Carried by <see cref="WindowBase.HelpRequested"/>.
    /// <see cref="Topic"/> is the nearest inherited
    /// <see cref="WindowBase.HelpTopicProperty"/> value, or <see langword="null"/>.
    /// </summary>
    public class HelpRequestedEventArgs : EventArgs
    {
        public HelpRequestedEventArgs(object topic)
        {
            Topic = topic;
        }

        public object Topic { get; }
    }
}
