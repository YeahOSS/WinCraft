namespace WinCraft.Infrastructure.Security
{
    /// <summary>
    /// Defines the privilege boundary required for one operation.
    /// </summary>
    public enum PrivilegeLevel
    {
        Standard,
        Administrator,
        TrustedInstaller
    }
}
