using System;
using System.Windows;
using System.Windows.Controls;

namespace WinCraft.UI
{
    /// <summary>Applies the backdrop palette to its visual subtree while active.</summary>
    public sealed class BackdropThemeScope : Decorator
    {
        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register(nameof(IsActive), typeof(bool), typeof(BackdropThemeScope),
                new PropertyMetadata(false, OnIsActiveChanged));

        private ResourceDictionary _backdropResourceDictionary;
        private bool _isThemeSubscriptionActive;

        public BackdropThemeScope()
        {
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        public bool IsActive
        {
            get => (bool)GetValue(IsActiveProperty);
            set => SetValue(IsActiveProperty, value);
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            UpdateResources();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            AttachThemeSubscription();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            DetachThemeSubscription();
        }

        private static void OnIsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((BackdropThemeScope)d).UpdateResources();
        }

        private void AttachThemeSubscription()
        {
            if (_isThemeSubscriptionActive)
                return;

            ThemeService.Instance.EffectiveThemeChanged += OnEffectiveThemeChanged;
            _isThemeSubscriptionActive = true;
        }

        private void DetachThemeSubscription()
        {
            if (!_isThemeSubscriptionActive)
                return;

            ThemeService.Instance.EffectiveThemeChanged -= OnEffectiveThemeChanged;
            _isThemeSubscriptionActive = false;
        }

        private void OnEffectiveThemeChanged()
        {
            if (!IsActive)
                return;

            RemoveResources();
            AddResources();
        }

        private void UpdateResources()
        {
            if (IsActive)
            {
                if (_backdropResourceDictionary == null)
                    AddResources();
            }
            else
            {
                RemoveResources();
            }
        }

        private void AddResources()
        {
            _backdropResourceDictionary = new ResourceDictionary
            {
                Source = new Uri(
                    $"/WinCraft;component/UI/Theme/Brushes.{ThemeService.Instance.EffectiveTheme}.Backdrop.xaml",
                    UriKind.Relative),
            };
            Resources.MergedDictionaries.Add(_backdropResourceDictionary);
        }

        private void RemoveResources()
        {
            if (_backdropResourceDictionary == null)
                return;

            Resources.MergedDictionaries.Remove(_backdropResourceDictionary);
            _backdropResourceDictionary = null;
        }
    }
}
