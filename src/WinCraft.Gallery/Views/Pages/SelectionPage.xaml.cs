using System.Windows.Media;
using WinCraft.Gallery.Controls;
using WinCraft.UI;

namespace WinCraft.Gallery.Views.Pages;

[GalleryPage("Selection", "CheckBox, RadioButton, Switch, and ListBox", IconGlyph.CheckboxChecked20)]
public partial class SelectionPage
{
    public SelectionPage()
    {
        InitializeComponent();
        SampleListView.ItemsSource = new[]
        {
            new SampleFileItem(IconGlyph.Document16, "Design spec v2", "Updated by team lead", "Synced", "#087F43", "Jan 15"),
            new SampleFileItem(IconGlyph.Folder16, "Budget forecast", "Pending review", "Pending", "#B34E12", "Feb 3"),
            new SampleFileItem(IconGlyph.Cloud16, "Build log", "Compilation failed", "Error", "#CC243F", "Mar 22"),
            new SampleFileItem(IconGlyph.Image16, "Release notes", "Draft copy", "New", "#0D6BDD", "Apr 8"),
        };
        SampleListView.SelectedIndex = 0;
    }
}

public sealed class SampleFileItem(IconGlyph icon, string name, string description, string status, string statusColorHex, string modified)
{
    public IconGlyph Icon { get; } = icon;
    public string Name { get; } = name;
    public string Description { get; } = description;
    public string Status { get; } = status;
    public Brush StatusBrush { get; } = new SolidColorBrush((Color)ColorConverter.ConvertFromString(statusColorHex));
    public string Modified { get; } = modified;
}
