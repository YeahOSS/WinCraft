using System;
using System.Globalization;
using System.Threading.Tasks;
using WinCraft.Infrastructure;
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
        private static string GitHubApiUrl => BuildApiUrl("https://api.github.com/repos/", "/releases/latest");

        private static string GiteeApiUrl => BuildApiUrl("https://gitee.com/api/v5/repos/", "/releases/latest");

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
                Version current = ProductInfo.ProductVersion;
                string targetAssetName = CurrentReleaseAsset.GetAssetName();
                using var downloader = new HttpDownloader();
                downloader.UserAgent = ProductInfo.ProductName + "/" + current.ToString(3);
                downloader.Timeout = 15_000; // 15 s is enough for a small JSON response

                string json = await downloader.FetchStringAsync(new Uri(apiUrl));

                var release = ReleaseResponseParser.Parse(json, targetAssetName);

                if (release.Version > current)
                {
                    if (string.IsNullOrEmpty(release.DownloadUrl))
                    {
                        return UpdateCheckOutcome.Failed(
                            string.Format("[{0}] The latest release does not contain {1}.", sourceName, targetAssetName));
                    }

                    return UpdateCheckOutcome.UpdateFound(release, sourceName);
                }

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

        private static string BuildApiUrl(string prefix, string suffix)
        {
            return $"{prefix}{ProductInfo.Publisher}/{ProductInfo.ProductName}{suffix}";
        }
    }
}
