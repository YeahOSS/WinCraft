using System;
using System.Windows;

namespace WinCraft.UI
{
    public abstract class ObjectsToCollapsedCvt(Func<object[], bool> combiner)
        : MultiStateConverterBase(combiner, Visibility.Collapsed, Visibility.Visible)
    {
    }
}
