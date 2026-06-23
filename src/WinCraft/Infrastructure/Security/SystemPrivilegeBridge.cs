using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Windows.Win32;
using WinCraft.Infrastructure.Diagnostics;
using WinCraft.Infrastructure.Ipc;
using WinCraft.Infrastructure.Shell;

namespace WinCraft.Infrastructure.Security
{
    /// <summary>
    /// Runs one request through a short-lived SYSTEM execute process.
    /// </summary>
    internal static class SystemPrivilegeBridge
    {
        public static CommandResult Execute(ElevatedCommandRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var resultPipeName = string.Format(
                "WinCraft.System.Result.{0}.{1}",
                PInvoke.GetCurrentProcessId(),
                Guid.NewGuid().ToString("N"));
            var requestPipeName = string.Format(
                "WinCraft.System.Request.{0}.{1}",
                PInvoke.GetCurrentProcessId(),
                Guid.NewGuid().ToString("N"));

            using var resultPipeHandle = ElevatedAgentPipeServer.Create(resultPipeName);
            using var requestPipeHandle = ElevatedAgentPipeServer.Create(requestPipeName);

            var executablePath = ProcessElevation.GetCurrentProcessPath();
            string[] args =
            {
                ElevatedAgentArguments.SystemExecuteMode,
                ElevatedAgentArguments.PipeName,
                resultPipeName,
                ElevatedAgentArguments.RequestPipeName,
                requestPipeName,
                ElevatedAgentArguments.RequestId,
                request.RequestId ?? string.Empty
            };

            if (!TokenProcessLauncher.TryStartProcessFromTrustedSource(
                "winlogon.exe",
                activeSessionOnly: true,
                executablePath,
                args,
                useActiveSessionId: false,
                out Process systemProcess))
            {
                return CommandResult.Failure(
                    PrivilegeErrorCodes.AgentStartFailed,
                    "The SYSTEM execute process could not be started.",
                    request.RequestId);
            }

            using (systemProcess)
            {
                try
                {
                    ElevatedAgentPipeServer.WaitForConnection(requestPipeHandle);
                    ValidateConnectedClientProcess(requestPipeHandle, systemProcess.Id);
                    PipeMessageIO.WriteMessage(requestPipeHandle, request);

                    ElevatedAgentPipeServer.WaitForConnection(resultPipeHandle);
                    ValidateConnectedClientProcess(resultPipeHandle, systemProcess.Id);
                    var result = PipeMessageIO.ReadMessage<CommandResult>(
                        resultPipeHandle,
                        "The SYSTEM execute process closed the pipe unexpectedly.");

                    if (!string.Equals(result?.RequestId, request.RequestId, StringComparison.Ordinal))
                    {
                        return CommandResult.Failure(
                            PrivilegeErrorCodes.UnexpectedRequestId,
                            "The SYSTEM execute process returned an unexpected request identifier.",
                            request.RequestId);
                    }

                    return result;
                }
                catch (Exception exception) when (
                    exception is Win32Exception
                    || exception is TimeoutException
                    || exception is InvalidOperationException)
                {
                    Log.Error(exception, "SYSTEM execute process failed.");
                    return CommandResult.Failure(
                        PrivilegeErrorCodes.AgentStartFailed,
                        exception.Message,
                        request.RequestId);
                }
            }
        }

        public static int RunSystemExecute(string[] args)
        {
            var pipeName = CommandLineArguments.GetValue(args, ElevatedAgentArguments.PipeName);
            var requestPipeName = CommandLineArguments.GetValue(args, ElevatedAgentArguments.RequestPipeName);
            var requestId = CommandLineArguments.GetValue(args, ElevatedAgentArguments.RequestId);

            try
            {
                var request = ReceiveRequest(requestPipeName);
                if (request == null)
                {
                    TrySendFailure(
                        pipeName,
                        PrivilegeErrorCodes.InvalidRequest,
                        "The SYSTEM execute process received an empty request.",
                        requestId);
                    return 1;
                }

                var result = ElevatedOperationExecutor.ExecuteLocal(request);
                SendResult(pipeName, result);
                return result.Succeeded ? 0 : 1;
            }
            catch (Exception exception) when (
                exception is Win32Exception
                || exception is TimeoutException
                || exception is InvalidOperationException)
            {
                TrySendFailure(
                    pipeName,
                    PrivilegeErrorCodes.AgentStartFailed,
                    exception.Message,
                    requestId);
                return 1;
            }
        }

        private static void SendResult(string pipeName, CommandResult result)
        {
            using var pipeHandle = PipeClientConnection.Open(pipeName);
            PipeMessageIO.WriteMessage(pipeHandle, result);
        }

        private static void TrySendFailure(
            string pipeName,
            string errorCode,
            string errorMessage,
            string requestId)
        {
            try
            {
                SendResult(pipeName, CommandResult.Failure(errorCode, errorMessage, requestId));
            }
            catch (Exception sendException) when (
                sendException is Win32Exception
                || sendException is TimeoutException
                || sendException is InvalidOperationException)
            {
                Log.Error(sendException, "Failed to send SYSTEM bridge failure result.");
            }
        }

        private static ElevatedCommandRequest ReceiveRequest(string pipeName)
        {
            using var pipeHandle = PipeClientConnection.Open(pipeName);
            return PipeMessageIO.ReadMessage<ElevatedCommandRequest>(
                pipeHandle,
                "The request pipe closed before the SYSTEM request arrived.");
        }

        private static void ValidateConnectedClientProcess(SafeFileHandle pipeHandle, int expectedProcessId)
        {
            if (!PInvoke.GetNamedPipeClientProcessId(pipeHandle, out uint actualProcessId))
                throw new Win32Exception(Marshal.GetLastWin32Error());

            if (actualProcessId != expectedProcessId)
            {
                throw new InvalidOperationException(
                    string.Format(
                        "The pipe was connected by an unexpected process. Expected pid={0}, actual pid={1}.",
                        expectedProcessId,
                        actualProcessId));
            }
        }
    }
}
