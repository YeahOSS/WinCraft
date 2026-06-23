namespace WinCraft.Infrastructure.Ipc
{
    /// <summary>
    /// Defines command-line arguments used by the elevated agent process.
    /// </summary>
    internal static class ElevatedAgentArguments
    {
        public const string ElevatedAgentMode = "--elevated-agent";
        public const string AttachElevatedAgentMode = "--attach-elevated-agent";
        public const string SystemExecuteMode = "--system-execute";
        public const string TrustedInstallerHopMode = "--trusted-installer-hop";
        public const string TrustedInstallerExecuteMode = "--trusted-installer-execute";
        public const string PipeName = "--pipe-name";
        public const string RequestPipeName = "--request-pipe-name";
        public const string AgentPid = "--agent-pid";
        public const string UiPid = "--ui-pid";
        public const string RequestId = "--request-id";
    }
}
