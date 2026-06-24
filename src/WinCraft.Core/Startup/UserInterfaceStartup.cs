using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WinCraft.Infrastructure;
using WinCraft.Infrastructure.Diagnostics;
using WinCraft.Infrastructure.Ipc;
using WinCraft.Infrastructure.RegistryAccess;
using WinCraft.Infrastructure.Security;
using WinCraft.Infrastructure.Shell;

namespace WinCraft.Startup
{
    internal static class UserInterfaceStartup
    {
        public static void Run(
            string[] args,
            Func<Application> createApplication,
            Action<Application> initializeApplication,
            Func<Window> createMainWindow)
        {
            var privilegeContext = CreatePrivilegeContext(args);
            var app = createApplication();
            GlobalExceptionHandler.RegisterDispatcher(app.Dispatcher);
            initializeApplication(app);
            app.MainWindow = createMainWindow();

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
            Application app)
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
    }
}
