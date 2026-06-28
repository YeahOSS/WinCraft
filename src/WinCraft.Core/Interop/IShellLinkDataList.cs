using System;
using System.Runtime.InteropServices;

namespace Windows.Win32.UI.Shell
{
    /// <summary>
    /// COM interface for Shell link data block access (flags, extra data).
    /// </summary>
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("45E2B4AE-B1C3-11D0-B92F-00A0C90312E1")]
    internal interface IShellLinkDataList
    {
        [PreserveSig]
        int AddDataBlock(IntPtr pDataBlock);

        [PreserveSig]
        int CopyDataBlock(uint dwSig, out IntPtr ppDataBlock);

        [PreserveSig]
        int RemoveDataBlock(uint dwSig);

        [PreserveSig]
        int GetFlags(out SHELL_LINK_DATA_FLAGS pdwFlags);

        [PreserveSig]
        int SetFlags(SHELL_LINK_DATA_FLAGS dwFlags);
    }
}
