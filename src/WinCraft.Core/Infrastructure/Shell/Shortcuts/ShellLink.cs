using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;
using IPersistFile = System.Runtime.InteropServices.ComTypes.IPersistFile;

namespace WinCraft.Infrastructure.Shell.Shortcuts
{
    /// <summary>
    /// Loads, creates, and saves Windows Shell shortcut (.lnk) files.
    /// </summary>
    [DebuggerDisplay("{" + nameof(FileName) + "}")]
    public sealed class ShellLink : IDisposable
    {
        private IShellLink _link;
        private bool _disposed;

        internal IShellLink Link
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(ShellLink));
                _link ??= (IShellLink)new CShellLink();
                return _link;
            }
        }

        public string FileName { get; private set; }

        public string TargetPath
        {
            get
            {
                var sb = new StringBuilder((int)PInvoke.MAX_PATH);
                Link.GetPath(sb, (int)PInvoke.MAX_PATH, IntPtr.Zero, SLGP_FLAGS.SLGP_RAWPATH);
                return sb.ToString();
            }
            set => Link.SetPath(value ?? string.Empty);
        }

        public string Arguments
        {
            get
            {
                var sb = new StringBuilder((int)PInvoke.MAX_PATH);
                Link.GetArguments(sb, (int)PInvoke.MAX_PATH);
                return sb.ToString();
            }
            set => Link.SetArguments(value ?? string.Empty);
        }

        public string WorkingDirectory
        {
            get
            {
                var sb = new StringBuilder((int)PInvoke.MAX_PATH);
                Link.GetWorkingDirectory(sb, (int)PInvoke.MAX_PATH);
                return sb.ToString();
            }
            set => Link.SetWorkingDirectory(value ?? string.Empty);
        }

        public (string FileName, int Index) IconLocation
        {
            get
            {
                var sb = new StringBuilder((int)PInvoke.MAX_PATH);
                Link.GetIconLocation(sb, (int)PInvoke.MAX_PATH, out int i);
                return (sb.ToString(), i);
            }
            set => Link.SetIconLocation(value.FileName, value.Index);
        }

        public string Description
        {
            get
            {
                var sb = new StringBuilder((int)PInvoke.MAX_PATH);
                Link.GetDescription(sb, (int)PInvoke.MAX_PATH);
                return sb.ToString();
            }
            set => Link.SetDescription(value ?? string.Empty);
        }

        public (Key Key, ModifierKeys Modifiers) HotKey
        {
            get
            {
                Link.GetHotKey(out ushort raw);
                ushort key = (ushort)(raw & 0xFF);
                ModifierKeys modifiers = MapHotKeyModifiersFromNative((ushort)((raw >> 8) & 0x07));
                return (KeyInterop.KeyFromVirtualKey(key), modifiers);
            }
            set
            {
                ushort modifiers = MapHotKeyModifiersToNative(value.Modifiers);
                ushort raw = (ushort)(KeyInterop.VirtualKeyFromKey(value.Key) | (modifiers << 8));
                Link.SetHotKey(raw);
            }
        }

        public WindowState WindowState
        {
            get
            {
                Link.GetShowCmd(out SHOW_WINDOW_CMD style);
                return style switch
                {
                    SHOW_WINDOW_CMD.SW_SHOWMAXIMIZED => WindowState.Maximized,
                    SHOW_WINDOW_CMD.SW_SHOWMINNOACTIVE => WindowState.Minimized,
                    _ => WindowState.Normal,
                };
            }
            set
            {
                Link.SetShowCmd(value switch
                {
                    WindowState.Maximized => SHOW_WINDOW_CMD.SW_SHOWMAXIMIZED,
                    WindowState.Minimized => SHOW_WINDOW_CMD.SW_SHOWMINNOACTIVE,
                    _ => SHOW_WINDOW_CMD.SW_SHOWNORMAL,
                });
            }
        }

        /// <summary>
        /// Gets the PIDL for this shortcut. The caller must free the returned pointer
        /// with <c>ILFree</c> (or <c>CoTaskMemFree</c> on Windows Vista and later).
        /// </summary>
        internal IntPtr Pidl
        {
            get
            {
                Link.GetIDList(out IntPtr pidl);
                return pidl;
            }
            set => Link.SetIDList(value);
        }

        public bool RunAsAdmin
        {
            get
            {
                ((IShellLinkDataList)Link).GetFlags(out SHELL_LINK_DATA_FLAGS flags);
                return (flags & SHELL_LINK_DATA_FLAGS.SLDF_RUNAS_USER) != 0;
            }
            set
            {
                var dataLink = (IShellLinkDataList)Link;
                dataLink.GetFlags(out SHELL_LINK_DATA_FLAGS flags);
                flags = value
                    ? (flags | SHELL_LINK_DATA_FLAGS.SLDF_RUNAS_USER)
                    : (flags & ~SHELL_LINK_DATA_FLAGS.SLDF_RUNAS_USER);
                dataLink.SetFlags(flags);
            }
        }

        // ── Constructors ─────────────────────────────────────────────

        /// <summary>
        /// Creates an empty shortcut with no target or file association.
        /// </summary>
        public ShellLink() { }

        /// <summary>
        /// Creates a shortcut and loads it from the specified path.
        /// </summary>
        public ShellLink(string lnkPath, bool writable = false)
        {
            Load(lnkPath, writable);
        }

        // ── Dispose ──────────────────────────────────────────────────

        ~ShellLink()
        {
            Dispose(false);
        }

        /// <summary>
        /// Releases the underlying COM shortcut object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
                return;
            _disposed = true;

            if (disposing && _link != null && Marshal.IsComObject(_link))
            {
                Marshal.FinalReleaseComObject(_link);
                _link = null;
            }
        }

        // ── Load / Save ──────────────────────────────────────────────

        /// <summary>
        /// Loads shortcut data from the specified file.  Returns <c>true</c> on success,
        /// <c>false</c> if the file does not exist or could not be opened.
        /// </summary>
        public bool Load(string lnkPath, bool writable)
        {
            FileName = lnkPath;
            if (File.Exists(lnkPath))
            {
                STGM mode = writable ? STGM.STGM_READWRITE : STGM.STGM_READ;
                try
                {
                    ((IPersistFile)Link).Load(lnkPath, (int)mode);
                    return true;
                }
                catch (Exception ex) when (ex is not OutOfMemoryException)
                {
                    return false;
                }
            }
            return false;
        }

        /// <summary>
        /// Saves the shortcut back to the file it was loaded from or last saved to.
        /// </summary>
        public void Save()
        {
            SaveAs(FileName);
        }

        /// <summary>
        /// Saves the shortcut to the specified path, updating <see cref="FileName"/>.
        /// </summary>
        public void SaveAs(string lnkPath)
        {
            ((IPersistFile)Link).Save(lnkPath, true);
        }

        /// <summary>
        /// Saves the shortcut to a byte array via <see cref="IPersistStream"/>,
        /// avoiding temporary files on disk.
        /// </summary>
        internal byte[] SaveToBytes()
        {
            int hr = ((IPersistStream)Link).GetSizeMax(out long size);
            if (((HRESULT)hr).Failed)
                throw new COMException("IPersistStream.GetSizeMax failed.", hr);

            IntPtr hGlobal = Marshal.AllocHGlobal((IntPtr)size);
            IStream stream = null;
            bool streamOwnsMemory = false;
            try
            {
                hr = PInvoke.CreateStreamOnHGlobal(hGlobal, true, out stream);
                if (((HRESULT)hr).Failed)
                    throw new COMException("CreateStreamOnHGlobal failed.", hr);
                streamOwnsMemory = true;

                hr = ((IPersistStream)Link).Save(stream, false);
                if (((HRESULT)hr).Failed)
                    throw new COMException("IPersistStream.Save failed.", hr);

                byte[] result = new byte[size];
                stream.Seek(0, 0, IntPtr.Zero);

                IntPtr pcbRead = Marshal.AllocHGlobal(sizeof(int));
                try
                {
                    Marshal.WriteInt32(pcbRead, 0);
                    stream.Read(result, result.Length, pcbRead);
                    int bytesRead = Marshal.ReadInt32(pcbRead);
                    if (bytesRead < result.Length)
                        Array.Resize(ref result, bytesRead);
                }
                finally
                {
                    Marshal.FreeHGlobal(pcbRead);
                }

                return result;
            }
            finally
            {
                if (stream != null)
                    Marshal.ReleaseComObject(stream);
                if (!streamOwnsMemory)
                    Marshal.FreeHGlobal(hGlobal);
            }
        }

        // WPF ModifierKeys: Alt=0x01, Control=0x02, Shift=0x04
        // IShellLink HOTKEYF:  Shift=0x01, Control=0x02, Alt=0x04
        // Control maps correctly; Shift and Alt must be swapped (bits 0 ↔ 2).

        private static ModifierKeys MapHotKeyModifiersFromNative(ushort hotkeyf)
        {
            uint bits = hotkeyf;
            uint swapped = (bits & 0x02) | ((bits & 0x01) << 2) | ((bits & 0x04) >> 2);
            return (ModifierKeys)swapped;
        }

        private static ushort MapHotKeyModifiersToNative(ModifierKeys modifiers)
        {
            uint bits = (uint)(modifiers & (ModifierKeys.Alt | ModifierKeys.Control | ModifierKeys.Shift));
            uint swapped = (bits & 0x02) | ((bits & 0x01) << 2) | ((bits & 0x04) >> 2);
            return (ushort)swapped;
        }
    }
}
