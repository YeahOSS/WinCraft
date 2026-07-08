using System;
using System.Windows;

namespace WinCraft.UI
{
    public abstract class ObjectToVisibleCvt(Func<object, bool> checkStateFunc)
        : StateConverterBase(checkStateFunc, Visibility.Visible, Visibility.Collapsed)
    {
    }
}
