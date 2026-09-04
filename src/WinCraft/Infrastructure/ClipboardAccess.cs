using System;
using System.Runtime.InteropServices;
using System.Windows;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Memory;
using Windows.Win32.System.Ole;
using ComDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;

namespace WinCraft.Infrastructure
{
    public static class ClipboardAccess
    {
        public static bool TrySetText(string text)
        {
            return TrySetText((IntPtr)PInvoke.GetActiveWindow(), text);
        }

        public static bool TrySetText(IntPtr ownerWindowHandle, string text)
        {
            // EmptyClipboard leaves no owner when OpenClipboard receives a null window handle.
            if (ownerWindowHandle == IntPtr.Zero)
                return false;

            return TrySetText((HWND)ownerWindowHandle, text);
        }

        private static unsafe bool TrySetText(HWND ownerWindow, string text)
        {
            if (text == null)
                return false;

            char[] characters;
            try
            {
                characters = new char[text.Length + 1];
                text.CopyTo(0, characters, 0, text.Length);
            }
            catch (OutOfMemoryException)
            {
                return false;
            }

            var memory = PInvoke.GlobalAlloc(
                GLOBAL_ALLOC_FLAGS.GMEM_MOVEABLE,
                (nuint)(characters.Length * sizeof(char)));
            if (memory.IsNull)
                return false;

            try
            {
                var destination = PInvoke.GlobalLock(memory);
                if (destination == null)
                    return false;

                try
                {
                    Marshal.Copy(characters, 0, (IntPtr)destination, characters.Length);
                }
                finally
                {
                    PInvoke.GlobalUnlock(memory);
                }

                if (!PInvoke.OpenClipboard(ownerWindow))
                    return false;

                try
                {
                    if (!PInvoke.EmptyClipboard())
                        return false;

                    if (PInvoke.SetClipboardData(
                            (uint)CLIPBOARD_FORMAT.CF_UNICODETEXT,
                            (HANDLE)(IntPtr)memory).IsNull)
                        return false;

                    memory = HGLOBAL.Null;
                    return true;
                }
                finally
                {
                    PInvoke.CloseClipboard();
                }
            }
            finally
            {
                if (!memory.IsNull)
                    PInvoke.GlobalFree(memory);
            }
        }

        /// <summary>
        /// Places arbitrary data on the clipboard without synchronously
        /// flushing an OLE data object into a static clipboard payload.
        /// </summary>
        /// <remarks>
        /// Data is offered through delayed OLE rendering and is therefore
        /// available while this process is running, but is not guaranteed to
        /// remain after it exits. Call from an STA thread.
        /// </remarks>
        public static bool TrySetDataObject(ComDataObject data)
        {
            if (data == null)
                return false;

            return PInvoke.OleSetClipboard(data) >= 0;
        }

        /// <summary>
        /// Places a WPF data object on the clipboard without retrying.
        /// </summary>
        public static bool TrySetDataObject(DataObject data)
        {
            if (data == null)
                return false;

            return TrySetDataObject((ComDataObject)data);
        }

        /// <summary>
        /// Places one WPF clipboard format on the clipboard without retrying.
        /// This supports standard formats such as Bitmap, FileDrop, Html, and
        /// Rtf as well as application-defined formats.
        /// </summary>
        public static bool TrySetData(string format, object data)
        {
            if (string.IsNullOrEmpty(format) || data == null)
                return false;

            var dataObject = new DataObject();
            dataObject.SetData(format, data);
            return TrySetDataObject((ComDataObject)dataObject);
        }
    }
}
