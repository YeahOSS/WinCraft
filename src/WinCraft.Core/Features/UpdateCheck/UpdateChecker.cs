using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using WinCraft.Infrastructure.Net;

namespace WinCraft.Features.UpdateCheck
{
    /// <summary>
    /// Checks for available updates by querying GitHub and Gitee release APIs.
    /// GitHub is the primary source; Gitee is the fallback. In Chinese locale
    /// environments the order is reversed for faster response in that region.
    /// </summary>
    public class UpdateChecker
    {
#pragma warning disable S1075 // Public API endpoints, not local paths
        private const string GitHubApiUrl =
            "https://api.github.com/repos/YeahOSS/WinCraft/releases/latest";

        private const string GiteeApiUrl =
            "https://gitee.com/api/v5/repos/YeahOSS/WinCraft/releases/latest";
#pragma warning restore S1075

        /// <summary>
        /// Check both sources for an available update, trying the preferred
        /// source first and falling back on failure.
        /// </summary>
        public async Task<UpdateCheckOutcome> CheckForUpdatesAsync()
        {
            UpdateCheckOutcome lastFailure = null;

            foreach (var (url, name) in GetSourceOrder())
            {
                var result = await CheckFromSourceAsync(url, name);
                if (result.Success)
                    return result;
                lastFailure = result;
            }

            return lastFailure
                ?? UpdateCheckOutcome.Failed("All update sources are unreachable.");
        }

        /// <summary>Check a single API endpoint for updates.</summary>
        public async Task<UpdateCheckOutcome> CheckFromSourceAsync(string apiUrl, string sourceName)
        {
            try
            {
                var exePath = Assembly.GetEntryAssembly().Location;
                var info = FileVersionInfo.GetVersionInfo(exePath);
                var current = new Version(info.ProductVersion);
                using var downloader = new HttpDownloader();
                downloader.UserAgent = "WinCraft/" + current.ToString(3);
                downloader.Timeout = 15_000; // 15 s is enough for a small JSON response

                string json = await downloader.FetchStringAsync(new Uri(apiUrl));

                var release = ReleaseResponseParser.Parse(json);

                if (release.Version > current)
                    return UpdateCheckOutcome.UpdateFound(release, sourceName);

                return UpdateCheckOutcome.UpToDate();
            }
            catch (Exception ex)
            {
                return UpdateCheckOutcome.Failed(
                    string.Format("[{0}] {1}", sourceName, ex.Message));
            }
        }

        /// <summary>
        /// Determine preferred source order. Chinese locale environments
        /// use Gitee first (lower latency in China); everywhere else uses
        /// GitHub first.
        /// </summary>
        private static (string url, string name)[] GetSourceOrder()
        {
            if (RegionInfo.CurrentRegion.Name == "CN")
                return [(GiteeApiUrl, "Gitee"), (GitHubApiUrl, "GitHub")];
            return [(GitHubApiUrl, "GitHub"), (GiteeApiUrl, "Gitee")];
        }
    }
}
