using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using WinCraft.Compatibility;
using WinCraft.Infrastructure.Diagnostics;
using WinCraft.Infrastructure.Shell;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.System.Services;
using Windows.Win32.System.Threading;

namespace WinCraft.Infrastructure.Security
{
    /// <summary>
    /// Starts processes under the primary token of another running process.
    /// </summary>
    internal static class TokenProcessLauncher
    {
        private const string SeDebugPrivilege = "SeDebugPrivilege";
        private const string SeTcbPrivilege = "SeTcbPrivilege";
        private const string WinlogonProcessName = "winlogon";
        private const string TrustedInstallerProcessName = "TrustedInstaller";
        private const string TrustedInstallerServiceName = "TrustedInstaller";
        private const uint ScManagerConnect = 0x0001;
        private const uint ServiceQueryStatus = 0x0004;

        /// <summary>
        /// Restricted token access mask for <see cref="DuplicateTokenEx"/>.
        /// Includes only the rights actually required: duplicate, query,
        /// impersonate, assign primary, adjust session ID, and adjust default.
        /// </summary>
        private const TOKEN_ACCESS_MASK RequiredTokenAccess =
            TOKEN_ACCESS_MASK.TOKEN_DUPLICATE
            | TOKEN_ACCESS_MASK.TOKEN_QUERY
            | TOKEN_ACCESS_MASK.TOKEN_IMPERSONATE
            | TOKEN_ACCESS_MASK.TOKEN_ASSIGN_PRIMARY
            | TOKEN_ACCESS_MASK.TOKEN_ADJUST_SESSIONID
            | TOKEN_ACCESS_MASK.TOKEN_ADJUST_DEFAULT;

        public static bool TryStartProcessFromShellToken(
            string executablePath,
            string[] args,
            out Process process)
        {
            process = null;

            var currentSessionId = GetCurrentSessionId();
            foreach (var explorerProcess in Process.GetProcessesByName("explorer"))
            {
                using (explorerProcess)
                {
                    try
                    {
                        if (explorerProcess.SessionId != currentSessionId)
                            continue;

                        if (TryStartProcessFromTokenSource(
                            explorerProcess.Id,
                            explorerProcess.SessionId,
                            executablePath,
                            args,
                            Path.GetDirectoryName(executablePath),
                            false,
                            useInteractiveDesktop: true,
                            enableDebugPrivilege: false,
                            out process))
                        {
                            return true;
                        }
                    }
                    catch (Exception exception) when (IsRecoverableLaunchException(exception))
                    {
                        Log.Debug(
                            $"Skipping shell token source pid={GetProcessIdOrUnknown(explorerProcess)}: {exception.Message}");
                    }
                }
            }

            return false;
        }

        public static bool TryStartProcessFromTrustedSource(
            string processName,
            bool activeSessionOnly,
            string executablePath,
            string[] args,
            bool useActiveSessionId,
            out Process process)
        {
            process = null;
            var targetProcessName = Path.GetFileNameWithoutExtension(processName ?? string.Empty);
            if (string.IsNullOrEmpty(targetProcessName))
                return false;

            var currentSessionId = GetCurrentSessionId();
            var expectedProcessId = GetExpectedTrustedSourceProcessId(targetProcessName);
            if (IsTrustedInstallerSource(targetProcessName) && !expectedProcessId.HasValue)
                return false;

            foreach (var sourceProcess in Process.GetProcessesByName(targetProcessName))
            {
                using (sourceProcess)
                {
                    try
                    {
                        if (!IsExpectedTrustedSourceProcess(
                            sourceProcess,
                            targetProcessName,
                            activeSessionOnly,
                            currentSessionId,
                            expectedProcessId))
                        {
                            continue;
                        }

                        if (TryStartProcessFromTokenSource(
                            sourceProcess.Id,
                            sourceProcess.SessionId,
                            executablePath,
                            args,
                            Path.GetDirectoryName(executablePath),
                            useActiveSessionId,
                            useInteractiveDesktop: false,
                            enableDebugPrivilege: true,
                            out process))
                        {
                            return true;
                        }
                    }
                    catch (Exception exception) when (IsRecoverableLaunchException(exception))
                    {
                        Log.Debug(
                            $"Skipping trusted token source pid={GetProcessIdOrUnknown(sourceProcess)}: {exception.Message}");
                    }
                }
            }

            return false;
        }

        private static int GetCurrentSessionId()
        {
            using var currentProcess = Process.GetCurrentProcess();
            return currentProcess.SessionId;
        }

        private static int? GetExpectedTrustedSourceProcessId(string targetProcessName)
        {
            if (!IsTrustedInstallerSource(targetProcessName))
                return null;

            return TryGetServiceProcessId(TrustedInstallerServiceName, out int serviceProcessId)
                ? serviceProcessId
                : null;
        }

        private static bool IsExpectedTrustedSourceProcess(
            Process sourceProcess,
            string targetProcessName,
            bool activeSessionOnly,
            int currentSessionId,
            int? expectedProcessId)
        {
            if (sourceProcess == null)
                return false;

            if (activeSessionOnly && sourceProcess.SessionId != currentSessionId)
                return false;

            if (expectedProcessId.HasValue && sourceProcess.Id != expectedProcessId.Value)
                return false;

            if (IsWinlogonSource(targetProcessName))
            {
                return IsExpectedProcessImagePath(
                    sourceProcess.Id,
                    PathCompat.Combine(GetWindowsDirectory(), "System32", "winlogon.exe"));
            }

            if (IsTrustedInstallerSource(targetProcessName))
            {
                return IsExpectedProcessImagePath(
                    sourceProcess.Id,
                    PathCompat.Combine(GetWindowsDirectory(), "servicing", "TrustedInstaller.exe"));
            }

            return false;
        }

        private static bool IsWinlogonSource(string targetProcessName)
        {
            return string.Equals(targetProcessName, WinlogonProcessName, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsTrustedInstallerSource(string targetProcessName)
        {
            return string.Equals(targetProcessName, TrustedInstallerProcessName, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetWindowsDirectory()
        {
            var systemRoot = Environment.GetEnvironmentVariable("SystemRoot");
            if (!string.IsNullOrEmpty(systemRoot))
                return systemRoot;

            var systemDirectory = Environment.GetFolderPath(Environment.SpecialFolder.System);
            return Directory.GetParent(systemDirectory)?.FullName ?? systemDirectory;
        }

        private static bool IsExpectedProcessImagePath(int processId, string expectedPath)
        {
            if (string.IsNullOrEmpty(expectedPath)
                || !TryGetProcessImagePath(processId, out string actualPath))
            {
                return false;
            }

            return string.Equals(
                Path.GetFullPath(actualPath),
                Path.GetFullPath(expectedPath),
                StringComparison.OrdinalIgnoreCase);
        }

        private static unsafe bool TryGetProcessImagePath(int processId, out string imagePath)
        {
            imagePath = null;

            using var processHandle = PInvoke.OpenProcess_SafeHandle(
                PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION,
                false,
                (uint)processId);

            if (processHandle == null || processHandle.IsInvalid)
                return false;

            var bufferLength = PInvoke.MAX_PATH;
            while (true)
            {
                char[] buffer = new char[bufferLength];
                uint writtenLength = (uint)buffer.Length;
                fixed (char* pBuffer = buffer)
                {
                    if (PInvoke.QueryFullProcessImageName(
                        processHandle,
                        PROCESS_NAME_FORMAT.PROCESS_NAME_WIN32,
                        new PWSTR(pBuffer),
                        ref writtenLength))
                    {
                        imagePath = new string(buffer, 0, (int)writtenLength);
                        return true;
                    }
                }

                var errorCode = Marshal.GetLastWin32Error();
                if (errorCode != (int)WIN32_ERROR.ERROR_INSUFFICIENT_BUFFER)
                    return false;

                checked
                {
                    bufferLength *= 2;
                }
            }
        }

        private static unsafe bool TryGetServiceProcessId(string serviceName, out int processId)
        {
            processId = 0;

            try
            {
                using var serviceControlManager = PInvoke.OpenSCManager((string)null, null, ScManagerConnect);
                if (serviceControlManager == null || serviceControlManager.IsInvalid)
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                using var service = PInvoke.OpenService(
                    serviceControlManager,
                    serviceName,
                    ServiceQueryStatus);
                if (service == null || service.IsInvalid)
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                var status = new SERVICE_STATUS_PROCESS();
                if (!PInvoke.QueryServiceStatusEx(
                        service,
                        SC_STATUS_TYPE.SC_STATUS_PROCESS_INFO,
                        (byte*)&status,
                        (uint)Marshal.SizeOf(typeof(SERVICE_STATUS_PROCESS)),
                        out _))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                if (status.dwProcessId == 0)
                    return false;

                processId = (int)status.dwProcessId;
                return true;
            }
            catch (Win32Exception exception)
            {
                Log.Debug($"Failed to query service pid for '{serviceName}': {exception.Message}");
                return false;
            }
        }

        private static unsafe bool TryStartProcessFromTokenSource(
            int processId,
            int sourceSessionId,
            string executablePath,
            string[] args,
            string workingDirectory,
            bool useActiveSessionId,
            bool useInteractiveDesktop,
            bool enableDebugPrivilege,
            out Process process)
        {
            process = null;

            if (string.IsNullOrEmpty(executablePath))
                throw new ArgumentException("The executable path is required.", nameof(executablePath));

            SafeFileHandle sourceProcessHandle = null;
            SafeFileHandle sourceTokenHandle = null;
            SafeFileHandle duplicateTokenHandle = null;
            TokenPrivilegeScope debugPrivilegeScope = null;
            TokenPrivilegeScope tcbPrivilegeScope = null;
            void* environmentBlock = null;

            try
            {
                if (enableDebugPrivilege)
                    debugPrivilegeScope = EnablePrivilege(SeDebugPrivilege);

                sourceProcessHandle = PInvoke.OpenProcess_SafeHandle(
                    PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_INFORMATION | PROCESS_ACCESS_RIGHTS.PROCESS_CREATE_PROCESS,
                    false,
                    (uint)processId);

                if (sourceProcessHandle == null || sourceProcessHandle.IsInvalid)
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                if (!PInvoke.OpenProcessToken(
                        sourceProcessHandle,
                        TOKEN_ACCESS_MASK.TOKEN_DUPLICATE | TOKEN_ACCESS_MASK.TOKEN_QUERY,
                        out sourceTokenHandle))
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                if (!PInvoke.DuplicateTokenEx(
                        sourceTokenHandle,
                        RequiredTokenAccess,
                        null,
                        SECURITY_IMPERSONATION_LEVEL.SecurityIdentification,
                        TOKEN_TYPE.TokenPrimary,
                        out duplicateTokenHandle))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                var activeSessionId = PInvoke.WTSGetActiveConsoleSessionId();
                if (useActiveSessionId && sourceSessionId != activeSessionId)
                {
                    tcbPrivilegeScope = EnablePrivilege(SeTcbPrivilege);
                    try
                    {
                        if (!PInvoke.SetTokenInformation(
                                duplicateTokenHandle,
                                TOKEN_INFORMATION_CLASS.TokenSessionId,
                                &activeSessionId,
                                sizeof(uint)))
                        {
                            throw new Win32Exception(Marshal.GetLastWin32Error());
                        }
                    }
                    finally
                    {
                        tcbPrivilegeScope.Dispose();
                        tcbPrivilegeScope = null;
                    }
                }

                if (!PInvoke.CreateEnvironmentBlock(out environmentBlock, duplicateTokenHandle, false))
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                var commandLine = ShellCommandLine.BuildArgumentString(BuildCommandLineArgs(executablePath, args));
                var creationFlags = PROCESS_CREATION_FLAGS.NORMAL_PRIORITY_CLASS
                    | PROCESS_CREATION_FLAGS.CREATE_UNICODE_ENVIRONMENT;

                var startupInfo = new STARTUPINFOW
                {
                    cb = (uint)Marshal.SizeOf(typeof(STARTUPINFOW))
                };

                if (useInteractiveDesktop)
                {
                    fixed (char* pDesktop = @"winsta0\default")
                    {
                        startupInfo.lpDesktop = new PWSTR(pDesktop);
                        return TryStartProcessWithStartupInfo(
                            duplicateTokenHandle,
                            executablePath,
                            commandLine,
                            creationFlags,
                            environmentBlock,
                            workingDirectory,
                            in startupInfo,
                            out process);
                    }
                }

                return TryStartProcessWithStartupInfo(
                    duplicateTokenHandle,
                    executablePath,
                    commandLine,
                    creationFlags,
                    environmentBlock,
                    workingDirectory,
                    in startupInfo,
                    out process);
            }
            finally
            {
                if (environmentBlock != null)
                    PInvoke.DestroyEnvironmentBlock(environmentBlock);
                tcbPrivilegeScope?.Dispose();
                debugPrivilegeScope?.Dispose();
                duplicateTokenHandle?.Dispose();
                sourceTokenHandle?.Dispose();
                sourceProcessHandle?.Dispose();
            }
        }

        private static unsafe bool TryStartProcessWithStartupInfo(
            SafeFileHandle tokenHandle,
            string executablePath,
            string commandLine,
            PROCESS_CREATION_FLAGS creationFlags,
            void* environmentBlock,
            string workingDirectory,
            in STARTUPINFOW startupInfo,
            out Process process)
        {
            process = null;

            if (!TryCreateProcessWithToken(
                    tokenHandle,
                    executablePath,
                    commandLine,
                    creationFlags,
                    environmentBlock,
                    workingDirectory,
                    in startupInfo,
                    out PROCESS_INFORMATION processInformation)
                && !TryCreateProcessAsUser(
                    tokenHandle,
                    executablePath,
                    commandLine,
                    creationFlags,
                    environmentBlock,
                    workingDirectory,
                    in startupInfo,
                    out processInformation))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            PInvoke.CloseHandle(processInformation.hThread);
            PInvoke.CloseHandle(processInformation.hProcess);

            process = Process.GetProcessById((int)processInformation.dwProcessId);
            return true;
        }

        private static unsafe bool TryCreateProcessWithToken(
            SafeFileHandle tokenHandle,
            string executablePath,
            string commandLine,
            PROCESS_CREATION_FLAGS creationFlags,
            void* environmentBlock,
            string workingDirectory,
            in STARTUPINFOW startupInfo,
            out PROCESS_INFORMATION processInformation)
        {
            var commandLineChars = CreateMutableCommandLineBuffer(commandLine);
            fixed (char* pCommandLine = commandLineChars)
            {
                return PInvoke.CreateProcessWithToken(
                    tokenHandle,
                    CREATE_PROCESS_LOGON_FLAGS.LOGON_WITH_PROFILE,
                    executablePath,
                    new PWSTR(pCommandLine),
                    creationFlags,
                    environmentBlock,
                    workingDirectory,
                    in startupInfo,
                    out processInformation);
            }
        }

        private static unsafe bool TryCreateProcessAsUser(
            SafeFileHandle tokenHandle,
            string executablePath,
            string commandLine,
            PROCESS_CREATION_FLAGS creationFlags,
            void* environmentBlock,
            string workingDirectory,
            in STARTUPINFOW startupInfo,
            out PROCESS_INFORMATION processInformation)
        {
            var commandLineChars = CreateMutableCommandLineBuffer(commandLine);
            fixed (char* pCommandLine = commandLineChars)
            {
                return PInvoke.CreateProcessAsUser(
                    tokenHandle,
                    executablePath,
                    new PWSTR(pCommandLine),
                    null,
                    null,
                    false,
                    creationFlags,
                    environmentBlock,
                    workingDirectory,
                    in startupInfo,
                    out processInformation);
            }
        }

        private static char[] CreateMutableCommandLineBuffer(string commandLine)
        {
            var length = commandLine?.Length ?? 0;
            var commandLineChars = new char[length + 1];
            if (length > 0)
                commandLine.CopyTo(0, commandLineChars, 0, length);

            return commandLineChars;
        }

        private static bool IsRecoverableLaunchException(Exception exception)
        {
            return exception is Win32Exception || exception is InvalidOperationException;
        }

        private static string GetProcessIdOrUnknown(Process process)
        {
            try
            {
                return process?.Id.ToString() ?? "unknown";
            }
            catch (InvalidOperationException)
            {
                return "unknown";
            }
        }

        private static unsafe TokenPrivilegeScope EnablePrivilege(string privilegeName)
        {
            using var processHandle = PInvoke.GetCurrentProcess_SafeHandle();

            if (!PInvoke.OpenProcessToken(
                    processHandle,
                    TOKEN_ACCESS_MASK.TOKEN_ADJUST_PRIVILEGES | TOKEN_ACCESS_MASK.TOKEN_QUERY,
                    out SafeFileHandle tokenHandle))
                throw new Win32Exception(Marshal.GetLastWin32Error());

            var ownsTokenHandle = true;
            try
            {
                if (!PInvoke.LookupPrivilegeValue(null, privilegeName, out LUID luid))
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                var tokenPrivileges = new TOKEN_PRIVILEGES
                {
                    PrivilegeCount = 1,
                    Privileges = default
                };
                tokenPrivileges.Privileges.e0 = new LUID_AND_ATTRIBUTES
                {
                    Luid = luid,
                    Attributes = TOKEN_PRIVILEGES_ATTRIBUTES.SE_PRIVILEGE_ENABLED
                };

                // AdjustTokenPrivileges reports partial assignment via last-error even when it succeeds.
                var previousState = new TOKEN_PRIVILEGES();
                uint previousStateLength = 0;
                PInvoke.SetLastError(WIN32_ERROR.ERROR_SUCCESS);
                if (!PInvoke.AdjustTokenPrivileges(
                        tokenHandle,
                        false,
                        &tokenPrivileges,
                        (uint)TOKEN_PRIVILEGES.SizeOf(1),
                        &previousState,
                        &previousStateLength))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                var lastError = Marshal.GetLastWin32Error();
                if (lastError == (int)WIN32_ERROR.ERROR_NOT_ALL_ASSIGNED)
                {
                    throw new Win32Exception(
                        lastError,
                        string.Format("The current token does not hold {0}.", privilegeName));
                }

                ownsTokenHandle = false;
                return new TokenPrivilegeScope(tokenHandle, previousState);
            }
            finally
            {
                if (ownsTokenHandle)
                    tokenHandle.Dispose();
            }
        }

        private sealed class TokenPrivilegeScope : IDisposable
        {
            private SafeFileHandle _tokenHandle;
            private readonly TOKEN_PRIVILEGES _previousState;

            public TokenPrivilegeScope(SafeFileHandle tokenHandle, TOKEN_PRIVILEGES previousState)
            {
                _tokenHandle = tokenHandle;
                _previousState = previousState;
            }

            public unsafe void Dispose()
            {
                var tokenHandle = _tokenHandle;
                if (tokenHandle == null)
                    return;

                _tokenHandle = null;
                try
                {
                    if (_previousState.PrivilegeCount > 0)
                    {
                        var previousState = _previousState;
                        PInvoke.AdjustTokenPrivileges(
                            tokenHandle,
                            false,
                            &previousState,
                            0,
                            null,
                            null);
                    }
                }
                finally
                {
                    tokenHandle.Dispose();
                }
            }
        }

        private static string[] BuildCommandLineArgs(string executablePath, string[] args)
        {
            var extraCount = args?.Length ?? 0;
            var commandLineArgs = new string[extraCount + 1];
            commandLineArgs[0] = executablePath;

            if (extraCount > 0)
                Array.Copy(args, 0, commandLineArgs, 1, extraCount);

            return commandLineArgs;
        }
    }
}
