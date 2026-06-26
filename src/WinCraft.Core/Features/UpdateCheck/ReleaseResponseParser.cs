using System;
using System.Collections.Generic;
using System.Globalization;
using WinCraft.Infrastructure.Net;

namespace WinCraft.Features.UpdateCheck
{
    /// <summary>
    /// Parses GitHub / Gitee release API JSON responses via <see cref="JsonMiniParser"/>.
    /// </summary>
    public static class ReleaseResponseParser
    {
        public static ReleaseInfo Parse(string json)
        {
            object root = JsonMiniParser.Parse(json);
            var dict = root as Dictionary<string, object>;
            if (dict == null)
                throw new FormatException("Expected a JSON object at the root of the release response.");

            var info = new ReleaseInfo();

            info.TagName = GetString(dict, "tag_name");
            info.Version = ParseVersion(info.TagName);
            info.ReleaseUrl = GetString(dict, "html_url");
            info.Changelog = GetString(dict, "body");

            // GitHub uses "published_at", Gitee uses "created_at".
            string dateStr = GetString(dict, "published_at")
                          ?? GetString(dict, "created_at");
            info.PublishedAt = ParseDateTime(dateStr);

            if (GetValue(dict, "assets") is List<object> assetsList)
            {
                foreach (var assetObj in assetsList)
                {
                    if (assetObj is not Dictionary<string, object> asset)
                        continue;

                    string name = GetString(asset, "name") ?? string.Empty;
                    if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        info.AssetName = name;
                        info.DownloadUrl = GetString(asset, "browser_download_url");
                        break;
                    }
                }
            }

            return info;
        }

        private static string GetString(Dictionary<string, object> dict, string key)
        {
            if (!dict.TryGetValue(key, out object value) || value == null)
                return null;
            return value as string;
        }

        private static object GetValue(Dictionary<string, object> dict, string key)
        {
            dict.TryGetValue(key, out object value);
            return value;
        }

        private static Version ParseVersion(string tagName)
        {
            if (string.IsNullOrEmpty(tagName))
                return new Version();

            string versionStr = tagName;
            if ((versionStr[0] == 'v' || versionStr[0] == 'V') && versionStr.Length > 1)
                versionStr = versionStr.Substring(1);

            try
            {
                return new Version(versionStr);
            }
            catch (Exception)
            {
                return new Version();
            }
        }

        private static DateTime ParseDateTime(string dateStr)
        {
            if (string.IsNullOrEmpty(dateStr))
                return DateTime.MinValue;

            // GitHub format: "2026-01-01T00:00:00Z", Gitee: "2026-01-01T00:00:00+08:00"
            if (DateTime.TryParse(dateStr, CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                    out DateTime parsed))
                return parsed;

            return DateTime.MinValue;
        }
    }
}
