using System;
using System.Threading.Tasks;
using System.Windows;
using WinCraft.Infrastructure;
using WinCraft.Infrastructure.Diagnostics;
using WinCraft.Infrastructure.Ipc;
using WinCraft.Infrastructure.RegistryAccess;
using WinCraft.Infrastructure.Security;
using WinCraft.Infrastructure.Shell;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace WinCraft.Startup
{
    /// <summary>
    /// Hosts a WPF application with WinCraft's single-instance and privileged-agent lifecycle.
    /// </summary>
    public static class WpfApplicationHost
    {
        /// <param name="onReady">Optional callback invoked after the main window is created and
        /// services are initialized, but before the single-instance host runs.
        /// Receives the <see cref="Application"/> and main <see cref="Window"/>.</param>
        public static void Run(
            string[] args,
            Func<Application> createApplication,
            Func<Window> createMainWindow,
            Action<Application, Window> onReady = null)
        {
            if (createApplication == null)
                throw new ArgumentNullException(nameof(createApplication));
            if (createMainWindow == null)
                throw new ArgumentNullException(nameof(createMainWindow));

            try
            {
                RunInternal(args, createApplication, createMainWindow, onReady);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "WinCraft failed to start");

                var dumpName = $"{DateTime.Now:yyyyMMdd_HHmmss}_Startup_{ex.GetType().Name}.dmp";
                var dumpPath = System.IO.Path.Combine(ProductInfo.DumpsDir, dumpName);
                CrashDump.TryWrite(dumpPath);

                // Show a native error dialog via MessageBoxW so the user sees
                // the failure even if WPF never loaded.
                ShowFatalErrorDialog(ex);

                try
                {
                    Application.Current?.Shutdown(1);
                    return;
                }
                catch
                {
                    // Shutdown itself may fail in a corrupted state.
                }

                Environment.Exit(1);
            }
        }

        private static void RunInternal(
            string[] args,
            Func<Application> createApplication,
            Func<Window> createMainWindow,
            Action<Application, Window> onReady)
        {
            var privilegeContext = CreatePrivilegeContext(args);
            var app = createApplication();
            GlobalExceptionHandler.RegisterDispatcher();
            WpfApplicationInitializer.Initialize();

            Window mainWindow = createMainWindow();
            app.MainWindow = mainWindow;
            mainWindow.Loaded += (_, _) => GlobalExceptionHandler.IsStartupComplete = true;

            InitializeApplicationServices(privilegeContext.Controller);
            app.Exit += (_, _) =>
            {
                privilegeContext.Controller?.Dispose();
                CleanupApplicationServices();
            };

            onReady?.Invoke(app, mainWindow);

            var host = new SingleInstanceHost(app);
            host.StartupNextInstance += (_, e) =>
            {
                HandleStartupNextInstance(e.CommandLine, privilegeContext, app);
            };
            host.Run(args ?? []);
        }

        private static void HandleStartupNextInstance(
            string[] commandLine,
            PrivilegeContext privilegeContext,
            Application app)
        {
            HandleAttachRequest(commandLine, privilegeContext, app.Dispatcher);
            if (!CommandLineArguments.Contains(commandLine, ElevatedAgentArguments.AttachElevatedAgentMode))
                ActivateMainWindow(app.MainWindow);
        }

        private static void ActivateMainWindow(Window window)
        {
            if (window == null)
                return;

            if (!window.IsVisible)
                window.Show();

            if (window.WindowState == WindowState.Minimized)
                window.WindowState = WindowState.Normal;
            window.Activate();
        }

        private static PrivilegeContext CreatePrivilegeContext(string[] args)
        {
            if (CommandLineArguments.Contains(args, ElevatedAgentArguments.AttachElevatedAgentMode))
            {
                string pipeName = CommandLineArguments.GetFlagValue(args, ElevatedAgentArguments.PipeName);
                int agentPid = CommandLineArguments.GetFlagInt32Value(args, ElevatedAgentArguments.AgentPid);
                if (!string.IsNullOrEmpty(pipeName) && agentPid > 0)
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

            string pipeName = CommandLineArguments.GetFlagValue(args, ElevatedAgentArguments.PipeName);
            int agentPid = CommandLineArguments.GetFlagInt32Value(args, ElevatedAgentArguments.AgentPid);
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
                        if (attachException != null)
                            Log.Error(attachException, "Failed to attach the existing UI instance to the elevated host.");
                        replacement?.Dispose();
                        return;
                    }

                    try
                    {
                        dispatcher.BeginInvoke(new Action(() =>
                        {
                            if (dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
                            {
                                replacement?.Dispose();
                                return;
                            }

                            var consumed = false;
                            try
                            {
                                if (attachException != null)
                                {
                                    Log.Error(attachException, "Failed to attach the existing UI instance to the elevated host.");
                                    return;
                                }

                                if (task.Status != TaskStatus.RanToCompletion || !task.Result)
                                {
                                    Log.Warn("Failed to attach the existing UI instance to the elevated host; keeping the current privilege controller.");
                                    return;
                                }

                                if (!ReferenceEquals(privilegeContext.Controller, previous))
                                    return;

                                InitializeApplicationServices(replacement);
                                privilegeContext.Controller = replacement;
                                previous?.Dispose();
                                consumed = true;
                            }
                            finally
                            {
                                if (!consumed)
                                    replacement?.Dispose();
                            }
                        }));
                    }
                    catch (InvalidOperationException)
                    {
                        replacement?.Dispose();
                    }
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

        private static void ShowFatalErrorDialog(Exception ex)
        {
            var message = $"WinCraft encountered a critical error during startup and must close.\n\n"
                        + $"Error: {ex.GetType().Name}\n"
                        + $"Message: {ex.Message}\n\n"
                        + $"Details have been saved to:\n{ProductInfo.LogsDir}";

            PInvoke.MessageBox(
                default(HWND),
                message,
                "WinCraft — Fatal Error",
                MESSAGEBOX_STYLE.MB_OK | MESSAGEBOX_STYLE.MB_ICONERROR);
        }
    }
}
