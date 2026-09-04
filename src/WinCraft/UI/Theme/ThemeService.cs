using System;
using System.Windows;
using Microsoft.Win32;
using Windows.Win32;
using WinCraft.Infrastructure;
using WinCraft.Infrastructure.Diagnostics;

namespace WinCraft.UI
{
    public class ThemeService
    {
        public static ThemeService Instance { get; } = new ThemeService();

        private ResourceDictionary _currentDictionary;
        private ResourceDictionary _lightDictionary;
        private ResourceDictionary _darkDictionary;
        private ThemeMode _mode = ThemeMode.Auto;
        private bool _lastSystemDarkMode;
        private bool _isInitialized;
        public ThemeMode EffectiveTheme { get; private set; } = ThemeMode.Light;

        /// <summary>The configured theme mode (Light, Dark, or Auto).</summary>
        public ThemeMode Mode => _mode;

        public event Action EffectiveThemeChanged;

        private ThemeService() { }

        public void Initialize()
        {
            if (_isInitialized)
                return;

            _isInitialized = true;

            // Reuse the startup dictionary so light mode does not leave a duplicate resource dictionary.
            _lightDictionary = TryFindThemeDictionary(ThemeMode.Light, out var startupDictionary)
                ? startupDictionary
                : LoadThemeDictionary(ThemeMode.Light);
            _currentDictionary = _lightDictionary;

            _lastSystemDarkMode = IsSystemDarkMode();
            ApplyEffectiveTheme();

            SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
            Application.Current.Exit += (s, e) => SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        }

        public void SetMode(ThemeMode mode)
        {
            if (_mode == mode)
                return;

            _mode = mode;
            ApplyEffectiveTheme();
        }

        private void ApplyEffectiveTheme()
        {
            var resolved = ResolveTheme(_mode);
            if (EffectiveTheme == resolved && _currentDictionary != null)
            {
                ApplyNativeDarkMode(resolved);
                return;
            }

            EffectiveTheme = resolved;
            ApplyThemeDictionary(resolved);
            ApplyNativeDarkMode(resolved);
            EffectiveThemeChanged?.Invoke();
        }

        private static ThemeMode ResolveTheme(ThemeMode mode)
        {
            if (mode == ThemeMode.Auto)
                return IsSystemDarkMode() ? ThemeMode.Dark : ThemeMode.Light;

            return mode;
        }

        private void ApplyThemeDictionary(ThemeMode theme)
        {
            var resources = Application.Current.Resources;
            if (resources == null)
                return;

            var newDict = theme == ThemeMode.Dark ? GetDarkDictionary() : _lightDictionary;
            if (newDict == null)
                return;

            // Insert new before removing old — no flicker gap.
            resources.MergedDictionaries.Add(newDict);

            if (_currentDictionary != null)
                resources.MergedDictionaries.Remove(_currentDictionary);

            _currentDictionary = newDict;
        }

        /// <summary>
        /// Returns the dark brushes dictionary, loading it on first access.
        /// Delaying this load avoids parsing the dark BAML at startup when the
        /// system is in light mode.
        /// </summary>
        private ResourceDictionary GetDarkDictionary()
        {
            return _darkDictionary ??= LoadThemeDictionary(ThemeMode.Dark);
        }

        private static ResourceDictionary LoadThemeDictionary(ThemeMode theme)
        {
            var source = GetThemeDictionarySource(theme);
            return new ResourceDictionary
            {
                Source = new Uri(source, UriKind.Relative)
            };
        }

        private static bool TryFindThemeDictionary(ThemeMode theme, out ResourceDictionary result)
        {
            result = null;
            var resources = Application.Current?.Resources;
            if (resources == null)
                return false;

            var source = GetThemeDictionarySource(theme);
            foreach (var dictionary in resources.MergedDictionaries)
            {
                if (dictionary.Source != null
                    && string.Equals(dictionary.Source.OriginalString, source, StringComparison.OrdinalIgnoreCase))
                {
                    result = dictionary;
                    return true;
                }
            }

            return false;
        }

        private static string GetThemeDictionarySource(ThemeMode theme)
        {
            return $"/WinCraft;component/UI/Theme/Brushes.{theme}.xaml";
        }

        private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            // Category is General for theme/color changes (ImmersiveColorSet)
            if (e.Category != UserPreferenceCategory.General || _mode != ThemeMode.Auto)
                return;

            var isDark = IsSystemDarkMode();
            if (isDark == _lastSystemDarkMode)
                return;

            _lastSystemDarkMode = isDark;
            UIHelper.RunOnUI(ApplyEffectiveTheme);
        }

        private static void ApplyNativeDarkMode(ThemeMode theme)
        {
            if (!WindowsVersion.IsAtLeast(WindowsRelease.Win10_1809))
                return;

            try
            {
                PInvoke.AllowDarkModeForApp(theme == ThemeMode.Dark);
                PInvoke.FlushMenuThemes();
            }
            catch (EntryPointNotFoundException)
            {
                Log.Debug("Native dark-mode theme functions are unavailable.");
            }
        }

        private static bool IsSystemDarkMode()
        {
            if (!WindowsVersion.IsAtLeast(WindowsRelease.Win10_1809))
                return false;

            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                var value = key?.GetValue("AppsUseLightTheme");
                return value is 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
