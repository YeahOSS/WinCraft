using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using WinCraft.Infrastructure.Shell;
using Windows.Win32;
using Windows.Win32.System.Memory;
using Windows.Win32.UI.Shell;

namespace WinCraft.Infrastructure.Shell.DragDrop
{
    /// <summary>
    /// Displays Shell drag images and drop descriptions for incoming drag operations.
    /// </summary>
    public static class ShellDropTarget
    {
        private static readonly IDropTargetHelper _helper = (IDropTargetHelper)new CDragDropHelper();

        // Only accessed on the UI thread.
        private static IDataObject _data;

        /// <summary>
        /// Registers a WPF element as a drop target, auto-wiring drag events
        /// for Shell drag image support.
        /// </summary>
        /// <param name="element">The target element (typically a Window or control).</param>
        /// <param name="allowedEffect">Drop effect shown while dragging over the target.</param>
        /// <param name="onDrop">Optional callback invoked when data is dropped.</param>
        public static void Register(
            System.Windows.UIElement element,
            System.Windows.DragDropEffects allowedEffect,
            Action<ShellDragEventArgs> onDrop = null)
        {
            element.AllowDrop = true;
            element.DragEnter += (s, e) =>
            {
                var args = new ShellDragEventArgs(e);
                OnDragEnter(args);
                e.Effects = allowedEffect;
                e.Handled = true;
            };
            element.DragOver += (s, e) =>
            {
                var args = new ShellDragEventArgs(e);
                OnDragOver(args);
                e.Effects = allowedEffect;
                e.Handled = true;
            };
            element.DragLeave += (s, e) => OnDragLeave();
            element.Drop += (s, e) =>
            {
                OnDragLeave();
                var args = new ShellDragEventArgs(e);
                OnDrop(args);
                onDrop?.Invoke(args);
            };
        }

        public static void OnDragEnter(ShellDragEventArgs args)
        {
            _data = args.Data;
            Point pt = args.Point;
            _helper.DragEnter(IntPtr.Zero, _data, ref pt, (int)args.Effects);
        }

        public static void OnDragOver(ShellDragEventArgs args)
        {
            _data = args.Data;
            Point pt = args.Point;
            _helper.DragOver(ref pt, (int)args.Effects);
        }

        public static void OnDrop(ShellDragEventArgs args)
        {
            _data = args.Data;
            Point pt = args.Point;
            _helper.Drop(_data, ref pt, (int)args.Effects);
        }

        public static void OnDragLeave() => _helper.DragLeave();

        public static void ClearDropDescription()
        {
            SetDropDescription(System.Windows.DragDropEffects.None, null, null);
        }

        public static void SetDropDescription(
            System.Windows.DragDropEffects effect, string message, string insert = null)
        {
            if (_data == null)
                return;

            var desc = new DROPDESCRIPTION
            {
                type = ResolveDropImageType(effect),
            };
            desc.szMessage.SetString(message);
            desc.szInsert.SetString(insert);

            int size = Marshal.SizeOf(typeof(DROPDESCRIPTION));
            var hMem = PInvoke.GlobalAlloc(GLOBAL_ALLOC_FLAGS.GMEM_MOVEABLE, (nuint)size);
            try
            {
                unsafe
                {
                    Marshal.StructureToPtr(desc, (IntPtr)PInvoke.GlobalLock(hMem), false);
                }
                PInvoke.GlobalUnlock(hMem);

                var fmt = DataObjectExtensions.CreateFormatEtc(PInvoke.CFSTR_DROPDESCRIPTION);
                var medium = new STGMEDIUM { tymed = TYMED.TYMED_HGLOBAL, unionmember = (IntPtr)hMem };
                _data.SetData(ref fmt, ref medium, true);
            }
            catch
            {
                PInvoke.GlobalFree(hMem);
                throw;
            }
        }

        private static DROPIMAGETYPE ResolveDropImageType(System.Windows.DragDropEffects effect)
        {
            if (effect == System.Windows.DragDropEffects.None)
                return DROPIMAGETYPE.DROPIMAGE_INVALID;

            // Map composite DragDropEffects to a single DROPIMAGETYPE value.
            // Shell only recognizes Copy, Move, Link, and a few other constants;
            // bitwise combinations produce undefined behavior.
            if ((effect & System.Windows.DragDropEffects.Copy) != 0)
                return DROPIMAGETYPE.DROPIMAGE_COPY;
            if ((effect & System.Windows.DragDropEffects.Move) != 0)
                return DROPIMAGETYPE.DROPIMAGE_MOVE;
            if ((effect & System.Windows.DragDropEffects.Link) != 0)
                return DROPIMAGETYPE.DROPIMAGE_LINK;

            return DROPIMAGETYPE.DROPIMAGE_NONE;
        }
    }
}
