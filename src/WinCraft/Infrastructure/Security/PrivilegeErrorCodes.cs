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
        public const string PrivilegeLevelRequired = "privilege_level_required";
        public const string TrustedInstallerHopFailed = "trusted_installer_hop_failed";
        public const string TrustedInstallerServiceFailed = "trusted_installer_service_failed";
        public const string UnexpectedRequestId = "unexpected_request_id";
        public const string RegistryAccessDenied = "registry_access_denied";
        public const string RegistryWriteFailed = "registry_write_failed";
        public const string RegistryDeleteFailed = "registry_delete_failed";
        public const string UnsupportedOperation = "unsupported_operation";
    }
}
