using System;
using System.Windows;

namespace WinCraft.UI
{
    public abstract class ObjectToCollapsedCvt(Func<object, bool> checkStateFunc)
        : StateConverterBase(checkStateFunc, Visibility.Collapsed, Visibility.Visible)
    {
    }
}
