using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace Windows.Win32.UI.Shell
{
    /// <summary>
    /// COM persistence interface for saving objects to an <see cref="IStream"/>.
    /// </summary>
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("00000109-0000-0000-C000-000000000046")]
    internal interface IPersistStream
    {
        [PreserveSig]
        int GetClassID(out Guid pClassID);

        [PreserveSig]
        int IsDirty();

        [PreserveSig]
        int Load(IStream pStm);

        [PreserveSig]
        int Save(IStream pStm, [MarshalAs(UnmanagedType.Bool)] bool fClearDirty);

        [PreserveSig]
        int GetSizeMax(out long pcbSize);
    }
}
