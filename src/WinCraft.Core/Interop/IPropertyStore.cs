using System;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;

namespace Windows.Win32.UI.Shell
{
    /// <summary>
    /// COM property store interface for reading and writing Shell link properties.
    /// </summary>
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
    internal interface IPropertyStore
    {
        [PreserveSig]
        int GetCount(out uint cProps);

        [PreserveSig]
        int GetAt(uint iProp, out PROPERTYKEY pkey);

        [PreserveSig]
        int GetValue(PROPERTYKEY key, out PROPVARIANT pv);

        [PreserveSig]
        int SetValue(PROPERTYKEY key, ref PROPVARIANT pv);

        [PreserveSig]
        int Commit();
    }
}
