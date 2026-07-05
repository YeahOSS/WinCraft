using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace Windows.Win32.UI.Shell
{
    /// <summary>
    /// COM interface for displaying Shell drag images and drop descriptions at the drop target.
    /// </summary>
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("4657278B-411B-11D2-839A-00C04FD918D0")]
    internal interface IDropTargetHelper
    {
        [PreserveSig] int DragEnter(IntPtr hWndTarget, IDataObject data, ref Point pt, int effect);
        [PreserveSig] int DragLeave();
        [PreserveSig] int DragOver(ref Point pt, int effect);
        [PreserveSig] int Drop(IDataObject data, ref Point pt, int effect);
        [PreserveSig] int Show(bool show);
    }
}
