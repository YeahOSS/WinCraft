using WinCraft.UI;

namespace WinCraft.Gallery.ViewModels.Pages;

public sealed class WindowChromeViewModel : ObservableObject
{
    public bool ShowIntro
    {
        get => GetValue<bool>();
        set => SetValue(value);
    }

    public RelayCommand OpenChromeDemoCommand { get; }

    public RelayCommand OpenSystemDemoCommand { get; }

    public WindowChromeViewModel()
    {
        ShowIntro = true;
        OpenChromeDemoCommand = new RelayCommand(OpenChromeDemo);
        OpenSystemDemoCommand = new RelayCommand(OpenSystemDemo);
    }

    private void OpenChromeDemo() => OpenDemo<ChromeWindow>("ChromeWindow");

    private void OpenSystemDemo() => OpenDemo<SystemWindow>("SystemWindow");

    private void OpenDemo<TWindow>(string title) where TWindow : WindowBase, new()
    {
        var window = new TWindow
        {
            Title = title,
            Width = 500,
            Height = 520,
            Owner = UIHelper.GetBestOwner(),
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner,
            Content = new Views.Pages.WindowChromePage
            {
                DataContext = new WindowChromeViewModel { ShowIntro = false },
            },
        };
        window.Show();
    }
}
