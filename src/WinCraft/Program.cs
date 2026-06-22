using System;
using System.Windows;
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
        /// <summary>
        /// Application entry point. Routes to the elevated agent when
        /// started with <c>--elevated-agent</c>, otherwise launches the UI.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Log.Initialize(FileLogger.CreateDefault());

            GlobalExceptionHandler.Register();

            if (CommandLineArguments.Contains(args, ElevatedAgentArguments.ElevatedAgentMode))
            {
                RunElevatedAgent(args);
                return;
            }

            RunUserInterface(args);
        }

        private static void RunUserInterface(string[] args)
        {
            var app = new App();
            GlobalExceptionHandler.RegisterDispatcher(app.Dispatcher);
            app.InitializeComponent();
            app.MainWindow = new MainWindow();

            // Skip the agent when already elevated — privileged operations can execute locally.
            var elevatedAgent = CreateElevatedAgentController();
            InitializeApplicationServices(elevatedAgent);

            app.Exit += (sender, e) =>
            {
                elevatedAgent?.Dispose();
                CleanupApplicationServices();
            };

            var host = new SingleInstanceHost(app);
            host.StartupNextInstance += (sender, e) =>
            {
                var window = app.MainWindow;
                if (window != null)
                {
                    if (window.WindowState == WindowState.Minimized)
                        window.WindowState = WindowState.Normal;
                    window.Activate();
                }
            };
            host.Run(args);
        }

        private static ElevatedAgentController CreateElevatedAgentController()
        {
            if (ProcessElevation.IsCurrentProcessElevated())
                return null;

            return new ElevatedAgentController();
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

        /// <summary>
        /// Elevated agent entry point. Connects to the UI-owned named pipe
        /// and processes privileged requests until shutdown.
        /// </summary>
        private static void RunElevatedAgent(string[] args)
        {
            var pipeName = CommandLineArguments.GetValue(args, ElevatedAgentArguments.PipeName);

            if (string.IsNullOrEmpty(pipeName))
            {
                Environment.ExitCode = 1;
                return;
            }

            try
            {
                ElevatedAgentPipeClient.Run(pipeName, ElevatedOperationExecutor.Execute);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Elevated agent pipe client failed");
                Environment.ExitCode = 1;
            }
        }
    }
}
