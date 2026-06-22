using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Windows.Win32;
using WinCraft.Infrastructure.Diagnostics;
using WinCraft.Infrastructure.Ipc;

namespace WinCraft.Infrastructure.Security
{
    /// <summary>
    /// Launches a persistent elevated agent on first use and reuses it
    /// for subsequent operations so UAC only appears once.
    /// The UI process owns the named pipe server; the elevated agent
    /// connects as a client. This direction avoids the UAC integrity-level
    /// and DACL barriers that block the reverse direction.
    /// </summary>
    internal sealed class ElevatedAgentController : IDisposable
    {
        private readonly object _lock = new();
        private Process _agentProcess;
        private SafeFileHandle _pipeHandle;
        private bool _agentRunning;
        private bool _agentConnected;
        private bool _disposed;

        /// <summary>
        /// Sends a request to the elevated agent and returns the result.
        /// </summary>
        /// <remarks>
        /// This method holds <see cref="_lock"/> during blocking pipe I/O.
        /// The lock serialises all agent access and ensures state consistency
        /// across the start / execute / disconnect lifecycle. Callers offload
        /// long-running operations to background threads so the lock does not
        /// block the UI dispatcher.
        /// </remarks>
        public CommandResult Execute(ElevatedCommandRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.OperationName))
            {
                return CommandResult.Failure(
                    PrivilegeErrorCodes.InvalidRequest,
                    "The elevated request is missing an operation name.");
            }

            var isShutdown = string.Equals(
                request.OperationName,
                ElevatedOperations.Shutdown,
                StringComparison.OrdinalIgnoreCase);

            lock (_lock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(ElevatedAgentController));

                CommandResult result;
                try
                {
                    if (!EnsureAgentRunning())
                    {
                        return CommandResult.Failure(
                            PrivilegeErrorCodes.AgentStartCancelled,
                            "The elevated agent start was cancelled.");
                    }

                    PipeMessageIO.WriteMessage(_pipeHandle, request);
                    result = PipeMessageIO.ReadMessage<CommandResult>(
                        _pipeHandle, "The elevated agent closed the pipe unexpectedly.");
                }
                catch (Win32Exception)
                {
                    Log.Warn("Elevated agent pipe communication failed; will relaunch on next request.");
                    _agentRunning = false;
                    _agentConnected = false;
                    return CommandResult.Failure(
                        PrivilegeErrorCodes.AgentUnavailable,
                        "The elevated agent is unavailable.");
                }
                catch (InvalidOperationException exception)
                {
                    Log.Error(exception, "Elevated agent protocol error.");
                    _agentRunning = false;
                    _agentConnected = false;
                    return CommandResult.Failure(
                        PrivilegeErrorCodes.EmptyAgentResponse,
                        exception.Message);
                }
                catch (TimeoutException)
                {
                    Log.Warn("Elevated agent connection timed out after all retries.");
                    _agentRunning = false;
                    _agentConnected = false;
                    return CommandResult.Failure(
                        PrivilegeErrorCodes.AgentStartFailed,
                        "The elevated agent did not connect within the timeout period.");
                }

                // The request/response cycle succeeded. Handle post-I/O
                // state separately so a failed disconnect cannot discard
                // the valid result.
                if (!isShutdown)
                {
                    try
                    {
                        PInvoke.DisconnectNamedPipe(_pipeHandle);
                    }
                    catch (Win32Exception)
                    {
                        // The agent may have already closed its end.
                        // Let EnsureAgentRunning recreate the pipe on
                        // the next call if the disconnect failed.
                    }
                    _agentConnected = false;
                }
                else
                {
                    _agentRunning = false;
                    _agentConnected = false;
                }

                return result;
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposed)
                    return;

                _disposed = true;

                if (_agentRunning && _agentConnected)
                {
                    try
                    {
                        Log.Info("Sending shutdown request to elevated agent.");
                        var shutdownRequest = new ElevatedCommandRequest
                        {
                            OperationName = ElevatedOperations.Shutdown
                        };
                        PipeMessageIO.WriteMessage(_pipeHandle, shutdownRequest);
                        PipeMessageIO.ReadMessage<CommandResult>(
                            _pipeHandle, "The elevated agent closed the pipe unexpectedly.");
                        _agentProcess?.WaitForExit(2000);
                    }
                    catch
                    {
                        // Agent may already be gone; proceed to kill.
                    }
                }

                if (_agentProcess != null && !_agentProcess.HasExited)
                {
                    try
                    {
                        Log.Warn("Elevated agent did not exit gracefully; force-killing.");
                        _agentProcess.Kill();
                    }
                    catch
                    {
                        // Process may already be dead.
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
            if (_agentRunning && _agentConnected
                && _agentProcess != null && !_agentProcess.HasExited)
                return true;

            // Agent not connected — reconnect if the process is still alive,
            // otherwise launch a new one.
            if (!_agentRunning || _agentProcess == null || _agentProcess.HasExited)
            {
                _agentRunning = false;
                _agentConnected = false;
                _pipeHandle?.Dispose();
                _pipeHandle = null;
                _agentProcess?.Dispose();
                _agentProcess = null;

                using var process = Process.GetCurrentProcess();
                var pipeName = string.Format("WinCraft.ElevatedAgent.{0}", process.Id);
                _pipeHandle = ElevatedAgentPipeServer.Create(pipeName);

                var arguments = new[]
                {
                    ElevatedAgentArguments.ElevatedAgentMode,
                    ElevatedAgentArguments.PipeName,
                    pipeName
                };

                if (!ProcessElevation.TryRelaunchElevated(arguments, out _agentProcess)
                    || _agentProcess == null)
                {
                    Log.Warn("Elevated agent launch was cancelled or failed.");
                    _pipeHandle?.Dispose();
                    _pipeHandle = null;
                    return false;
                }

                Log.Info($"Elevated agent launched (pid={_agentProcess.Id}, pipe={pipeName}).");
                _agentRunning = true;
            }

            // Wait for the elevated agent to connect, with retries.
            // The agent process does not exist until the user accepts the UAC
            // prompt, so the first timeout may fire before the agent has started.
            // Each attempt waits up to the pipe server's connect timeout.
            const int maxConnectionAttempts = 3;
            for (int attempt = 0; attempt < maxConnectionAttempts; attempt++)
            {
                try
                {
                    ElevatedAgentPipeServer.WaitForConnection(_pipeHandle);
                    break;
                }
                catch (TimeoutException)
                {
                    // If the agent process has already exited, the user cancelled
                    // UAC or the agent crashed — propagate the failure immediately.
                    if (_agentProcess != null && _agentProcess.HasExited)
                        throw;

                    if (attempt == maxConnectionAttempts - 1)
                        throw;

                    Log.Info("Elevated agent connection timed out; retrying.");
                    _pipeHandle?.Dispose();
                    _pipeHandle = ElevatedAgentPipeServer.Create(
                        string.Format("WinCraft.ElevatedAgent.{0}", Process.GetCurrentProcess().Id));
                }
            }

            // Verify that the connecting process is the agent we launched.
            if (!PInvoke.GetNamedPipeClientProcessId(_pipeHandle, out uint connectedProcessId))
                throw new Win32Exception(Marshal.GetLastWin32Error());

            if (connectedProcessId != _agentProcess.Id)
            {
                _agentRunning = false;
                _agentConnected = false;
                throw new InvalidOperationException(
                    "The elevated agent pipe was connected by an unexpected process.");
            }

            Log.Info($"Elevated agent connected (pid={connectedProcessId}).");
            _agentConnected = true;
            return true;
        }
    }
}
