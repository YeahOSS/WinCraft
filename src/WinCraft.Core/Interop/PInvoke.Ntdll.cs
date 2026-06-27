using System.Runtime.InteropServices;

namespace Windows.Win32
{
    internal static partial class PInvoke
    {
        private const string Ntdll = "ntdll.dll";

        /// <summary>
        /// Returns version information about the currently running operating system.
        /// </summary>
        /// <remarks>
        /// Hand-written — RtlGetVersion is absent from both the SDK and WDK
        /// win32metadata packages (ntdll user-mode exports are not covered).
        /// See docs/win32-interop.md § CsWin32 COM Interface Limitations.
        /// </remarks>
        [DllImport(Ntdll, ExactSpelling = true)]
        internal static extern int RtlGetVersion(ref RTL_OSVERSIONINFOEXW lpVersionInformation);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct RTL_OSVERSIONINFOEXW
        {
            public uint dwOSVersionInfoSize;
            public uint dwMajorVersion;
            public uint dwMinorVersion;
            public uint dwBuildNumber;
            public uint dwPlatformId;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szCSDVersion;

            public ushort wServicePackMajor;
            public ushort wServicePackMinor;
            public ushort wSuiteMask;
            public byte wProductType;
            public byte wReserved;
        }
    }
}
