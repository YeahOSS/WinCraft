using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace WinCraft.Infrastructure
{
    /// <summary>
    /// Windows release milestones for feature gating.
    /// Build numbers reference https://learn.microsoft.com/zh-cn/windows/release-health/windows11-release-information.
    /// </summary>
    public enum WindowsRelease
    {
        /// <summary>Unrecognized or future release.</summary>
        Unknown = 0,
        /// <summary>Windows XP (5.1)</summary>
        XP,
        /// <summary>Windows Vista (6.0)</summary>
        Vista,
        /// <summary>Windows Vista SP1 (6.0.6001)</summary>
        Vista_SP1,
        /// <summary>Windows Vista SP2 (6.0.6002)</summary>
        Vista_SP2,
        /// <summary>Windows 7 (6.1)</summary>
        Win7,
        /// <summary>Windows 7 SP1 (6.1.7601)</summary>
        Win7_SP1,
        /// <summary>Windows 8 (6.2)</summary>
        Win8,
        /// <summary>Windows 8.1 (6.3)</summary>
        Win8_1,

        // ── Windows 10 ───────────────────────────────────────────
        /// <summary>1507 (10.0.10240)</summary>
        Win10_1507,
        /// <summary>1511 (10.0.10586)</summary>
        Win10_1511,
        /// <summary>1607 (10.0.14393)</summary>
        Win10_1607,
        /// <summary>1703 (10.0.15063)</summary>
        Win10_1703,
        /// <summary>1709 (10.0.16299)</summary>
        Win10_1709,
        /// <summary>1803 (10.0.17134)</summary>
        Win10_1803,
        /// <summary>1809 (10.0.17763)</summary>
        Win10_1809,
        /// <summary>1903 (10.0.18362)</summary>
        Win10_1903,
        /// <summary>1909 (10.0.18363)</summary>
        Win10_1909,
        /// <summary>2004 (10.0.19041)</summary>
        Win10_2004,
        /// <summary>20H2 (10.0.19042)</summary>
        Win10_20H2,
        /// <summary>21H1 (10.0.19043)</summary>
        Win10_21H1,
        /// <summary>21H2 (10.0.19044)</summary>
        Win10_21H2,
        /// <summary>22H2 (10.0.19045) — final Windows 10 release</summary>
        Win10_22H2,

        // ── Windows 11 ───────────────────────────────────────────
        /// <summary>21H2 (10.0.22000)</summary>
        Win11_21H2,
        /// <summary>22H2 (10.0.22621)</summary>
        Win11_22H2,
        /// <summary>23H2 (10.0.22631)</summary>
        Win11_23H2,
        /// <summary>24H2 (10.0.26100)</summary>
        Win11_24H2,
        /// <summary>25H2 (10.0.26200)</summary>
        Win11_25H2,
        /// <summary>26H1 (10.0.28000)</summary>
        Win11_26H1,
    }

    public static class WindowsVersion
    {
        // Uses a manual P/Invoke for RtlGetVersion rather than CsWin32.
        // RtlGetVersion is excluded from both the SDK and WDK win32metadata
        // packages (ntdll user-mode exports are not covered).  CsWin32
        // cannot emit the binding because it is absent from the metadata.
        [DllImport("ntdll.dll", ExactSpelling = true)]
        private static extern int RtlGetVersion(ref RTL_OSVERSIONINFOEXW lpVersionInformation);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct RTL_OSVERSIONINFOEXW
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

        private struct OsVersionInfo
        {
            public Version Version;
            public int ServicePack_Major;
            public int ServicePack_Minor;
        }

        private static readonly OsVersionInfo _info = GetVersionInfo();

        public static Version Current => _info.Version;

        /// <summary>Service pack major version (e.g. 1 for Windows 7 SP1), or 0 if none.</summary>
        public static int ServicePackMajor => _info.ServicePack_Major;

        /// <summary>Service pack minor version, usually 0.</summary>
        public static int ServicePackMinor => _info.ServicePack_Minor;

        public static bool IsAtLeast(WindowsRelease release)
        {
            return _info.Version >= GetVersion(release);
        }

        public static bool IsAtLeast(int major, int minor = 0, int build = 0)
        {
            return _info.Version >= new Version(major, minor, build);
        }

        public static bool IsBelow(WindowsRelease release)
        {
            return _info.Version < GetVersion(release);
        }

        public static bool IsBelow(int major, int minor = 0, int build = 0)
        {
            return _info.Version < new Version(major, minor, build);
        }

        public static Version GetVersion(WindowsRelease release)
        {
            return release switch
            {
                WindowsRelease.Unknown => throw new InvalidOperationException(
                    "Cannot map an unknown release to a version."),
                WindowsRelease.XP => new Version(5, 1),
                WindowsRelease.Vista => new Version(6, 0),
                WindowsRelease.Vista_SP1 => new Version(6, 0, 6001),
                WindowsRelease.Vista_SP2 => new Version(6, 0, 6002),
                WindowsRelease.Win7 => new Version(6, 1),
                WindowsRelease.Win7_SP1 => new Version(6, 1, 7601),
                WindowsRelease.Win8 => new Version(6, 2),
                WindowsRelease.Win8_1 => new Version(6, 3),

                // Windows 10
                WindowsRelease.Win10_1507 => new Version(10, 0, 10240),
                WindowsRelease.Win10_1511 => new Version(10, 0, 10586),
                WindowsRelease.Win10_1607 => new Version(10, 0, 14393),
                WindowsRelease.Win10_1703 => new Version(10, 0, 15063),
                WindowsRelease.Win10_1709 => new Version(10, 0, 16299),
                WindowsRelease.Win10_1803 => new Version(10, 0, 17134),
                WindowsRelease.Win10_1809 => new Version(10, 0, 17763),
                WindowsRelease.Win10_1903 => new Version(10, 0, 18362),
                WindowsRelease.Win10_1909 => new Version(10, 0, 18363),
                WindowsRelease.Win10_2004 => new Version(10, 0, 19041),
                WindowsRelease.Win10_20H2 => new Version(10, 0, 19042),
                WindowsRelease.Win10_21H1 => new Version(10, 0, 19043),
                WindowsRelease.Win10_21H2 => new Version(10, 0, 19044),
                WindowsRelease.Win10_22H2 => new Version(10, 0, 19045),

                // Windows 11
                WindowsRelease.Win11_21H2 => new Version(10, 0, 22000),
                WindowsRelease.Win11_22H2 => new Version(10, 0, 22621),
                WindowsRelease.Win11_23H2 => new Version(10, 0, 22631),
                WindowsRelease.Win11_24H2 => new Version(10, 0, 26100),
                WindowsRelease.Win11_25H2 => new Version(10, 0, 26200),
                WindowsRelease.Win11_26H1 => new Version(10, 0, 28000),

                _ => throw new InvalidEnumArgumentException(
                    nameof(release), (int)release, typeof(WindowsRelease)),
            };
        }

        /// <summary>A human-readable name for the release, e.g. "Windows 11 24H2".</summary>
        public static string GetDisplayName(WindowsRelease release)
        {
            return release switch
            {
                WindowsRelease.Unknown => nameof(WindowsRelease.Unknown),
                WindowsRelease.Win8_1 => "Windows 8.1",
                _ => "Windows " + release.ToString().Replace("Win", "").Replace('_', ' '),
            };
        }

        private static OsVersionInfo GetVersionInfo()
        {
            var info = new RTL_OSVERSIONINFOEXW
            {
                dwOSVersionInfoSize = (uint)Marshal.SizeOf(typeof(RTL_OSVERSIONINFOEXW))
            };
            int status = RtlGetVersion(ref info);
            if (status == 0) // STATUS_SUCCESS
            {
                return new OsVersionInfo
                {
                    Version = new Version((int)info.dwMajorVersion,
                                          (int)info.dwMinorVersion,
                                          (int)info.dwBuildNumber),
                    ServicePack_Major = info.wServicePackMajor,
                    ServicePack_Minor = info.wServicePackMinor,
                };
            }
            // RtlGetVersion should never fail on a running NT system;
            // fall back to the (possibly shimmed) BCL API as a last resort.
            return new OsVersionInfo { Version = Environment.OSVersion.Version };
        }

        /// <summary>
        /// The highest known <see cref="WindowsRelease"/> that is not newer
        /// than the current OS version, or <see cref="WindowsRelease.Unknown"/>
        /// when no known release matches.
        /// </summary>
        public static WindowsRelease GetCurrentRelease()
        {
            var best = WindowsRelease.Unknown;
            Version current = _info.Version;
            foreach (WindowsRelease release in Enum.GetValues(typeof(WindowsRelease)))
            {
                if (release == WindowsRelease.Unknown)
                    continue;
                if (current >= GetVersion(release))
                    best = release;
                else
                    break;
            }
            return best;
        }
    }
}
