using System;
using System.Diagnostics;
using WinCraft.Infrastructure.Diagnostics;
using WinCraft.Infrastructure.Ipc;
using WinCraft.Infrastructure.Security;
using WinCraft.Infrastructure.Shell;

namespace WinCraft.Startup
{
    internal static class ElevatedHostStartup
    {
        public static void RunElevatedAgent(string[] args)
        {
            var pipeName = CommandLineArguments.GetValue(args, ElevatedAgentArguments.PipeName);
            var uiPid = GetExpectedUiProcessId(args, pipeName);
            RunElevatedHost(pipeName, uiPid);
        }

        public static void RunElevatedBootstrap(string[] args, Action<string[]> runUserInterface)
        {
            var currentProcessId = ProcessElevation.GetCurrentProcessId();
            var pipeName = string.Format(
                "WinCraft.ElevatedAgent.{0}.{1}",
                currentProcessId,
                Guid.NewGuid().ToString("N"));
            var bootstrapArgs = new[]
            {
                ElevatedAgentArguments.AttachElevatedAgentMode,
                ElevatedAgentArguments.PipeName,
                pipeName,
                ElevatedAgentArguments.AgentPid,
                currentProcessId.ToString()
            };
            var launchArgs = AppendArguments(bootstrapArgs, args);

            if (!ProcessElevation.TryLaunchUnelevatedFromShell(launchArgs, out Process uiProcess) || uiProcess == null)
            {
                Log.Warn("Failed to launch an unelevated UI instance from the shell; falling back to the current elevated UI process.");
                runUserInterface(args);
                return;
            }

            using (uiProcess)
            {
                RunElevatedHost(pipeName, uiProcess.Id);
            }
        }

        internal static string[] AppendArguments(string[] leadingArgs, string[] trailingArgs)
        {
            var leadingCount = leadingArgs?.Length ?? 0;
            var trailingCount = trailingArgs?.Length ?? 0;
            var combinedArgs = new string[leadingCount + trailingCount];

            if (leadingCount > 0)
                Array.Copy(leadingArgs, 0, combinedArgs, 0, leadingCount);

            if (trailingCount > 0)
                Array.Copy(trailingArgs, 0, combinedArgs, leadingCount, trailingCount);

            return combinedArgs;
        }

        internal static int? TryParsePipeOwnerProcessId(string pipeName)
        {
            const string prefix = "WinCraft.ElevatedAgent.";
            if (string.IsNullOrEmpty(pipeName)
                || !pipeName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var remainder = pipeName.Substring(prefix.Length);
            var dotIndex = remainder.IndexOf('.');
            var pidPart = dotIndex >= 0
                ? remainder.Substring(0, dotIndex)
                : remainder;

            return int.TryParse(pidPart, out int pid) && pid > 0
                ? pid
                : null;
        }

        private static int? GetExpectedUiProcessId(string[] args, string pipeName)
        {
            if (CommandLineArguments.TryGetInt32Value(args, ElevatedAgentArguments.UiPid, out int uiPid)
                && uiPid > 0)
            {
                return uiPid;
            }

            return TryParsePipeOwnerProcessId(pipeName);
        }

        private static void RunElevatedHost(string pipeName, int? uiPid)
        {
            if (string.IsNullOrEmpty(pipeName))
            {
                Environment.ExitCode = 1;
                return;
            }

            try
            {
                ElevatedAgentPipeClient.Run(pipeName, uiPid, ElevatedOperationExecutor.Execute);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Elevated agent pipe client failed");
                Environment.ExitCode = 1;
            }
        }
    }
}
