namespace WinCraft.Features.UpdateCheck
{
    /// <summary>
    /// Result of an update check. Use static factory methods to create instances.
    /// </summary>
    public class UpdateCheckOutcome
    {
        /// <summary>Whether the check completed without network or parse errors.</summary>
        public bool Success { get; private set; }

        /// <summary>Whether a newer version is available (false when <see cref="Success"/> is false).</summary>
        public bool UpdateAvailable { get; private set; }

        /// <summary>The latest release info; null when <see cref="Success"/> is false.</summary>
        public ReleaseInfo LatestRelease { get; private set; }

        /// <summary>Human-readable error description; null on success.</summary>
        public string ErrorMessage { get; private set; }

        /// <summary>Which source ("GitHub" or "Gitee") serviced the request.</summary>
        public string Source { get; private set; }

        public static UpdateCheckOutcome UpdateFound(ReleaseInfo release, string source)
        {
            return new UpdateCheckOutcome
            {
                Success = true,
                UpdateAvailable = true,
                LatestRelease = release,
                Source = source,
            };
        }

        public static UpdateCheckOutcome UpToDate()
        {
            return new UpdateCheckOutcome
            {
                Success = true,
                UpdateAvailable = false,
            };
        }

        public static UpdateCheckOutcome Failed(string errorMessage)
        {
            return new UpdateCheckOutcome
            {
                Success = false,
                UpdateAvailable = false,
                ErrorMessage = errorMessage,
            };
        }

        public override string ToString()
        {
            if (!Success)
                return string.Format("UpdateCheckOutcome(Failed: {0})", ErrorMessage ?? "unknown");
            if (!UpdateAvailable)
                return "UpdateCheckOutcome(UpToDate)";
            return string.Format("UpdateCheckOutcome(Available: {0})", LatestRelease);
        }
    }
}
