namespace WinCraft.Infrastructure.Security
{
    /// <summary>
    /// Describes how the current UI instance reaches its privileged host.
    /// </summary>
    internal sealed class PrivilegeContext
    {
        public ElevatedAgentController Controller { get; set; }
    }
}
