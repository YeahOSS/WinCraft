using WinCraft.Compatibility;
using WinCraft.Infrastructure;

namespace WinCraft.Features.UpdateCheck
{
    internal static class CurrentReleaseAsset
    {
        public static string GetAssetName()
        {
            if (IsStandaloneExecutable())
                return FrameworkCompat.IsNet30
                    ? ReleaseAssetNames.LegacyPortable
                    : ReleaseAssetNames.StandardPortable;

            if (MsiHelper.IsProductInstalled(ProductInfo.ProductName, ProductInfo.Publisher))
                return ReleaseAssetNames.MsiInstaller;

            return ReleaseAssetNames.NsisInstaller;
        }

        private static bool IsStandaloneExecutable()
        {
            return string.IsNullOrEmpty(typeof(CurrentReleaseAsset).Assembly.Location);
        }
    }
}
