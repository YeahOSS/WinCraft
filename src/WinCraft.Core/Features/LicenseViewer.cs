using System;
using System.Diagnostics;
using System.IO;

namespace WinCraft.Features
{
    public static class LicenseViewer
    {
        private const string LicenseFileName = "LICENSE.txt";
        private const string OpenSourceLicensesFileName = "OPEN-SOURCE-LICENSES.md";
#pragma warning disable S1075
        private const string LicenseFallbackUrl = "https://raw.githubusercontent.com/YeahOSS/WinCraft/master/LICENSE";
        private const string OpenSourceLicensesFallbackUrl = "https://raw.githubusercontent.com/YeahOSS/WinCraft/master/docs/OPEN-SOURCE-LICENSES.md";
#pragma warning restore S1075

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
            string localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);

            Process.Start(new ProcessStartInfo
            {
                FileName = File.Exists(localPath) ? localPath : fallbackUrl,
                UseShellExecute = true
            });
        }
    }
}
