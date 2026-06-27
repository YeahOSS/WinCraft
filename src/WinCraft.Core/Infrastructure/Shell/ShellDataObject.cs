using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Memory;

namespace WinCraft.Infrastructure.Shell
{
    /// <summary>
    /// Managed implementation of <see cref="IDataObject"/> for Shell APIs that
    /// need a COM data object (drag-and-drop, clipboard, context menus, etc.).
    /// Write-only — use it to prepare a payload.  For receiving data, read the
    /// WPF event args or the original Shell object directly.
    /// </summary>
    public sealed class ShellDataObject : IDataObject, IDisposable
    {

        private readonly Dictionary<FORMATETC, STGMEDIUM> _storage = [];

        // ── Convenience wrappers ────────────────────────────────────────

        /// <summary>
        /// Stores a string value under the Unicode text format.
        /// </summary>
        public void SetText(string text)
        {
            SetData(System.Windows.DataFormats.UnicodeText, text);
        }

        /// <summary>
        /// Stores a bitmap under the Bitmap format.
        /// </summary>
        public void SetImage(System.Drawing.Bitmap bitmap)
        {
            SetData(System.Windows.DataFormats.Bitmap, bitmap);
        }

        /// <summary>
        /// Stores file paths under the FileDrop format.
        /// </summary>
        public void SetFileDrop(string[] files)
        {
            SetData(System.Windows.DataFormats.FileDrop, files);
        }

        /// <summary>
        /// Stores an arbitrary object under a named format.
        /// Uses WPF's <see cref="System.Windows.DataObject"/> as an intermediate
        /// marshaler to convert the value into a COM STGMEDIUM.
        /// </summary>
        public void SetData(string format, object data)
        {
            var fmt = DataObjectExtensions.CreateFormatEtc(format);
            var wpfData = new System.Windows.DataObject();
            wpfData.SetData(format, data);
            ((IDataObject)wpfData).GetData(ref fmt, out var medium);
            ((IDataObject)this).SetData(ref fmt, ref medium, true);
        }

        /// <summary>
        /// Stores raw bytes under a named format.
        /// </summary>
        public void SetData(string format, byte[] data)
        {
            var hMem = PInvoke.GlobalAlloc(GLOBAL_ALLOC_FLAGS.GMEM_MOVEABLE, (nuint)data.Length);
            try
            {
                unsafe
                {
                    var dst = (byte*)PInvoke.GlobalLock(hMem);
                    Marshal.Copy(data, 0, (IntPtr)dst, data.Length);
                }
                PInvoke.GlobalUnlock(hMem);

                var fmt = DataObjectExtensions.CreateFormatEtc(format);
                var medium = new STGMEDIUM { tymed = TYMED.TYMED_HGLOBAL, unionmember = (IntPtr)hMem };
                ((IDataObject)this).SetData(ref fmt, ref medium, true);
            }
            catch
            {
                PInvoke.GlobalFree(hMem);
                throw;
            }
        }

        // ── IDataObject implementation ──────────────────────────────────

        public void Dispose()
        {
            var mediums = _storage.Values.ToArray();
            for (int i = 0; i < mediums.Length; i++)
                PInvoke.ReleaseStgMedium(ref mediums[i]);
            _storage.Clear();
            GC.SuppressFinalize(this);
        }

        int IDataObject.DAdvise(ref FORMATETC pFormatetc, ADVF advf, IAdviseSink adviseSink, out int connection)
        {
            connection = 0;
            return HRESULT.E_NOTIMPL;
        }

        void IDataObject.DUnadvise(int connection) => Marshal.ThrowExceptionForHR(HRESULT.E_NOTIMPL);

        int IDataObject.EnumDAdvise(out IEnumSTATDATA enumAdvise)
        {
            enumAdvise = null;
            return HRESULT.OLE_E_ADVISENOTSUPPORTED;
        }

        public IEnumFORMATETC EnumFormatEtc(DATADIR direction)
        {
            if (direction == DATADIR.DATADIR_GET)
            {
                var formats = _storage.Keys.ToArray();
                PInvoke.CreateFormatEnumerator(formats.Length, formats, out var e);
                return e;
            }

            Marshal.ThrowExceptionForHR(HRESULT.E_NOTIMPL);
            return default;
        }

        int IDataObject.GetCanonicalFormatEtc(ref FORMATETC formatIn, out FORMATETC formatOut)
        {
            formatOut = default;
            return HRESULT.DATA_S_SAMEFORMATETC;
        }

        void IDataObject.GetData(ref FORMATETC format, out STGMEDIUM medium)
        {
            medium = default;
            int hr = TryGetData(format, out var found);
            if (hr < 0)
                return; // format not in storage — return TYMED_NULL medium
            PInvoke.CopyStgMedium(ref found, ref medium);
        }

        void IDataObject.GetDataHere(ref FORMATETC format, ref STGMEDIUM medium)
        {
            int hr = TryGetData(format, out var found);
            if (hr < 0)
                return; // format not in storage — leave caller's medium untouched
            PInvoke.CopyStgMedium(ref found, ref medium);
        }

        int IDataObject.QueryGetData(ref FORMATETC format) => TryGetData(format, out _);

        void IDataObject.SetData(ref FORMATETC formatIn, ref STGMEDIUM medium, bool release)
        {
            if (!release)
                Marshal.ThrowExceptionForHR(HRESULT.E_NOTIMPL);

            // Remove any existing entry matching the same key.
            // Copy to locals — ref params cannot be captured by lambdas.
            var cf = formatIn.cfFormat;
            var ty = formatIn.tymed;
            var dw = formatIn.dwAspect;
            var pt = formatIn.ptd;
            var matches = _storage.Keys.Where(k =>
                k.cfFormat == cf && k.tymed == ty && k.dwAspect == dw && k.ptd == pt).ToList();
            foreach (var m in matches)
                _storage.Remove(m);

            _storage.Add(formatIn, medium);
        }

        private int TryGetData(FORMATETC format, out STGMEDIUM medium)
        {
            medium = default;
            var entries = _storage.ToArray();

            var byFormat = entries.Where(e => e.Key.cfFormat == format.cfFormat);
            if (!byFormat.Any())
                return HRESULT.DV_E_FORMATETC;

            var byAspect = byFormat.Where(e => e.Key.dwAspect == format.dwAspect);
            if (!byAspect.Any())
                return HRESULT.DV_E_DVASPECT;

            var byMedium = byAspect.Where(e => format.tymed.HasFlag(e.Key.tymed));
            if (!byMedium.Any())
                return HRESULT.DV_E_TYMED;

            var byIndex = byMedium.Where(e => e.Key.lindex == format.lindex);
            if (!byIndex.Any())
                return HRESULT.DV_E_LINDEX;

            medium = byIndex.First().Value;
            return HRESULT.S_OK;
        }
    }
}
