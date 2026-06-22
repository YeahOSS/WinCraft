namespace WinCraft.Infrastructure.Security
{
    /// <summary>
    /// Lists shared error codes used by privilege and agent workflows.
    /// </summary>
    internal static class PrivilegeErrorCodes
    {
        public const string AgentStartCancelled = "agent_start_cancelled";
        public const string AgentStartFailed = "agent_start_failed";
        public const string AgentUnavailable = "agent_unavailable";
        public const string ElevatedAgentUnavailable = "elevated_agent_unavailable";
        public const string EmptyAgentResponse = "empty_agent_response";
        public const string InvalidRequest = "invalid_request";
        public const string RegistryWriteFailed = "registry_write_failed";
        public const string RegistryDeleteFailed = "registry_delete_failed";
        public const string UnsupportedOperation = "unsupported_operation";
    }
}
