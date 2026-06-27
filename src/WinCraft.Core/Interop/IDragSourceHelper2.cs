using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace Windows.Win32.UI.Shell
{
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("83E07D0D-0C5F-4163-BF1A-60B274051E40")]
    internal interface IDragSourceHelper2
    {
        [PreserveSig] int InitializeFromBitmap(ref SHDRAGIMAGE dragImage, IDataObject data);
        [PreserveSig] int InitializeFromWindow(IntPtr hWnd, ref Point pt, IDataObject data);
        [PreserveSig] int SetFlags(int dwFlags);
    }
}
