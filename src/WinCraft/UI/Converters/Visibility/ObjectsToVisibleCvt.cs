using System;
using System.Windows;

namespace WinCraft.UI
{
    public abstract class ObjectsToVisibleCvt(Func<object[], bool> combiner)
        : MultiStateConverterBase(combiner, Visibility.Visible, Visibility.Collapsed)
    {
    }
}
