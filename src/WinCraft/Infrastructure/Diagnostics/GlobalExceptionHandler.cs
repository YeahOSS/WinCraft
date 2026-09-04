using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace WinCraft.Infrastructure.Diagnostics
{
    /// <summary>
    /// Registers application-wide unhandled-exception handlers so exceptions
    /// on UI threads, background threads, and unobserved tasks are logged
    /// before the process terminates or is recovered on the UI thread.
    /// </summary>
    internal static class GlobalExceptionHandler
    {
        /// <summary>
        /// Becomes <c>true</c> after the main window is loaded and the
        /// application is processing user input.  Dispatcher exceptions
        /// before this point are treated as fatal startup errors and
        /// allowed to propagate rather than being swallowed.
        /// </summary>
        public static bool IsStartupComplete { get; set; }

        /// <summary>
        /// Registers process-wide exception handlers (app domain and task scheduler).
        /// Call once in <c>Main</c> before any work starts.
        /// </summary>
        public static void Register()
        {
            AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }

        /// <summary>
        /// Registers the UI-thread exception handler on the application dispatcher.
        /// Call once after the <see cref="Application"/> instance is created.
        /// </summary>
        public static void RegisterDispatcher()
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher != null)
                dispatcher.UnhandledException += OnDispatcherUnhandledException;
        }

        private static void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                var context = e.IsTerminating
                    ? "Unhandled exception (app domain, terminating)"
                    : "Unhandled exception (app domain)";
                Log.Fatal(ex, context);

                if (e.IsTerminating)
                    WriteCrashDump(ex);
            }
            else
            {
                var label = e.IsTerminating ? " (terminating)" : string.Empty;
                Log.Fatal($"Unhandled non-exception object (app domain{label}): {e.ExceptionObject}");
            }
        }

        private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Log.Fatal(e.Exception, "Unhandled exception (dispatcher)");

            if (!IsStartupComplete)
            {
                // Startup-phase exception — do not swallow it.
                // Let it propagate to the startup guard in UserInterfaceStartup,
                // which will show an error dialog and exit the process.
                return;
            }

            // Runtime exception — log, write a crash dump, then keep running.
            // Debug.Fail fires a user-visible assertion only in DEBUG builds;
            // the Error-level log ensures the exception is always recorded.
            Log.Error(e.Exception, "Unhandled runtime exception (dispatcher) — recovered");
            Debug.Fail(e.Exception.ToString());
            WriteCrashDump(e.Exception);
            e.Handled = true;
        }

        private static void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Log.Error(e.Exception, "Unobserved task exception");
            // Do not mark as observed — let the runtime apply its default policy.
        }

        private static void WriteCrashDump(Exception ex)
        {
            var fileName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{ex.GetType().Name}.dmp";
            var dumpPath = Path.Combine(ProductInfo.DumpsDir, fileName);

            if (CrashDump.TryWrite(dumpPath))
                Log.Info($"Crash dump written to {dumpPath}");
            else
                Log.Error($"Failed to write crash dump to {dumpPath}");
        }
    }
}
