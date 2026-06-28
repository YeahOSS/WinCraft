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
        public const string RegistryDeleteKey = "registry.deleteKey";
        public const string RegistryMoveKey = "registry.moveKey";
        public const string FileWrite = "file.write";
        public const string FileDelete = "file.delete";
        public const string FileRename = "file.rename";
        public const string FileSetAttributes = "file.setAttributes";
    }
}
