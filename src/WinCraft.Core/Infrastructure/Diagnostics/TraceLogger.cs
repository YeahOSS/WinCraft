using System.Diagnostics;

namespace WinCraft.Infrastructure.Diagnostics
{
    /// <summary>
    /// Default logger implementation backed by <see cref="Trace"/>.
    /// Outputs to attached trace listeners (Debug view in Visual Studio, or file via app.config).
    /// </summary>
    internal sealed class TraceLogger : ILogger
    {
        public void Write(LogLevel level, string message)
        {
            var formatted = Log.FormatEntry(level, message);

            switch (level)
            {
                case LogLevel.Debug:
                case LogLevel.Info:
                    Trace.TraceInformation(formatted);
                    break;
                case LogLevel.Warn:
                    Trace.TraceWarning(formatted);
                    break;
                case LogLevel.Error:
                case LogLevel.Fatal:
                    Trace.TraceError(formatted);
                    break;
            }
        }
    }
}
