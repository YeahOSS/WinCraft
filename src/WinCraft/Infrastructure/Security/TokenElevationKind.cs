namespace WinCraft.Infrastructure.Security
{
    /// <summary>
    /// Stable representation of the Win32 token elevation type.
    /// </summary>
    internal enum TokenElevationKind
    {
        Default,
        Full,
        Limited
    }
}
