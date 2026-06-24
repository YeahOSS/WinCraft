using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using WinCraft.Infrastructure.Shell;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;

namespace WinCraft.Infrastructure.Security
{
    /// <summary>
    /// Provides account, token, and relaunch helpers for privilege routing.
    /// </summary>
    internal static class ProcessElevation
    {
        /// <summary>
        /// Returns whether the current process is already running elevated.
        /// </summary>
        public static bool IsCurrentProcessElevated()
        {
            return GetCurrentProcessElevationState() != ProcessElevationState.Standard;
        }

        public static uint GetCurrentProcessId()
        {
            return PInvoke.GetCurrentProcessId();
        }

        internal static ProcessElevationState GetCurrentProcessElevationState()
        {
            var isCurrentTokenAdministrator = IsCurrentTokenAdministrator();
            using var tokenHandle = OpenCurrentProcessToken();
            if (tokenHandle == null)
                return ClassifyElevationState(isCurrentTokenAdministrator, null);

            if (TryGetTokenElevationKind(tokenHandle, out TokenElevationKind elevationKind))
                return ClassifyElevationState(isCurrentTokenAdministrator, elevationKind);

            return ClassifyElevationState(isCurrentTokenAdministrator, null);
        }

        internal static ProcessElevationState ClassifyElevationState(
            bool isCurrentTokenAdministrator,
            TokenElevationKind? elevationKind)
        {
            if (!isCurrentTokenAdministrator)
                return ProcessElevationState.Standard;

            if (!elevationKind.HasValue)
                return ProcessElevationState.FullAdministrator;

            return elevationKind.Value switch
            {
                TokenElevationKind.Full => ProcessElevationState.SplitTokenElevated,
                TokenElevationKind.Default => ProcessElevationState.FullAdministrator,
                _ => ProcessElevationState.Standard
            };
        }

        /// <summary>
        /// Restarts the current executable as administrator.
        /// Returns false when the user cancels the UAC prompt.
        /// </summary>
        public static bool TryRelaunchElevated(string[] args, out Process elevatedProcess)
        {
            var executablePath = GetCurrentProcessPath();
            var startInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                Arguments = ShellCommandLine.BuildArgumentString(args),
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = Path.GetDirectoryName(executablePath)
            };

            elevatedProcess = null;

            try
            {
                elevatedProcess = Process.Start(startInfo);
                return elevatedProcess != null;
            }
            catch (Win32Exception exception)
            {
                if (exception.NativeErrorCode == (int)WIN32_ERROR.ERROR_CANCELLED)
                    return false;

                throw;
            }
        }

        public static bool TryLaunchUnelevatedFromShell(string[] args, out Process uiProcess)
        {
            var executablePath = GetCurrentProcessPath();
            return TokenProcessLauncher.TryStartProcessFromShellToken(executablePath, args, out uiProcess);
        }

        /// <summary>
        /// Returns the full path of the current process executable
        /// via <c>GetModuleFileName(NULL)</c> rather than the BCL
        /// <see cref="Process.MainModule"/> which allocates a
        /// <see cref="ProcessModule"/> and may throw
        /// <see cref="Win32Exception"/> on access-denied.
        /// </summary>
        internal static unsafe string GetCurrentProcessPath()
        {
            const int initialBufferLength = 260;
            var bufferLength = initialBufferLength;

            while (true)
            {
                char[] buffer = new char[bufferLength];
                fixed (char* pBuffer = buffer)
                {
                    uint len = PInvoke.GetModuleFileName(null, new PWSTR(pBuffer), (uint)buffer.Length);
                    if (len == 0)
                        throw new Win32Exception(Marshal.GetLastWin32Error());

                    if (len < buffer.Length - 1)
                        return new string(buffer, 0, (int)len);
                }

                checked
                {
                    bufferLength *= 2;
                }
            }
        }

        private static SafeFileHandle OpenCurrentProcessToken()
        {
            using var processHandle = PInvoke.GetCurrentProcess_SafeHandle();
            if (!PInvoke.OpenProcessToken(processHandle, TOKEN_ACCESS_MASK.TOKEN_QUERY, out SafeFileHandle tokenHandle))
                return null;

            return tokenHandle;
        }

        private static bool IsCurrentTokenAdministrator()
        {
            try
            {
                return PInvoke.IsUserAnAdmin();
            }
            catch
            {
                return false;
            }
        }

        private static unsafe bool TryGetTokenElevationKind(SafeHandle tokenHandle, out TokenElevationKind elevationKind)
        {
            // Use stack-allocated int to receive the DWORD-sized elevation type value.
            // Marshal.SizeOf fails on the CsWin32-generated TOKEN_ELEVATION_TYPE type,
            // but the underlying data is always a 4-byte DWORD.
            int elevationTypeValue = 0;

            if (!PInvoke.GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenElevationType, (void*)(&elevationTypeValue), sizeof(int), out _))
            {
                elevationKind = TokenElevationKind.Default;
                return false;
            }

            return TryMapTokenElevationKind((TOKEN_ELEVATION_TYPE)elevationTypeValue, out elevationKind);
        }

        private static bool TryMapTokenElevationKind(
            TOKEN_ELEVATION_TYPE elevationType,
            out TokenElevationKind elevationKind)
        {
            if (elevationType == TOKEN_ELEVATION_TYPE.TokenElevationTypeDefault)
            {
                elevationKind = TokenElevationKind.Default;
                return true;
            }

            if (elevationType == TOKEN_ELEVATION_TYPE.TokenElevationTypeFull)
            {
                elevationKind = TokenElevationKind.Full;
                return true;
            }

            if (elevationType == TOKEN_ELEVATION_TYPE.TokenElevationTypeLimited)
            {
                elevationKind = TokenElevationKind.Limited;
                return true;
            }

            elevationKind = TokenElevationKind.Default;
            return false;
        }
    }
}
