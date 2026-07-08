using System.Diagnostics;
using System.IO;
using WinCraft.Infrastructure;

namespace WinCraft.Features
{
    public static class LicenseViewer
    {
        private const string LicenseFileName = "LICENSE.rtf";
        private const string OpenSourceLicensesFileName = "OPEN-SOURCE-LICENSES.md";
        private static string LicenseFallbackUrl =>
            BuildRawGitHubUrl("LICENSE");

        private static string OpenSourceLicensesFallbackUrl =>
            BuildRawGitHubUrl("docs/OPEN-SOURCE-LICENSES.md");

        public static void OpenLicense()
        {
            OpenFileOrUrl(LicenseFileName, LicenseFallbackUrl);
        }

        public static void OpenSourceLicenses()
        {
            OpenFileOrUrl(OpenSourceLicensesFileName, OpenSourceLicensesFallbackUrl);
        }

        private static void OpenFileOrUrl(string fileName, string fallbackUrl)
        {
            string localPath = Path.Combine(ProductInfo.StartupPath, fileName);

            Process.Start(new ProcessStartInfo
            {
                FileName = File.Exists(localPath) ? localPath : fallbackUrl,
                UseShellExecute = true
            });
        }

        private static string BuildRawGitHubUrl(string path)
        {
            return $"https://raw.githubusercontent.com/{ProductInfo.Publisher}/{ProductInfo.ProductName}/master/{path}";
        }
    }
}
