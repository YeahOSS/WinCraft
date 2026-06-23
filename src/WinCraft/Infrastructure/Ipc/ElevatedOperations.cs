namespace WinCraft.Infrastructure.Ipc
{
    /// <summary>
    /// Lists the supported elevated operation names.
    /// </summary>
    internal static class ElevatedOperations
    {
        public const string Ping = "ping";
        public const string Shutdown = "shutdown";
        public const string RegistryWrite = "registry.write";
        public const string RegistryDelete = "registry.delete";
    }
}
