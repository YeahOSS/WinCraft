using System;
using System.Windows;
using WinCraft.Infrastructure.Diagnostics;
using WinCraft.Infrastructure.Ipc;
using WinCraft.Infrastructure.Security;
using WinCraft.Infrastructure.Shell;

namespace WinCraft.Startup
{
    internal static class ProgramHost
    {
        public static void Run(
            string[] args,
            Func<Application> createApplication,
            Action<Application> initializeApplication,
            Func<Window> createMainWindow)
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

            void RunUserInterface(string[] uiArgs)
            {
                UserInterfaceStartup.Run(uiArgs, createApplication, initializeApplication, createMainWindow);
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
