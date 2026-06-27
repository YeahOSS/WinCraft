using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Memory;

namespace WinCraft.Infrastructure.Shell
{
    /// <summary>
    /// Extension methods for reading and writing Shell clipboard formats on <see cref="IDataObject"/>.
    /// </summary>
    internal static class DataObjectExtensions
    {
        public static FORMATETC CreateFormatEtc(string format)
        {
            return new()
            {
                cfFormat = (short)System.Windows.DataFormats.GetDataFormat(format).Id,
                dwAspect = DVASPECT.DVASPECT_CONTENT,
                tymed = TYMED.TYMED_HGLOBAL,
                lindex = -1,
            };
        }

        public static bool GetBoolean(this IDataObject data, string format)
        {
            var fmt = CreateFormatEtc(format);
            if (data.QueryGetData(ref fmt) != HRESULT.S_OK)
                return false;

            data.GetData(ref fmt, out STGMEDIUM medium);
            try
            {
                var hMem = new HGLOBAL(medium.unionmember);
                try
                {
                    unsafe
                    {
                        return PInvoke.GlobalLock(hMem) != null;
                    }
                }
                finally
                {
                    PInvoke.GlobalUnlock(hMem);
                }
            }
            finally
            {
                PInvoke.ReleaseStgMedium(ref medium);
            }
        }

        public static void SetBoolean(this IDataObject data, string format, bool value)
        {
            var hMem = PInvoke.GlobalAlloc(GLOBAL_ALLOC_FLAGS.GMEM_MOVEABLE, (nuint)Marshal.SizeOf(typeof(int)));
            try
            {
                var fmt = CreateFormatEtc(format);
                try
                {
                    unsafe
                    {
                        *(int*)PInvoke.GlobalLock(hMem) = value ? 1 : 0;
                    }
                }
                finally
                {
                    PInvoke.GlobalUnlock(hMem);
                }

                var medium = new STGMEDIUM { tymed = TYMED.TYMED_HGLOBAL, unionmember = (IntPtr)hMem };
                data.SetData(ref fmt, ref medium, true);
            }
            catch
            {
                PInvoke.GlobalFree(hMem);
                throw;
            }
        }

        public static unsafe MemoryStream GetStream(this IDataObject data, string format)
        {
            var fmt = CreateFormatEtc(format);
            if (data.QueryGetData(ref fmt) != HRESULT.S_OK)
                return null;

            data.GetData(ref fmt, out STGMEDIUM medium);
            try
            {
                var hMem = new HGLOBAL(medium.unionmember);
                try
                {
                    void* src = PInvoke.GlobalLock(hMem);
                    int size = (int)PInvoke.GlobalSize(hMem);
                    byte[] buf = new byte[size];
                    Marshal.Copy((IntPtr)src, buf, 0, size);
                    return new MemoryStream(buf);
                }
                finally
                {
                    PInvoke.GlobalUnlock(hMem);
                }
            }
            finally
            {
                PInvoke.ReleaseStgMedium(ref medium);
            }
        }
    }
}
