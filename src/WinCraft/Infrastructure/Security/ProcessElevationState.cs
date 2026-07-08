namespace WinCraft.Infrastructure.Security
{
    /// <summary>
    /// Describes how the current process obtained its administrator capability.
    /// </summary>
    internal enum ProcessElevationState
    {
        Standard,
        SplitTokenElevated,
        FullAdministrator
    }
}
