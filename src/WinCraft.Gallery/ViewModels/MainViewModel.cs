using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using WinCraft.Gallery.Controls;
using WinCraft.Gallery.Models;
using WinCraft.Gallery.Views.Pages;
using WinCraft.UI;

namespace WinCraft.Gallery.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    public MainViewModel()
    {
        Tabs = [];
        HomeItems =
        [
            new GalleryHomeItem(
                "UI",
                "Browse controls, layouts, navigation, windows, and icons.",
                IconGlyph.DesignIdeas24,
                GalleryHomeDestination.Ui),
            new GalleryHomeItem(
                "Features",
                "Try image drag-and-drop and the on-demand administrator agent.",
                IconGlyph.Toolbox24,
                GalleryHomeDestination.Features),
            new GalleryHomeItem(
                "About",
                "Learn about WinCraft, its framework targets, and the gallery.",
                IconGlyph.Info24,
                GalleryHomeDestination.About)
        ];
        UiPages = [];
        DiscoverUiPagesAsync(UiPages);
        OpenHomeItemCommand = new RelayCommand<GalleryHomeItem>(OpenHomeItem);
        OpenPageCommand = new RelayCommand<NavItem>(OpenPage);
        SetThemeModeCommand = new RelayCommand<ThemeMode>(SetThemeMode);
        ToggleUiPageEnabledCommand = new RelayCommand(ToggleUiPageEnabled);
        CurrentThemeMode = ThemeService.Instance.Mode;
        IsUiPageEnabled = true;

        HomeTab = CreateTab(typeof(HomePage), "Home", IconGlyph.Home24);
        Tabs.Add(HomeTab);
        SelectedTab = HomeTab;
    }

    public ObservableCollection<GalleryTab> Tabs { get; }

    public ObservableCollection<GalleryHomeItem> HomeItems { get; }

    public ObservableCollection<NavItem> UiPages { get; }

    public GalleryTab HomeTab { get; }

    public GalleryTab SelectedTab
    {
        get => GetValue<GalleryTab>();
        set => SetValue(value);
    }

    public ThemeMode CurrentThemeMode
    {
        get => GetValue<ThemeMode>();
        private set => SetValue(value);
    }

    public bool IsFollowingSystemTheme => CurrentThemeMode == ThemeMode.Auto;

    public bool IsLightTheme => CurrentThemeMode == ThemeMode.Light;

    public bool IsDarkTheme => CurrentThemeMode == ThemeMode.Dark;

    public bool IsUiPageEnabled
    {
        get => GetValue<bool>();
        private set => SetValue(value);
    }

    public RelayCommand<GalleryHomeItem> OpenHomeItemCommand { get; }

    public RelayCommand<NavItem> OpenPageCommand { get; }

    public RelayCommand<ThemeMode> SetThemeModeCommand { get; }

    public RelayCommand ToggleUiPageEnabledCommand { get; }

    private void OpenHomeItem(GalleryHomeItem item)
    {
        if (item == null)
            return;

        switch (item.Destination)
        {
            case GalleryHomeDestination.Ui:
                OpenUiCatalog();
                break;
            case GalleryHomeDestination.Features:
                OpenTab(typeof(FeaturesPage),
                    () => CreateTab(typeof(FeaturesPage), "Features", IconGlyph.Toolbox24));
                break;
            case GalleryHomeDestination.About:
                OpenTab(typeof(AboutPage),
                    () => CreateTab(typeof(AboutPage), "About", IconGlyph.Info24));
                break;
        }
    }

    private void OpenUiCatalog()
    {
        OpenTab(typeof(UiCatalogPage),
            () => CreateTab(typeof(UiCatalogPage), "UI", IconGlyph.DesignIdeas24));
    }

    private void OpenPage(NavItem page)
    {
        if (page?.PageType == null)
            return;

        OpenTab(page.PageType, () =>
        {
            if (typeof(UserControl).IsAssignableFrom(page.PageType))
                return CreateTab(page.PageType, page.Name, page.Icon);

            return null;
        });
    }

    private void OpenTab(Type pageType, Func<GalleryTab> createTab)
    {
        var existing = Tabs.FirstOrDefault(tab => tab.PageType == pageType);
        if (existing != null)
        {
            SelectedTab = existing;
            return;
        }

        var tab = createTab();
        if (tab == null)
            return;

        Tabs.Add(tab);
        SelectedTab = tab;
    }

    private static GalleryTab CreateTab(Type pageType, string title, IconGlyph icon) =>
        new(pageType, title, icon);

    private void SetThemeMode(ThemeMode mode)
    {
        if (CurrentThemeMode == mode)
            return;

        ThemeService.Instance.SetMode(mode);
        SetValue(mode, nameof(CurrentThemeMode));
        RaisePropertyChanged(nameof(IsFollowingSystemTheme));
        RaisePropertyChanged(nameof(IsLightTheme));
        RaisePropertyChanged(nameof(IsDarkTheme));
    }

    private void ToggleUiPageEnabled()
    {
        IsUiPageEnabled = !IsUiPageEnabled;
    }

    /// <summary>
    /// Scans the Gallery assembly for UI pages in the background, then
    /// populates <paramref name="target"/> on the UI thread.  Moving assembly
    /// reflection off the UI thread avoids a synchronous JIT spike during
    /// <see cref="MainViewModel"/> construction.
    /// </summary>
    private static void DiscoverUiPagesAsync(ObservableCollection<NavItem> target)
    {
        var scheduler = TaskScheduler.FromCurrentSynchronizationContext();
        Task.Run(() => DiscoverUiPages().ToList())
            .ContinueWith(t =>
            {
                foreach (var item in t.Result)
                    target.Add(item);
            }, CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion, scheduler);
    }

    private static IEnumerable<NavItem> DiscoverUiPages()
    {
        var pages = new List<NavItem>();
        var assembly = typeof(App).Assembly;

        foreach (var type in assembly.GetTypes())
        {
            if (type.IsAbstract || !typeof(UserControl).IsAssignableFrom(type))
                continue;

            var attribute = type.GetCustomAttributes(typeof(GalleryPageAttribute), false)
                .OfType<GalleryPageAttribute>()
                .FirstOrDefault();
            if (attribute == null)
                continue;

            pages.Add(new NavItem(
                attribute.Name,
                attribute.Description,
                attribute.Icon,
                type));
        }

        return pages.OrderBy(page => page.Name);
    }
}
