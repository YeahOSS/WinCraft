using System;
using System.Drawing;
using System.Runtime.InteropServices.ComTypes;
using System.Windows.Input;
using WinCraft.Infrastructure;
using WinCraft.Infrastructure.Shell;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.Shell;

namespace WinCraft.Infrastructure.Shell.DragDrop
{
    /// <summary>
    /// Initiates WPF drag-and-drop with a Shell-rendered drag image.
    /// </summary>
    public static class ShellDragSource
    {
        /// <summary>
        /// When true (default), creates a custom cursor from the drag bitmap
        /// as a fallback when the Shell layered drag window is unavailable.
        /// </summary>
        public static bool ShowDragImageWhenNotSupported { get; set; } = true;

        private static readonly IDragSourceHelper2 _helper = (IDragSourceHelper2)new CDragDropHelper();

        // Only accessed on the UI thread.
        private static IDataObject _data;
        private static Bitmap _dragImage;
        private static Point _offset;

        public static System.Windows.DragDropEffects DoDragDrop(
            System.Windows.UIElement element,
            IDataObject data,
            System.Windows.DragDropEffects allowedEffects,
            Bitmap dragImage,
            Point? dragImageOffset = null)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (dragImage == null)
                throw new ArgumentNullException(nameof(dragImage));

            SetDragImage(data, dragImage, dragImageOffset);

            element.GiveFeedback += OnGiveFeedback;
            element.QueryContinueDrag += OnQueryContinueDrag;

            System.Windows.DragDropEffects result;
            try
            {
                result = System.Windows.DragDrop.DoDragDrop(element, data, allowedEffects);
            }
            finally
            {
                element.GiveFeedback -= OnGiveFeedback;
                element.QueryContinueDrag -= OnQueryContinueDrag;
                Mouse.OverrideCursor = null;
            }

            return result;
        }

        private static void SetDragImage(IDataObject data, Bitmap dragImage, Point? offset = null)
        {
            _data = data;
            _dragImage = dragImage;
            _offset = offset ?? new Point(0, dragImage.Height);

            var image = new SHDRAGIMAGE
            {
                hbmpDragImage = (HBITMAP)dragImage.GetHbitmap(Color.FromArgb(0)),
                sizeDragImage = dragImage.Size,
                ptOffset = _offset,
                crColorKey = (COLORREF)uint.MaxValue,
            };

            _helper.SetFlags((int)DSH_FLAGS.DSH_ALLOWDROPDESCRIPTIONTEXT);
            _helper.InitializeFromBitmap(ref image, data);
        }

        private static void OnGiveFeedback(object sender, System.Windows.GiveFeedbackEventArgs e)
        {
            bool handled = (_data != null
                && _data.GetBoolean("IsShowingLayered")
                && UpdateDragImage())
                || (ShowDragImageWhenNotSupported && TrySetFallbackCursor());

            e.Handled = handled;
            e.UseDefaultCursors = !handled;
        }

        private static bool TrySetFallbackCursor()
        {
            if (_dragImage == null)
                return false;

            try
            {
                Mouse.OverrideCursor = CursorHelper.CreateCursorFromBitmap(_dragImage, _offset);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void OnQueryContinueDrag(object sender, System.Windows.QueryContinueDragEventArgs e)
        {
            if (e.EscapePressed)
            {
                e.Action = System.Windows.DragAction.Cancel;
                return;
            }

            var ks = (int)e.KeyStates;
            bool leftDown = (ks & (int)System.Windows.DragDropKeyStates.LeftMouseButton) != 0;
            bool rightDown = (ks & (int)System.Windows.DragDropKeyStates.RightMouseButton) != 0;
            e.Action = !leftDown && !rightDown
                ? System.Windows.DragAction.Drop
                : System.Windows.DragAction.Continue;
        }

        private static bool UpdateDragImage()
        {
            var fmt = DataObjectExtensions.CreateFormatEtc("DragWindow");
            if (_data.QueryGetData(ref fmt) != HRESULT.S_OK)
                return false;

            _data.GetData(ref fmt, out STGMEDIUM medium);
            try
            {
                var hMem = new HGLOBAL(medium.unionmember);
                try
                {
                    unsafe
                    {
                        var hWnd = new HWND(*(IntPtr*)PInvoke.GlobalLock(hMem));
                        return PInvoke.IsWindow(hWnd)
                            && PInvoke.PostMessage(hWnd, PInvoke.WM_USER + 3 /* DDWM_UPDATEWINDOW */, 0, 0);
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
    }
}
