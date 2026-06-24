namespace WinCraft.Infrastructure.RegistryAccess
{
    /// <summary>
    /// Identifies the registry hive targeted by a write operation.
    /// </summary>
    internal enum RegistryValueLocation
    {
        CurrentUser,
        LocalMachine,
        ClassesRoot
    }
}
