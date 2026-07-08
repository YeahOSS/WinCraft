using System;
using WinCraft.Infrastructure.Diagnostics;
using WinCraft.Infrastructure.Ipc;
using WinCraft.Infrastructure.Security;
using WinCraft.Infrastructure.Shell;

namespace WinCraft.Startup
{
    /// <summary>
    /// Routes the process entry point to the correct startup mode based on
    /// command-line arguments and elevation state.
    /// </summary>
    public static class ProgramHost
    {
        /// <summary>
        /// Initializes platform services, selects the startup mode, and
        /// dispatches to the appropriate startup path.
        /// </summary>
        public static void Run(string[] args)
        {
            Log.Initialize(FileLogger.CreateDefault());
            GlobalExceptionHandler.Register();

            if (CommandLineArguments.Contains(args, ElevatedAgentArguments.SystemExecuteMode))
            {
                Environment.ExitCode = SystemPrivilegeBridge.RunSystemExecute(args);
                return;
            }

            if (CommandLineArguments.Contains(args, ElevatedAgentArguments.TrustedInstallerExecuteMode))
            {
                Environment.ExitCode = TrustedInstallerBridge.RunTrustedInstallerExecute(args);
                return;
            }

            if (CommandLineArguments.Contains(args, ElevatedAgentArguments.TrustedInstallerHopMode))
            {
                Environment.ExitCode = TrustedInstallerBridge.RunTrustedInstallerHop(args);
                return;
            }

            if (CommandLineArguments.Contains(args, ElevatedAgentArguments.ElevatedAgentMode))
            {
                ElevatedHostStartup.RunElevatedAgent(args);
                return;
            }

            static void RunUserInterface(string[] uiArgs)
            {
                UserInterfaceStartup.Run(uiArgs);
            }

            if (StartupModeSelector.Select(ProcessElevation.GetCurrentProcessElevationState()) == StartupProcessMode.ElevatedBootstrap)
            {
                ElevatedHostStartup.RunElevatedBootstrap(args, RunUserInterface);
                return;
            }

            RunUserInterface(args);
        }
    }
}
