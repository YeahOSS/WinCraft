namespace WinCraft.Infrastructure.Security
{
    /// <summary>
    /// Distinguishes successful, cancelled, and unavailable privileged operations.
    /// </summary>
    internal enum PrivilegeExecutionStatus
    {
        Succeeded,
        Cancelled,
        Unavailable,
        Failed
    }
}
