using System;
using System.Windows;

namespace WinCraft.UI
{
    public abstract class ObjectToHiddenCvt(Func<object, bool> checkStateFunc)
        : StateConverterBase(checkStateFunc, Visibility.Hidden, Visibility.Visible)
    {
    }
}
