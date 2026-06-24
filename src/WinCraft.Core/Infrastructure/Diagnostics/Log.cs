using System;

namespace WinCraft.Infrastructure.Diagnostics
{
    /// <summary>
    /// Static logging facade for the application.
    /// Call <see cref="Initialize"/> once at startup before any log calls.
    /// </summary>
    public static class Log
    {
        private static ILogger _logger;

        private static readonly Lazy<ILogger> _defaultLogger = new(() => new TraceLogger());

        /// <summary>
        /// Initializes the logging system with the specified logger.
        /// Must be called before any other Log methods.
        /// </summary>
        public static void Initialize(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Returns the current logger, or a lazily-initialized fallback
        /// <see cref="TraceLogger"/> when not yet initialized.
        /// </summary>
        private static ILogger Logger => _logger ?? _defaultLogger.Value;

        public static void Debug(string message)
        {
            Logger.Write(LogLevel.Debug, message);
        }

        public static void Info(string message)
        {
            Logger.Write(LogLevel.Info, message);
        }

        public static void Warn(string message)
        {
            Logger.Write(LogLevel.Warn, message);
        }

        public static void Error(string message)
        {
            Logger.Write(LogLevel.Error, message);
        }

        /// <summary>
        /// Logs an error with an exception and context message.
        /// </summary>
        public static void Error(Exception ex, string context)
        {
            WriteException(LogLevel.Error, ex, context);
        }

        /// <summary>
        /// Logs a fatal-level message.
        /// </summary>
        public static void Fatal(string message)
        {
            Logger.Write(LogLevel.Fatal, message);
        }

        /// <summary>
        /// Logs a fatal error with an exception and context message.
        /// </summary>
        public static void Fatal(Exception ex, string context)
        {
            WriteException(LogLevel.Fatal, ex, context);
        }

        private static void WriteException(LogLevel level, Exception ex, string context)
        {
            if (ex == null)
                return;

            var message = context != null
                ? $"{context}{Environment.NewLine}{ex}"
                : ex.ToString();

            Logger.Write(level, message);
        }

        /// <summary>
        /// Formats a log entry with timestamp, level, and message.
        /// Shared across <see cref="ILogger"/> implementations.
        /// </summary>
        internal static string FormatEntry(LogLevel level, string message)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            return $"[{timestamp}] [{level.ToString().ToUpperInvariant()}] {message}";
        }
    }
}
