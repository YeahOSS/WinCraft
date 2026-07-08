using WinCraft.Interop;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;

namespace WinCraft.Infrastructure.Shell
{
    /// <summary>
    /// Queries file association information via the Windows AssocQueryString API.
    /// </summary>
    internal static class FileAssoc
    {
        /// <summary>
        /// On Windows 7+ prevents the API from returning placeholder results
        /// (e.g. OpenWith.exe) for extensions with no registered association.
        /// Falls back to <see cref="ASSOCF.ASSOCF_NONE"/> on earlier versions.
        /// </summary>
        private static readonly ASSOCF DefaultFlags =
            WindowsVersion.IsAtLeast(WindowsRelease.Win7)
                ? ASSOCF.ASSOCF_INIT_IGNOREUNKNOWN
                : ASSOCF.ASSOCF_NONE;

        /// <summary>
        /// On Windows 8+ resolves <c>rundll32.exe</c> wrappers to the target
        /// DLL path.  Bit is ignored on earlier versions.
        /// </summary>
        private static readonly ASSOCF ExecutableFlags =
            DefaultFlags
            | (WindowsVersion.IsAtLeast(WindowsRelease.Win8)
                ? ASSOCF.ASSOCF_REMAPRUNDLL
                : ASSOCF.ASSOCF_NONE);

        public static string GetProgId(string extension)
        {
            return Query(ASSOCSTR.ASSOCSTR_PROGID, extension, null, DefaultFlags);
        }

        public static string GetFriendlyAppName(string assoc)
        {
            return Query(ASSOCSTR.ASSOCSTR_FRIENDLYAPPNAME, assoc, null, DefaultFlags);
        }

        public static string GetDefaultIcon(string assoc)
        {
            return Query(ASSOCSTR.ASSOCSTR_DEFAULTICON, assoc, null, DefaultFlags);
        }

        public static string GetExecutable(string extension)
        {
            return Query(ASSOCSTR.ASSOCSTR_EXECUTABLE, extension, null, ExecutableFlags);
        }

        public static string GetVerbCommand(string extension, string verb)
        {
            return Query(ASSOCSTR.ASSOCSTR_COMMAND, extension, verb, DefaultFlags);
        }

        private static string Query(ASSOCSTR assocStr, string assoc, string extra, ASSOCF flags)
        {
            if (string.IsNullOrEmpty(assoc))
                return null;

            uint outLen = 0;
            HRESULT hr = PInvoke.AssocQueryString(
                flags, assocStr, assoc, extra,
                default, ref outLen);

            if (hr.Value != 1 || outLen == 0)
                return null;

            // AssocQueryString returns S_FALSE (1) on size query, not S_OK.
            // Second call fills the buffer.
            uint len = outLen;
            string result = StringBuffer.HeapWrite(
                (int)len,
                p => PInvoke.AssocQueryString(
                    flags, assocStr, assoc, extra, p, ref len));

            return string.IsNullOrEmpty(result) ? null : result;
        }
    }
}
