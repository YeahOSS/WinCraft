using System;
using System.IO;

namespace WinCraft.Infrastructure.Diagnostics
{
    /// <summary>
    /// Logs to a text file under the local application data folder.
    /// </summary>
    internal sealed class FileLogger : ILogger, IDisposable
    {
        private readonly object _lock = new();
        private readonly StreamWriter _writer;
        private bool _disposed;

        /// <summary>
        /// Creates a file logger that writes to the specified absolute path.
        /// Intermediate directories are created automatically.
        /// </summary>
        public FileLogger(string logFilePath)
        {
            if (logFilePath == null)
                throw new ArgumentNullException(nameof(logFilePath));

            var directory = Path.GetDirectoryName(logFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            // Allow multiple processes (e.g. the UI and the elevated agent)
            // to write to the same log file concurrently.
            var stream = new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            _writer = new StreamWriter(stream)
            {
                AutoFlush = true
            };
        }

        /// <summary>
        /// Creates a file logger under <c>%LocalAppData%\WinCraft\Logs\</c>
        /// with a timestamped file name.
        /// </summary>
        public static FileLogger CreateDefault()
        {
            var fileName = $"{nameof(WinCraft)}_{DateTime.Now:yyyyMMdd}.log";
            var logPath = Path.Combine(AppDataPaths.Logs, fileName);

            return new FileLogger(logPath);
        }

        public void Write(LogLevel level, string message)
        {
            if (_disposed)
                return;

            var formatted = Log.FormatEntry(level, message);

            lock (_lock)
            {
                if (!_disposed)
                    _writer.WriteLine(formatted);
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposed)
                    return;
                _disposed = true;
                _writer.Dispose();
            }
        }
    }
}
