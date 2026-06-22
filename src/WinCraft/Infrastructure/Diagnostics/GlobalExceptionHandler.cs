using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace WinCraft.Infrastructure.Diagnostics
{
    /// <summary>
    /// Registers application-wide unhandled-exception handlers so exceptions
    /// on UI threads, background threads, and unobserved tasks are logged
    /// before the process terminates.
    /// </summary>
    internal static class GlobalExceptionHandler
    {
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
        /// Call once after the <see cref="System.Windows.Application"/> instance is created.
        /// </summary>
        public static void RegisterDispatcher(Dispatcher dispatcher)
        {
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
            // Prevent the application from crashing so the user can continue working.
            e.Handled = true;
        }

        private static void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Log.Error(e.Exception, "Unobserved task exception");
            // Do not mark as observed — let the runtime apply its default policy.
        }

        private static void WriteCrashDump(Exception ex)
        {
            var fileName = $"Crash_{DateTime.Now:yyyyMMdd_HHmmss}_{ex.GetType().Name}.dmp";
            var dumpPath = Path.Combine(AppDataPaths.Dumps, fileName);

            if (CrashDump.TryWrite(dumpPath))
                Log.Info($"Crash dump written to {dumpPath}");
            else
                Log.Error($"Failed to write crash dump to {dumpPath}");
        }
    }
}
