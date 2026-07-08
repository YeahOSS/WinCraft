using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace Windows.Win32.UI.Shell
{
    /// <summary>
    /// COM interface for rendering a drag image during a Shell drag operation.
    /// </summary>
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("DE5BF786-477A-11D2-839D-00C04FD918D0")]
    internal interface IDragSourceHelper
    {
        [PreserveSig] int InitializeFromBitmap(ref SHDRAGIMAGE dragImage, IDataObject data);
        [PreserveSig] int InitializeFromWindow(IntPtr hWnd, ref Point pt, IDataObject data);
    }
}
