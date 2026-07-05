using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Threading;
using WinCraft.Infrastructure.Diagnostics;
using WinCraft.Infrastructure.Ipc;

namespace WinCraft.Infrastructure.Security
{
    /// <summary>
    /// Owns the UI-side pipe server and reaches a persistent privileged host.
    /// </summary>
    internal sealed class ElevatedAgentController : IDisposable
    {
        private readonly object _lock = new();
        private readonly string _pipeName;
        private readonly int _uiProcessId;
        private readonly bool _attachOnly;
        private readonly bool _ownsAgentProcess;
        private Process _agentProcess;
        private int? _agentProcessId;
        private SafeFileHandle _pipeHandle;
        private bool _agentRunning;
        private bool _agentConnected;
        private bool _disposed;
        private AgentConnectionState _state;

        public ElevatedAgentController()
            : this(null, null, attachOnly: false)
        {
        }

        public ElevatedAgentController(int? attachedAgentPid, string pipeName, bool attachOnly)
        {
            _pipeName = string.IsNullOrEmpty(pipeName)
                ? string.Format("WinCraft.ElevatedAgent.{0}", PInvoke.GetCurrentProcessId())
                : pipeName;
            _uiProcessId = (int)PInvoke.GetCurrentProcessId();
            _attachOnly = attachOnly;
            _ownsAgentProcess = !attachedAgentPid.HasValue;
            _state = AgentConnectionState.Idle;

            if (attachedAgentPid.HasValue)
            {
                _agentProcessId = attachedAgentPid.Value;
                _agentRunning = ProcessMightBeRunning(attachedAgentPid.Value);
                if (!_agentRunning)
                {
                    // The advertised host may have exited before the UI finished
                    // starting. Leave the controller unattached so the caller can
                    // surface a normal "host unavailable" failure instead of
                    // crashing during startup.
                    _agentProcessId = null;
                }
            }
        }

        /// <summary>
        /// Current lifecycle state of the elevated agent connection.
        /// Safe to read from any thread.
        /// </summary>
        public AgentConnectionState State
        {
            get
            {
                lock (_lock)
                    return _state;
            }
        }

        public CommandResult Execute(ElevatedCommandRequest request)
        {
            if (string.IsNullOrEmpty(request?.OperationName))
            {
                return CommandResult.Failure(
                    PrivilegeErrorCodes.InvalidRequest,
                    "The elevated request is missing an operation name.",
                    request?.RequestId);
            }

            var isShutdown = string.Equals(
                request.OperationName,
                ElevatedOperations.Shutdown,
                StringComparison.OrdinalIgnoreCase);

            lock (_lock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(ElevatedAgentController));

                Log.Debug($"Processing elevated operation '{request.OperationName}' (id={request.RequestId}).");

                try
                {
                    if (!EnsureAgentRunning())
                    {
                        return CommandResult.Failure(
                            PrivilegeErrorCodes.AgentStartCancelled,
                            "The elevated agent start was cancelled.",
                            request.RequestId);
                    }

                    PipeMessageIO.WriteMessage(_pipeHandle, request);
                    var result = PipeMessageIO.ReadMessage<CommandResult>(
                        _pipeHandle,
                        "The elevated agent closed the pipe unexpectedly.");

                    CompleteRequestLifecycle(isShutdown);
                    return result;
                }
                catch (Win32Exception)
                {
                    Log.Warn("Elevated agent pipe communication failed; will relaunch on next request.");
                    ResetRunningState();
                    return CommandResult.Failure(
                        PrivilegeErrorCodes.AgentUnavailable,
                        "The elevated agent is unavailable.",
                        request.RequestId);
                }
                catch (InvalidOperationException exception)
                {
                    Log.Error(exception, "Elevated agent protocol error.");
                    ResetRunningState();
                    return CommandResult.Failure(
                        PrivilegeErrorCodes.EmptyAgentResponse,
                        exception.Message,
                        request.RequestId);
                }
                catch (TimeoutException)
                {
                    Log.Warn("Elevated agent connection timed out after all retries.");
                    ResetRunningState();
                    return CommandResult.Failure(
                        PrivilegeErrorCodes.AgentStartFailed,
                        "The elevated agent did not connect within the timeout period.",
                        request.RequestId);
                }
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposed)
                    return;

                _disposed = true;
                _state = AgentConnectionState.Disposed;

                if (_ownsAgentProcess)
                {
                    try
                    {
                        TryShutdownAgent();
                        _agentProcess?.WaitForExit(2000);
                    }
                    catch
                    {
                        // Best-effort shutdown only.
                    }

                    if (_agentProcess != null && IsAgentProcessHandleRunning())
                    {
                        try
                        {
                            Log.Warn("Elevated agent did not exit gracefully; force-killing.");
                            _agentProcess.Kill();
                        }
                        catch
                        {
                            // The process may already be exiting.
                        }
                    }
                }

                _pipeHandle?.Dispose();
                _pipeHandle = null;
                _agentProcess?.Dispose();
                _agentProcess = null;
                _agentRunning = false;
                _agentConnected = false;
            }
        }

        private bool EnsureAgentRunning()
        {
            if (_agentRunning && _agentConnected && IsAgentProcessRunning())
                return true;

            if (!_agentRunning || !IsAgentProcessRunning())
            {
                _agentConnected = false;
                _agentRunning = false;
                _agentProcess?.Dispose();
                _agentProcess = null;
                _agentProcessId = null;

                if (_attachOnly)
                    throw new InvalidOperationException("The attached elevated host is no longer running.");

                var arguments = new[]
                {
                    ElevatedAgentArguments.ElevatedAgentMode,
                    ElevatedAgentArguments.PipeName,
                    _pipeName,
                    ElevatedAgentArguments.UiPid,
                    _uiProcessId.ToString()
                };

                Log.Info("Launching elevated agent via UAC...");
                _state = AgentConnectionState.Launching;
                if (!ProcessElevation.TryRelaunchElevated(arguments, out _agentProcess) || _agentProcess == null)
                {
                    Log.Warn("Elevated agent launch was cancelled or failed.");
                    _state = AgentConnectionState.Idle;
                    _pipeHandle?.Dispose();
                    _pipeHandle = null;
                    return false;
                }

                _agentRunning = true;
                _agentProcessId = _agentProcess.Id;
                Log.Info($"Elevated agent launched (pid={_agentProcess.Id}, pipe={_pipeName}).");
            }

            if (_pipeHandle == null || _pipeHandle.IsInvalid)
                _pipeHandle = ElevatedAgentPipeServer.Create(_pipeName);

            const int maxConnectionAttempts = 3;
            _state = AgentConnectionState.WaitingForConnection;
            Log.Info("Waiting for elevated agent to connect to the named pipe...");
            for (int attempt = 0; attempt < maxConnectionAttempts; attempt++)
            {
                try
                {
                    ElevatedAgentPipeServer.WaitForConnection(_pipeHandle);
                    break;
                }
                catch (TimeoutException)
                {
                    if (!IsAgentProcessRunning())
                    {
                        Log.Warn("Elevated agent process exited while waiting for connection.");
                        throw;
                    }

                    if (attempt == maxConnectionAttempts - 1)
                    {
                        Log.Warn("All connection attempts to the elevated agent failed.");
                        throw;
                    }

                    Log.Warn(
                        $"Elevated agent connection attempt {attempt + 1}/{maxConnectionAttempts} timed out; retrying...");
                    _pipeHandle?.Dispose();
                    _pipeHandle = ElevatedAgentPipeServer.Create(_pipeName);
                }
            }

            if (!PInvoke.GetNamedPipeClientProcessId(_pipeHandle, out uint connectedProcessId))
                throw new Win32Exception(Marshal.GetLastWin32Error());

            if (!_agentProcessId.HasValue || connectedProcessId != _agentProcessId.Value)
            {
                Log.Warn(
                    $"Pipe connected by unexpected process (expected={_agentProcessId?.ToString() ?? "unknown"}, actual={connectedProcessId}).");
                ResetRunningState();
                throw new InvalidOperationException(
                    "The elevated agent pipe was connected by an unexpected process.");
            }

            _agentConnected = true;
            _state = AgentConnectionState.Connected;
            Log.Info($"Elevated agent connected and verified (pid={connectedProcessId}).");
            return true;
        }

        private void TryShutdownAgent()
        {
            if (!_agentRunning || !IsAgentProcessRunning())
                return;

            if (!_agentConnected && !TryReconnectToRunningAgent())
                return;

            try
            {
                Log.Info("Sending shutdown request to elevated agent.");
                var shutdownRequest = new ElevatedCommandRequest
                {
                    OperationName = ElevatedOperations.Shutdown,
                    PrivilegeLevel = PrivilegeLevel.Administrator,
                    RequestId = Guid.NewGuid().ToString("N")
                };
                PipeMessageIO.WriteMessage(_pipeHandle, shutdownRequest);
                PipeMessageIO.ReadMessage<CommandResult>(
                    _pipeHandle,
                    "The elevated agent closed the pipe unexpectedly.");
                _agentRunning = false;
                _agentConnected = false;
            }
            catch
            {
                // The host may already be terminating.
            }
        }

        private bool TryReconnectToRunningAgent()
        {
            if (!IsAgentProcessRunning())
                return false;

            try
            {
                if (_pipeHandle == null || _pipeHandle.IsInvalid)
                    _pipeHandle = ElevatedAgentPipeServer.Create(_pipeName);

                ElevatedAgentPipeServer.WaitForConnection(_pipeHandle);

                if (!PInvoke.GetNamedPipeClientProcessId(_pipeHandle, out uint connectedProcessId))
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                if (!_agentProcessId.HasValue || connectedProcessId != _agentProcessId.Value)
                    throw new InvalidOperationException(
                        "The elevated agent pipe was connected by an unexpected process.");

                _agentConnected = true;
                return true;
            }
            catch
            {
                _agentConnected = false;
                return false;
            }
        }

        private void CompleteRequestLifecycle(bool isShutdown)
        {
            if (isShutdown)
            {
                _agentRunning = false;
                _agentConnected = false;
                _state = AgentConnectionState.Idle;
                return;
            }

            try
            {
                PInvoke.DisconnectNamedPipe(_pipeHandle);
            }
            catch (Win32Exception)
            {
                // The client side may already be closed.
            }

            _agentConnected = false;
            _state = AgentConnectionState.Idle;
        }

        private void ResetRunningState()
        {
            _agentRunning = false;
            _agentConnected = false;
            _state = AgentConnectionState.Idle;
            _pipeHandle?.Dispose();
            _pipeHandle = null;
        }

        private bool IsAgentProcessRunning()
        {
            if (!_agentRunning || !_agentProcessId.HasValue)
                return false;

            if (_agentProcess == null)
                return ProcessMightBeRunning(_agentProcessId.Value);

            return IsAgentProcessHandleRunning();
        }

        private bool IsAgentProcessHandleRunning()
        {
            if (_agentProcess == null)
                return false;

            try
            {
                return !_agentProcess.HasExited;
            }
            catch (Win32Exception exception)
            {
                Log.Debug(
                    $"Unable to query elevated agent process state (pid={_agentProcessId?.ToString() ?? "unknown"}): {exception.Message}");
                return _agentProcessId.HasValue && ProcessMightBeRunning(_agentProcessId.Value);
            }
            catch (InvalidOperationException)
            {
                return _agentProcessId.HasValue && ProcessMightBeRunning(_agentProcessId.Value);
            }
        }

        private static bool ProcessMightBeRunning(int processId)
        {
            if (processId <= 0)
                return false;

            using var processHandle = PInvoke.OpenProcess_SafeHandle(
                PROCESS_ACCESS_RIGHTS.PROCESS_SYNCHRONIZE,
                false,
                (uint)processId);

            if (processHandle != null && !processHandle.IsInvalid)
            {
                // OpenProcess can succeed for an exited process object while handles still exist.
                var waitResult = PInvoke.WaitForSingleObject(processHandle, 0);
                return waitResult == WAIT_EVENT.WAIT_TIMEOUT;
            }

            var errorCode = Marshal.GetLastWin32Error();
            return errorCode == (int)WIN32_ERROR.ERROR_ACCESS_DENIED;
        }
    }
}
