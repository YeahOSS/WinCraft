using System.Windows;
using WinCraft.Gallery.ViewModels.Pages;

namespace WinCraft.Gallery.Views.Pages;

public partial class FeaturesPage
{
    public FeaturesPage()
    {
        InitializeComponent();
    }

    private void ImageDropTarget_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = FeaturesViewModel.CanAcceptImage(e.Data)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void ImageDropTarget_Drop(object sender, DragEventArgs e)
    {
        if (DataContext is FeaturesViewModel viewModel)
            viewModel.TrySetDroppedImage(e.Data);

        e.Handled = true;
    }
}
