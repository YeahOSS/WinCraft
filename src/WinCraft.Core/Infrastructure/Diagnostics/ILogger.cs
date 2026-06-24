namespace WinCraft.Infrastructure.Diagnostics
{
    /// <summary>
    /// Lightweight logging abstraction used across the application.
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Writes a log entry at the specified level.
        /// </summary>
        void Write(LogLevel level, string message);
    }
}
