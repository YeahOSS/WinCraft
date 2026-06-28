using System;
using System.Runtime.InteropServices;
using System.Text;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;

namespace WinCraft.Infrastructure.Shell.Shortcuts
{
    /// <summary>
    /// Win+X hash — computation, reading, and writing for Shell shortcut (.lnk) files.
    /// </summary>
    /// <remarks>
    /// Algorithm reverse-engineered by Rafael Rivera.
    /// https://withinrafael.com/2014/04/05/the-winx-menu-and-its-hashing-algorithm/
    /// Reference implementations: https://github.com/riverar/hashlnk (C++),
    /// https://github.com/xmoer/HashLnk (C#).
    /// </remarks>
    public static class WinxHash
    {
        private static readonly (string envVar, string folderId)[] SpecialFolders =
        [
            ("%ProgramFiles%", "{905e63b6-c1bf-494e-b29c-65b732d3d21a}"),
            ("%SystemRoot%\\System32", "{1ac14e77-02e7-4e5d-b744-2eb1ae5198b7}"),
            ("%SystemRoot%", "{f38bf404-1d43-42f2-9305-67de0b28fc23}"),
        ];

        // ── Computation ─────────────────────────────────────────────

        /// <summary>
        /// Computes the Win+X hash for the given shortcut properties.
        /// </summary>
        public static bool TryComputeHash(string targetPath, string arguments, out uint hash)
        {
            string blob = targetPath;
            if (!string.IsNullOrEmpty(blob))
            {
                foreach (var (envVar, folderId) in SpecialFolders)
                {
                    string dirName = Environment.ExpandEnvironmentVariables(envVar);
                    if (blob.StartsWith(dirName + "\\", StringComparison.OrdinalIgnoreCase))
                    {
                        blob = blob.Replace(dirName, folderId);
                        break;
                    }
                }
            }

            blob += arguments;
            blob += "do not prehash links.  this should only be done by the user.";
            blob = blob.ToLowerInvariant();

            byte[] inBytes = Encoding.Unicode.GetBytes(blob);
            int byteCount = inBytes.Length;
            byte[] outBytes = new byte[byteCount];

            unsafe
            {
                fixed (byte* pIn = inBytes)
                fixed (byte* pOut = outBytes)
                {
                    var hr = PInvoke.HashData(pIn, (uint)byteCount, pOut, (uint)byteCount);
                    hash = BitConverter.ToUInt32(outBytes, 0);
                    return hr.Succeeded;
                }
            }
        }

        internal static bool TryComputePropVariant(string targetPath, string arguments, out PROPVARIANT pv)
        {
            if (TryComputeHash(targetPath, arguments, out uint hash))
            {
                pv = PROPVARIANT.FromObject(hash);
                return true;
            }
            pv = default;
            return false;
        }

        // ── Property-store I/O ─────────────────────────────────────

        /// <summary>
        /// Reads the stored Win+X hash from <paramref name="link"/>.  The shortcut
        /// must have been loaded from a file before calling this method.
        /// Returns <c>true</c> if a hash was present.
        /// </summary>
        public static bool TryReadFromLink(ShellLink link, out uint hash)
        {
            hash = 0;

            if (PInvoke.PSGetPropertyKeyFromName("System.Winx.Hash", out PROPERTYKEY pk).Failed)
                return false;

            var propertyStore = (IPropertyStore)link.Link;
            int hr = propertyStore.GetValue(pk, out PROPVARIANT pv);
            try
            {
                if (((HRESULT)hr).Failed || pv.vt != (ushort)VarEnum.VT_UI4)
                    return false;

                hash = pv.uintVal;
                return true;
            }
            finally
            {
                pv.Clear();
            }
        }

        /// <summary>
        /// Writes the Win+X hash to <paramref name="link"/> and saves it to disk.
        /// Supported on Windows 8 through Windows 11 21H2; the OS manages the hash from 22H2 onward.
        /// Reopens the file for writing, which discards any unsaved in-memory changes.
        /// </summary>
        public static bool WriteToLink(ShellLink link)
        {
            if (WindowsVersion.IsBelow(WindowsRelease.Win8)
                || WindowsVersion.IsAtLeast(WindowsRelease.Win11_22H2))
                return false;

            if (link.FileName == null || !link.Load(link.FileName, writable: true))
                return false;

            if (PInvoke.PSGetPropertyKeyFromName("System.Winx.Hash", out PROPERTYKEY pk).Failed)
                return false;

            if (!TryComputePropVariant(link.TargetPath, link.Arguments, out PROPVARIANT pv))
                return false;

            try
            {
                var propertyStore = (IPropertyStore)link.Link;
                if (((HRESULT)propertyStore.SetValue(pk, ref pv)).Failed)
                    return false;

                if (((HRESULT)propertyStore.Commit()).Failed)
                    return false;
            }
            finally
            {
                pv.Clear();
            }

            link.Save();
            return true;
        }
    }
}
