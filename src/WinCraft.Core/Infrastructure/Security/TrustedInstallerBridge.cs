using System;
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Win32.SafeHandles;
using Windows.Win32;
using WinCraft.Infrastructure.Diagnostics;
using WinCraft.Infrastructure.Ipc;
using WinCraft.Infrastructure.Shell;
using System.Runtime.InteropServices;

namespace WinCraft.Infrastructure.Security
{
    /// <summary>
    /// Runs one request through the SYSTEM -> TrustedInstaller hop.
    /// </summary>
    internal static class TrustedInstallerBridge
    {
        public static CommandResult Execute(ElevatedCommandRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var resultPipeName = string.Format(
                "WinCraft.TrustedInstaller.Result.{0}.{1}",
                PInvoke.GetCurrentProcessId(),
                Guid.NewGuid().ToString("N"));
            var requestPipeName = string.Format(
                "WinCraft.TrustedInstaller.Request.{0}.{1}",
                PInvoke.GetCurrentProcessId(),
                Guid.NewGuid().ToString("N"));

            using var resultPipeHandle = ElevatedAgentPipeServer.Create(resultPipeName);
            using var requestPipeHandle = ElevatedAgentPipeServer.Create(requestPipeName);

            var executablePath = ProcessElevation.GetCurrentProcessPath();
            string[] args =
            {
                ElevatedAgentArguments.TrustedInstallerHopMode,
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
                    PrivilegeErrorCodes.TrustedInstallerHopFailed,
                    "The SYSTEM hop process could not be started.",
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
                        "The TrustedInstaller hop closed the pipe unexpectedly.");

                    if (!string.Equals(result?.RequestId, request.RequestId, StringComparison.Ordinal))
                    {
                        return CommandResult.Failure(
                            PrivilegeErrorCodes.UnexpectedRequestId,
                            "The TrustedInstaller hop returned an unexpected request identifier.",
                            request.RequestId);
                    }

                    return result;
                }
                catch (Win32Exception exception)
                {
                    Log.Error(exception, "TrustedInstaller hop pipe error.");
                    return CommandResult.Failure(
                        PrivilegeErrorCodes.TrustedInstallerHopFailed,
                        exception.Message,
                        request.RequestId);
                }
                catch (TimeoutException exception)
                {
                    Log.Error(exception, "TrustedInstaller hop timed out.");
                    return CommandResult.Failure(
                        PrivilegeErrorCodes.TrustedInstallerHopFailed,
                        exception.Message,
                        request.RequestId);
                }
                catch (InvalidOperationException exception)
                {
                    Log.Error(exception, "TrustedInstaller hop protocol error.");
                    return CommandResult.Failure(
                        PrivilegeErrorCodes.TrustedInstallerHopFailed,
                        exception.Message,
                        request.RequestId);
                }
            }
        }

        public static int RunTrustedInstallerHop(string[] args)
        {
            var callerResultPipeName = CommandLineArguments.GetValue(args, ElevatedAgentArguments.PipeName);
            var requestPipeName = CommandLineArguments.GetValue(args, ElevatedAgentArguments.RequestPipeName);
            var requestId = CommandLineArguments.GetValue(args, ElevatedAgentArguments.RequestId);
            var executeResultPipeName = string.Format(
                "WinCraft.TrustedInstaller.Result.{0}.{1}",
                PInvoke.GetCurrentProcessId(),
                Guid.NewGuid().ToString("N"));
            var executeRequestPipeName = string.Format(
                "WinCraft.TrustedInstaller.Request.{0}.{1}",
                PInvoke.GetCurrentProcessId(),
                Guid.NewGuid().ToString("N"));

            try
            {
                var request = ReceiveRequest(requestPipeName);
                if (request == null)
                {
                    SendResult(callerResultPipeName, CommandResult.Failure(
                        PrivilegeErrorCodes.InvalidRequest,
                        "The TrustedInstaller hop received an empty request.",
                        requestId));
                    return 1;
                }

                TrustedInstallerService.EnsureRunning();

                var executablePath = ProcessElevation.GetCurrentProcessPath();
                string[] executeArgs =
                {
                    ElevatedAgentArguments.TrustedInstallerExecuteMode,
                    ElevatedAgentArguments.PipeName,
                    executeResultPipeName,
                    ElevatedAgentArguments.RequestPipeName,
                    executeRequestPipeName,
                    ElevatedAgentArguments.RequestId,
                    request.RequestId ?? string.Empty
                };

                using var executeResultPipeHandle = ElevatedAgentPipeServer.Create(executeResultPipeName);
                using var executeRequestPipeHandle = ElevatedAgentPipeServer.Create(executeRequestPipeName);

                if (!TokenProcessLauncher.TryStartProcessFromTrustedSource(
                    "TrustedInstaller.exe",
                    activeSessionOnly: false,
                    executablePath,
                    executeArgs,
                    useActiveSessionId: true,
                    out Process tiProcess))
                {
                    SendResult(callerResultPipeName, CommandResult.Failure(
                        PrivilegeErrorCodes.TrustedInstallerHopFailed,
                        "The TrustedInstaller execute process could not be started.",
                        request.RequestId));
                    return 1;
                }

                using (tiProcess)
                {
                    try
                    {
                        ElevatedAgentPipeServer.WaitForConnection(executeRequestPipeHandle);
                        ValidateConnectedClientProcess(executeRequestPipeHandle, tiProcess.Id);
                        PipeMessageIO.WriteMessage(executeRequestPipeHandle, request);

                        ElevatedAgentPipeServer.WaitForConnection(executeResultPipeHandle);
                        ValidateConnectedClientProcess(executeResultPipeHandle, tiProcess.Id);
                        var result = PipeMessageIO.ReadMessage<CommandResult>(
                            executeResultPipeHandle,
                            "The TrustedInstaller execute process closed the pipe unexpectedly.");

                        if (!string.Equals(result?.RequestId, request.RequestId, StringComparison.Ordinal))
                        {
                            result = CommandResult.Failure(
                                PrivilegeErrorCodes.UnexpectedRequestId,
                                "The TrustedInstaller execute process returned an unexpected request identifier.",
                                request.RequestId);
                        }

                        SendResult(callerResultPipeName, result);
                        return result.Succeeded ? 0 : 1;
                    }
                    catch (Exception exception) when (
                        exception is Win32Exception
                        || exception is TimeoutException
                        || exception is InvalidOperationException)
                    {
                        SendResult(callerResultPipeName, CommandResult.Failure(
                            PrivilegeErrorCodes.TrustedInstallerHopFailed,
                            exception.Message,
                            request.RequestId));
                        return 1;
                    }
                }
            }
            catch (Exception exception) when (
                exception is Win32Exception
                || exception is TimeoutException
                || exception is InvalidOperationException)
            {
                TrySendFailure(
                    callerResultPipeName,
                    PrivilegeErrorCodes.TrustedInstallerHopFailed,
                    exception.Message,
                    requestId);
                return 1;
            }
            catch (Exception exception)
            {
                TrySendFailure(
                    callerResultPipeName,
                    PrivilegeErrorCodes.TrustedInstallerServiceFailed,
                    exception.Message,
                    requestId);
                return 1;
            }
        }

        public static int RunTrustedInstallerExecute(string[] args)
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
                        "The TrustedInstaller execute process received an empty request.",
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
                    PrivilegeErrorCodes.TrustedInstallerHopFailed,
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
                Log.Error(sendException, "Failed to send privileged bridge failure result.");
            }
        }

        private static ElevatedCommandRequest ReceiveRequest(string pipeName)
        {
            using var pipeHandle = PipeClientConnection.Open(pipeName);
            return PipeMessageIO.ReadMessage<ElevatedCommandRequest>(
                pipeHandle,
                "The request pipe closed before the privileged request arrived.");
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
