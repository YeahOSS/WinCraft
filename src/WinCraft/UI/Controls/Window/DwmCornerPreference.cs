namespace WinCraft.UI
{
    /// <summary>
    /// DWM window corner rounding preference (Win11+).
    /// Values map to <c>DWM_WINDOW_CORNER_PREFERENCE</c>.
    /// Below Win11 the frame stays square — no DWM API provides
    /// anti-aliased rounding there without losing native window features.
    /// </summary>
    public enum DwmCornerPreference
    {
        /// <summary>Let the system decide whether to round the corners.</summary>
        Default = 0,
        /// <summary>Never round the corners.</summary>
        DoNotRound = 1,
        /// <summary>Round the corners.</summary>
        Round = 2,
        /// <summary>Round the corners with a small radius.</summary>
        RoundSmall = 3,
    }
}
