using WinCraft.Infrastructure.Security;

namespace WinCraft.Startup
{
    internal static class StartupModeSelector
    {
        public static StartupProcessMode Select(ProcessElevationState elevationState)
        {
            return elevationState == ProcessElevationState.SplitTokenElevated
                ? StartupProcessMode.ElevatedBootstrap
                : StartupProcessMode.UserInterface;
        }
    }
}
