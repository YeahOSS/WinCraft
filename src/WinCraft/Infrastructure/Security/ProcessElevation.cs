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
    /// Provides account and token elevation checks for startup routing.
    /// </summary>
    internal static class ProcessElevation
    {
        /// <summary>
        /// Returns whether the current logon can acquire an administrator token.
        /// </summary>
        public static bool IsCurrentAccountAdministrator()
        {
            var isCurrentTokenAdministrator = IsCurrentTokenAdministrator();
            using var tokenHandle = OpenCurrentProcessToken();
            if (tokenHandle == null)
                return isCurrentTokenAdministrator;

            if (!TryGetTokenElevationType(tokenHandle, out TOKEN_ELEVATION_TYPE elevationType))
                return isCurrentTokenAdministrator;

            return IsAdministratorElevationType(elevationType)
                || isCurrentTokenAdministrator;
        }

        /// <summary>
        /// Returns whether the current process is already running elevated.
        /// </summary>
        public static bool IsCurrentProcessElevated()
        {
            var isCurrentTokenAdministrator = IsCurrentTokenAdministrator();
            using var tokenHandle = OpenCurrentProcessToken();
            if (tokenHandle == null)
                return isCurrentTokenAdministrator;

            if (TryGetTokenElevation(tokenHandle, out TOKEN_ELEVATION elevation))
                return elevation.TokenIsElevated != 0;

            if (TryGetTokenElevationType(tokenHandle, out TOKEN_ELEVATION_TYPE elevationType))
                return elevationType == TOKEN_ELEVATION_TYPE.TokenElevationTypeFull;

            return isCurrentTokenAdministrator;
        }

        /// <summary>
        /// Restarts the current executable as administrator.
        /// Returns false when the user cancels the UAC prompt.
        /// </summary>
        public static bool TryRelaunchElevated(string[] args, out Process elevatedProcess)
        {
            using var process = Process.GetCurrentProcess();
            var executablePath = process.MainModule.FileName;
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

        private static SafeFileHandle OpenCurrentProcessToken()
        {
            using var process = Process.GetCurrentProcess();
            // ownsHandle: false — the process handle is owned by the OS; we borrow it here.
            using var processHandle = new SafeFileHandle(process.Handle, ownsHandle: false);
            if (!PInvoke.OpenProcessToken(processHandle, TOKEN_ACCESS_MASK.TOKEN_QUERY, out SafeFileHandle tokenHandle))
                return null;

            return tokenHandle;
        }

        private static bool IsAdministratorElevationType(TOKEN_ELEVATION_TYPE elevationType)
        {
            return elevationType
                is TOKEN_ELEVATION_TYPE.TokenElevationTypeLimited
                or TOKEN_ELEVATION_TYPE.TokenElevationTypeFull;
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

        private static unsafe bool TryGetTokenElevation(SafeHandle tokenHandle, out TOKEN_ELEVATION elevation)
        {
            var elevationSize = Marshal.SizeOf(typeof(TOKEN_ELEVATION));
            var elevationPointer = Marshal.AllocHGlobal(elevationSize);

            try
            {
                if (!PInvoke.GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenElevation, (void*)elevationPointer, (uint)elevationSize, out _))
                {
                    elevation = new TOKEN_ELEVATION();
                    return false;
                }

                elevation = (TOKEN_ELEVATION)Marshal.PtrToStructure(elevationPointer, typeof(TOKEN_ELEVATION));
                return true;
            }
            finally
            {
                Marshal.FreeHGlobal(elevationPointer);
            }
        }

        private static unsafe bool TryGetTokenElevationType(SafeHandle tokenHandle, out TOKEN_ELEVATION_TYPE elevationType)
        {
            // Use stack-allocated int to receive the DWORD-sized elevation type value.
            // Marshal.SizeOf fails on the CsWin32-generated TOKEN_ELEVATION_TYPE type,
            // but the underlying data is always a 4-byte DWORD.
            int elevationTypeValue = 0;

            if (!PInvoke.GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenElevationType, (void*)(&elevationTypeValue), sizeof(int), out _))
            {
                elevationType = TOKEN_ELEVATION_TYPE.TokenElevationTypeDefault;
                return false;
            }

            elevationType = (TOKEN_ELEVATION_TYPE)elevationTypeValue;
            return true;
        }
    }
}
