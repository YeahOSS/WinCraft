using System;
using System.Windows;

namespace WinCraft.UI
{
    public abstract class ObjectsToHiddenCvt(Func<object[], bool> combiner)
        : MultiStateConverterBase(combiner, Visibility.Hidden, Visibility.Visible)
    {
    }
}
