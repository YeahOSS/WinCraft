using System;
using System.IO;
using WinCraft.Infrastructure.Shell;
using WinCraft.Interop;
using Windows.Win32;
using Windows.Win32.UI.Shell;

namespace WinCraft.Infrastructure.FileSystem
{
    /// <summary>
    /// Reads and updates a folder's <c>desktop.ini</c>.
    /// Inherits generic INI access from <see cref="IniFile"/> and adds
    /// desktop.ini-specific conveniences and folder-attribute management.
    /// </summary>
    internal sealed class DesktopIniFile(string directoryPath)
        : IniFile(Path.Combine(directoryPath, FileName))
    {
        internal const string FileName = "desktop.ini";

        // ── Localized names ──

        public LocalizedNameCollection LocalizedNames => new(this);

        // ── Folder icon ──

        public unsafe IconLocation? Icon
        {
            get
            {
                char* buf = stackalloc char[(int)PInvoke.MAX_PATH];
                var fcs = CreateSettings(PInvoke.FCSM_ICONFILE, buf);
                if (PInvoke.SHGetSetFolderCustomSettings(ref fcs, directoryPath, PInvoke.FCS_READ).Failed)
                    return null;

                string file = ReadString(buf);
                return !string.IsNullOrEmpty(file)
                    ? new IconLocation(file, fcs.iIconIndex)
                    : (IconLocation?)null;
            }
            set => WriteSetting(PInvoke.FCSM_ICONFILE, value?.FileName, value?.Index ?? 0);
        }

        // ── Folder infotip ──

        public unsafe string InfoTip
        {
            get => ReadSetting(PInvoke.FCSM_INFOTIP);
            set => WriteSetting(PInvoke.FCSM_INFOTIP, value);
        }

        // ── Folder logo ──

        public unsafe string Logo
        {
            get => ReadSetting(PInvoke.FCSM_LOGO);
            set => WriteSetting(PInvoke.FCSM_LOGO, value);
        }

        // ── Helpers ──

        /// <summary>
        /// Reads a string-valued folder custom setting via
        /// <see cref="PInvoke.SHGetSetFolderCustomSettings"/>.
        /// Returns <c>null</c> when the value is absent or the API fails.
        /// </summary>
        private unsafe string ReadSetting(uint mask)
        {
            char* buf = stackalloc char[(int)PInvoke.MAX_PATH];
            var fcs = CreateSettings(mask, buf);
            return PInvoke.SHGetSetFolderCustomSettings(ref fcs, directoryPath, PInvoke.FCS_READ).Failed
                ? null
                : ReadString(buf);
        }

        /// <summary>
        /// Writes a string-valued folder custom setting via
        /// <see cref="PInvoke.SHGetSetFolderCustomSettings"/>.
        /// Throws when the API fails.
        /// </summary>
        private unsafe void WriteSetting(uint mask, string value, int iconIndex = 0)
        {
            char* buf = stackalloc char[(int)PInvoke.MAX_PATH];
            StringBuffer.CopyToBuffer(buf, value ?? string.Empty, (int)PInvoke.MAX_PATH);
            var fcs = CreateSettings(mask, buf);
            fcs.iIconIndex = iconIndex;
            PInvoke.SHGetSetFolderCustomSettings(ref fcs, directoryPath, PInvoke.FCS_FORCEWRITE)
                .ThrowOnFailure();
        }

        /// <summary>
        /// Initialises an <see cref="SHFOLDERCUSTOMSETTINGS"/> with
        /// <paramref name="mask"/> and the given buffer for the matching string field.
        /// Only the field indicated by <paramref name="mask"/> receives the buffer
        /// so that concurrent producers never alias.
        /// </summary>
        private static unsafe SHFOLDERCUSTOMSETTINGS CreateSettings(uint mask, char* buf)
        {
            var fcs = new SHFOLDERCUSTOMSETTINGS
            {
                dwSize = (uint)sizeof(SHFOLDERCUSTOMSETTINGS),
                dwMask = mask,
            };

            if ((mask & PInvoke.FCSM_ICONFILE) != 0)
            {
                fcs.pszIconFile = buf;
                fcs.cchIconFile = PInvoke.MAX_PATH;
            }

            if ((mask & PInvoke.FCSM_INFOTIP) != 0)
            {
                fcs.pszInfoTip = buf;
                fcs.cchInfoTip = PInvoke.MAX_PATH;
            }

            if ((mask & PInvoke.FCSM_LOGO) != 0)
            {
                fcs.pszLogo = buf;
                fcs.cchLogo = PInvoke.MAX_PATH;
            }

            return fcs;
        }

        private static unsafe string ReadString(char* buf)
        {
            string result = StringBuffer.ReadNullTerminated(buf, (int)PInvoke.MAX_PATH);
            return !string.IsNullOrEmpty(result) ? result : null;
        }

        // ── Content & refresh ──

        public string GetContent()
        {
            try
            {
                return File.ReadAllText(FilePath);
            }
            catch (IOException)
            {
                return string.Empty;
            }
            catch (UnauthorizedAccessException)
            {
                return string.Empty;
            }
        }

        public static unsafe void RefreshFolder(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
                return;

            string iniPath = Path.Combine(folderPath, FileName);
            if (!File.Exists(iniPath))
                File.WriteAllText(iniPath, string.Empty);

            if (File.Exists(iniPath))
            {
                var attrs = File.GetAttributes(iniPath);
                File.SetAttributes(iniPath, attrs | FileAttributes.Hidden | FileAttributes.System);
            }

            PInvoke.PathMakeSystemFolder(folderPath);

            fixed (char* p = folderPath)
            {
                PInvoke.SHChangeNotify(
                    SHCNE_ID.SHCNE_UPDATEDIR,
                    SHCNF_FLAGS.SHCNF_PATH,
                    p,
                    null);
            }
        }

        /// <summary>
        /// Read/write access to <c>[LocalizedFileNames]</c> entries mapped by file name.
        /// </summary>
        public sealed class LocalizedNameCollection(DesktopIniFile ini)
        {
            private const string Section = "LocalizedFileNames";

            public string this[string fileName]
            {
                get => ini[Section, fileName];
                set => ini[Section, fileName] = value;
            }

            public bool Remove(string fileName)
            {
                return ini.RemoveKey(Section, fileName);
            }
        }
    }
}
