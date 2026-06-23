namespace WinCraft.Infrastructure.RegistryAccess
{
    /// <summary>
    /// Controls the highest registry privilege level that automatic escalation may use.
    /// </summary>
    internal enum RegistryPrivilegePolicy
    {
        Auto,
        AutoWithoutTI,
        CurrentUserOnly
    }
}
