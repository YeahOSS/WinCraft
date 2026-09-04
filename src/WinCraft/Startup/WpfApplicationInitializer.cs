using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using WinCraft.Compatibility;
using WinCraft.Infrastructure;
using WinCraft.UI;

namespace WinCraft.Startup
{
    public static class WpfApplicationInitializer
    {
        private static bool _isRuntimeConfigured;

        public static void ConfigureRuntime()
        {
            if (_isRuntimeConfigured)
                return;

            _isRuntimeConfigured = true;
            EnablePerMonitorDpiScaling();
            TextBoxBaseCompat.EnableNonAdornerSelectionRendering();
        }

        public static void Initialize()
        {
            UIHelper.FixMenuShowDelay();
            TextInputClipboard.Register();
            RightClickFocus.Register();
            ThemeService.Instance.Initialize();
            OverrideIconFontsIfExternal();
        }

        /// <summary>
        /// When loose .ttf files exist next to the executable (installer
        /// deployment), replace the embedded pack://application font references
        /// with file:/// URIs that load fonts directly from disk.
        /// </summary>
        private static void OverrideIconFontsIfExternal()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var regularPath = Path.Combine(baseDir, "iconfont.ttf");
            if (!File.Exists(regularPath))
                return;

            var resources = Application.Current.Resources;
            resources["IconFontFamily"] = new FontFamily(
                $"file:///{regularPath}#FluentSystemIcons-Regular");
            resources["IconFontFamilyFilled"] = new FontFamily(
                $"file:///{Path.Combine(baseDir, "iconfont-filled.ttf")}#FluentSystemIcons-Filled");
        }

        private static void EnablePerMonitorDpiScaling()
        {
            typeof(object).Assembly.TryInvokeStaticMethod(
                "System.AppContext", "SetSwitch",
                "Switch.System.Windows.DoNotScaleForDpiChanges", false);
            typeof(object).Assembly.TryInvokeStaticMethod(
                "System.AppContext", "SetSwitch",
                "Switch.System.Windows.DoNotUsePresentationDpiCapabilityTier2OrGreater", false);
        }
    }
}
