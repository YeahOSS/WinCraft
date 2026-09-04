using System.Runtime.InteropServices;

namespace Windows.Win32
{
    internal static partial class PInvoke
    {
        private const string Uxtheme = "uxtheme.dll";

        // Undocumented ordinal-only exports used for Windows 10 1809+ dark-mode
        // title-bar support. CsWin32 cannot generate ordinal-based imports.
        [DllImport(Uxtheme, EntryPoint = "#135")]
        internal static extern bool AllowDarkModeForApp([MarshalAs(UnmanagedType.Bool)] bool allow);

        [DllImport(Uxtheme, EntryPoint = "#136")]
        internal static extern void FlushMenuThemes();
    }
}
