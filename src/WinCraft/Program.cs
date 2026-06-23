using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Windows.Win32;
using WinCraft.Infrastructure;
using WinCraft.Infrastructure.Diagnostics;
using WinCraft.Infrastructure.Ipc;
using WinCraft.Infrastructure.RegistryAccess;
using WinCraft.Infrastructure.Security;
using WinCraft.Infrastructure.Shell;

namespace WinCraft
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Log.Initialize(FileLogger.CreateDefault());
            GlobalExceptionHandler.Register();

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
                RunElevatedAgent(args);
                return;
            }

            if (ProcessElevation.IsCurrentProcessElevated())
            {
                RunElevatedBootstrap(args);
                return;
            }

            RunUserInterface(args);
        }

        private static void RunUserInterface(string[] args)
        {
            var privilegeContext = CreatePrivilegeContext(args);
            var app = new App();
            GlobalExceptionHandler.RegisterDispatcher(app.Dispatcher);
            app.InitializeComponent();
            app.MainWindow = new MainWindow();

            InitializeApplicationServices(privilegeContext.Controller);

            app.Exit += (sender, e) =>
            {
                privilegeContext.Controller?.Dispose();
                CleanupApplicationServices();
            };

            var host = new SingleInstanceHost(app);
            host.StartupNextInstance += (sender, e) =>
            {
                HandleStartupNextInstance(e.CommandLine.ToArray(), privilegeContext, app);
            };
            host.Run(args);
        }

        private static void HandleStartupNextInstance(
            string[] commandLine,
            PrivilegeContext privilegeContext,
            App app)
        {
            HandleAttachRequest(commandLine, privilegeContext, app.Dispatcher);
            ActivateMainWindow(app.MainWindow);
        }

        private static void ActivateMainWindow(Window window)
        {
            if (window == null)
                return;

            if (window.WindowState == WindowState.Minimized)
                window.WindowState = WindowState.Normal;
            window.Activate();
        }

        private static PrivilegeContext CreatePrivilegeContext(string[] args)
        {
            if (CommandLineArguments.Contains(args, ElevatedAgentArguments.AttachElevatedAgentMode))
            {
                var pipeName = CommandLineArguments.GetValue(args, ElevatedAgentArguments.PipeName);
                var agentPid = CommandLineArguments.GetInt32Value(args, ElevatedAgentArguments.AgentPid);
                return CreateAttachedPrivilegeContext(agentPid, pipeName);
            }

            if (ProcessElevation.IsCurrentProcessElevated())
                return new PrivilegeContext();

            return new PrivilegeContext
            {
                Controller = new ElevatedAgentController()
            };
        }

        private static PrivilegeContext CreateAttachedPrivilegeContext(int agentPid, string pipeName)
        {
            return new PrivilegeContext
            {
                Controller = new ElevatedAgentController(agentPid, pipeName, attachOnly: true)
            };
        }

        private static void InitializeApplicationServices(ElevatedAgentController elevatedAgent)
        {
            var privilegeBroker = new PrivilegeBroker(elevatedAgent);
            ApplicationServices.PrivilegeBroker = privilegeBroker;
            ApplicationServices.RegistryWriter = new PrivilegedRegistryWriter(privilegeBroker);
        }

        private static void CleanupApplicationServices()
        {
            ApplicationServices.RegistryWriter = null;
            ApplicationServices.PrivilegeBroker = null;
        }

        private static void HandleAttachRequest(
            string[] args,
            PrivilegeContext privilegeContext,
            System.Windows.Threading.Dispatcher dispatcher)
        {
            if (privilegeContext == null
                || dispatcher == null
                || !CommandLineArguments.Contains(args, ElevatedAgentArguments.AttachElevatedAgentMode))
            {
                return;
            }

            var pipeName = CommandLineArguments.GetValue(args, ElevatedAgentArguments.PipeName);
            var agentPid = CommandLineArguments.GetInt32Value(args, ElevatedAgentArguments.AgentPid);
            if (string.IsNullOrEmpty(pipeName) || agentPid <= 0)
                return;

            var replacement = CreateAttachedPrivilegeContext(agentPid, pipeName).Controller;
            var previous = privilegeContext.Controller;
            Task.Run(() => TryAttachToExistingHost(replacement))
                .ContinueWith(task =>
                {
                    var attachException = task.Exception;
                    if (dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
                    {
                        replacement?.Dispose();
                        if (attachException != null)
                            Log.Error(attachException, "Failed to attach the existing UI instance to the elevated host.");
                        return;
                    }

                    dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (attachException != null)
                        {
                            replacement?.Dispose();
                            Log.Error(attachException, "Failed to attach the existing UI instance to the elevated host.");
                            return;
                        }

                        if (task.Status != TaskStatus.RanToCompletion || !task.Result)
                        {
                            replacement?.Dispose();
                            Log.Warn("Failed to attach the existing UI instance to the elevated host; keeping the current privilege controller.");
                            return;
                        }

                        if (!ReferenceEquals(privilegeContext.Controller, previous))
                        {
                            replacement?.Dispose();
                            return;
                        }

                        privilegeContext.Controller = replacement;
                        InitializeApplicationServices(replacement);
                        previous?.Dispose();
                    }));
                }, TaskScheduler.Default);
        }

        private static bool TryAttachToExistingHost(ElevatedAgentController controller)
        {
            if (controller == null)
                return false;

            var result = controller.Execute(new ElevatedCommandRequest
            {
                OperationName = ElevatedOperations.Ping,
                PrivilegeLevel = PrivilegeLevel.Administrator,
                RequestId = Guid.NewGuid().ToString("N")
            });

            return result != null && result.Succeeded;
        }

        private static void RunElevatedBootstrap(string[] args)
        {
            var currentProcessId = PInvoke.GetCurrentProcessId();
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
                RunUserInterface(args);
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

        private static void RunElevatedAgent(string[] args)
        {
            var pipeName = CommandLineArguments.GetValue(args, ElevatedAgentArguments.PipeName);
            var uiPid = GetExpectedUiProcessId(args, pipeName);
            RunElevatedHost(pipeName, uiPid);
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
