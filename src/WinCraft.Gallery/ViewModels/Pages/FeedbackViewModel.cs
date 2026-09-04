using System.Windows;
using WinCraft.UI;
using MessageBox = WinCraft.UI.MessageBox;

namespace WinCraft.Gallery.ViewModels.Pages;

public sealed class FeedbackViewModel : ObservableObject
{
    public FeedbackViewModel()
    {
        ShowInfoToastCommand = new RelayCommand(
            () => MessageTip.Show("Task completed successfully."));
        ShowSuccessToastCommand = new RelayCommand(
            () => MessageTip.Show("Your changes have been saved.", VisualRole.Success));
        ShowWarningToastCommand = new RelayCommand(
            () => MessageTip.Show("Disk space is running low.", VisualRole.Warning, autoHideDelay: 5000));
        ShowErrorToastCommand = new RelayCommand(
            () => MessageTip.Show("Failed to connect to the server.", VisualRole.Error, autoHideDelay: 0));

        ShowInfoDialogCommand = new RelayCommand(
            () => MessageBox.Show("This is a themed modal dialog that follows the current light or dark theme.", "Information"));
        ShowConfirmDialogCommand = new RelayCommand(ShowConfirmDialog);
        ShowRememberChoiceDialogCommand = new RelayCommand(ShowRememberChoiceDialog);
        ResetRememberChoiceCommand = new RelayCommand(ResetRememberChoice);
        ShowErrorDialogCommand = new RelayCommand(
            () => MessageBox.Show(
                "An unexpected error occurred while processing your request.",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error));
    }

    public RelayCommand ShowInfoToastCommand { get; }
    public RelayCommand ShowSuccessToastCommand { get; }
    public RelayCommand ShowWarningToastCommand { get; }
    public RelayCommand ShowErrorToastCommand { get; }
    public RelayCommand ShowInfoDialogCommand { get; }
    public RelayCommand ShowConfirmDialogCommand { get; }
    public RelayCommand ShowRememberChoiceDialogCommand { get; }
    public RelayCommand ResetRememberChoiceCommand { get; }
    public RelayCommand ShowErrorDialogCommand { get; }

    public bool IsUpdateReminderSuppressed
    {
        get => GetValue<bool>();
        set => SetValue(value);
    }

    private static void ShowConfirmDialog()
    {
        var result = MessageBox.Show(
            "Are you sure you want to delete this item? This action cannot be undone.",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
            MessageTip.Show("Item deleted.", VisualRole.Success);
    }

    private void ShowRememberChoiceDialog()
    {
        if (IsUpdateReminderSuppressed)
        {
            MessageTip.Show("The update reminder is suppressed. Reset the sample to show it again.");
            return;
        }

        var response = MessageBox.ShowWithResponse(new MessageBoxRequest
        {
            Message = "A new version is available. Would you like to review the release notes?",
            Title = "Update available",
            Button = MessageBoxButton.YesNo,
            Icon = MessageBoxImage.Information,
            AdditionalContent = "This sample retains the choice only for the current session.",
            CheckBoxText = "Don't show this reminder again",
        });

        if (response.Result != MessageBoxResult.Yes)
            return;

        IsUpdateReminderSuppressed = response.IsCheckBoxChecked;
        MessageTip.Show(
            response.IsCheckBoxChecked
                ? "Future reminders are suppressed for this sample."
                : "Release notes opened.",
            VisualRole.Success);
    }

    private void ResetRememberChoice()
    {
        IsUpdateReminderSuppressed = false;
        MessageTip.Show("The update reminder sample was reset.");
    }
}
