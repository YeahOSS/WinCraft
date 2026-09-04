using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WinCraft.Infrastructure.Security;
using WinCraft.UI;

namespace WinCraft.Gallery.ViewModels.Pages;

public sealed class FeaturesViewModel : ObservableObject
{
    public FeaturesViewModel()
    {
        ImageStatus = "No image has been dropped yet.";
        ElevationStatus = "No administrator agent has been requested.";
        RequestElevationCommand = new AsyncRelayCommand(RequestElevationAsync);
    }

    public ImageSource DroppedImage
    {
        get => GetValue<ImageSource>();
        private set => SetValue(value);
    }

    public string ImageStatus
    {
        get => GetValue<string>();
        private set => SetValue(value);
    }

    public string ElevationStatus
    {
        get => GetValue<string>();
        private set => SetValue(value);
    }

    public AsyncRelayCommand RequestElevationCommand { get; }

    public static bool CanAcceptImage(IDataObject data)
    {
        return data != null
            && (data.GetDataPresent(DataFormats.Bitmap)
                || data.GetDataPresent(DataFormats.FileDrop));
    }

    public bool TrySetDroppedImage(IDataObject data)
    {
        if (data != null
            && data.GetDataPresent(DataFormats.Bitmap)
            && data.GetData(DataFormats.Bitmap) is ImageSource image)
        {
            DroppedImage = image;
            ImageStatus = "Bitmap received.";
            return true;
        }

        if (data != null
            && data.GetDataPresent(DataFormats.FileDrop)
            && data.GetData(DataFormats.FileDrop) is string[] filePaths)
        {
            foreach (var filePath in filePaths)
            {
                if (!File.Exists(filePath))
                    continue;

                try
                {
                    DroppedImage = LoadImage(filePath);
                    ImageStatus = Path.GetFileName(filePath);
                    return true;
                }
                catch (Exception exception) when (exception is NotSupportedException || exception is IOException || exception is FormatException)
                {
                    ImageStatus = $"Could not load {Path.GetFileName(filePath)}; trying another file.";
                }
            }
        }

        ImageStatus = "The dropped data does not contain a supported image.";
        return false;
    }

    private async Task RequestElevationAsync()
    {
        ElevationStatus = "Waiting for administrator approval...";
        var result = await PrivilegeService.CheckAdministratorAsync();
        if (result.Succeeded)
        {
            ElevationStatus = "Administrator agent connected.";
            return;
        }

        if (result.IsCancelled)
        {
            ElevationStatus = "Administrator request was cancelled.";
            return;
        }

        ElevationStatus = string.IsNullOrEmpty(result.ErrorMessage)
            ? "Administrator agent could not be started."
            : result.ErrorMessage;
    }

    private static ImageSource LoadImage(string filePath)
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.UriSource = new Uri(filePath, UriKind.Absolute);
        image.EndInit();
        if (image.CanFreeze)
            image.Freeze();

        return image;
    }
}
