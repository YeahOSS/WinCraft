namespace WinCraft.Infrastructure.Ipc
{
    /// <summary>
    /// Defines command-line arguments used by the elevated agent process.
    /// </summary>
    internal static class ElevatedAgentArguments
    {
        public const string ElevatedAgentMode = "--elevated-agent";
        public const string PipeName = "--pipe-name";
    }
}
