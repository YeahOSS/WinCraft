using System;

namespace WinCraft.Features.UpdateCheck
{
    /// <summary>Parsed release information from a hosting platform API.</summary>
    public class ReleaseInfo
    {
        public Version Version { get; set; }

        public string TagName { get; set; }

        public string ReleaseUrl { get; set; }

        public string DownloadUrl { get; set; }

        public string AssetName { get; set; }

        public string Changelog { get; set; }

        public DateTime PublishedAt { get; set; }

        public override string ToString()
        {
            return string.Format("{0} ({1})", TagName ?? Version?.ToString(), PublishedAt.ToString("s"));
        }
    }
}
