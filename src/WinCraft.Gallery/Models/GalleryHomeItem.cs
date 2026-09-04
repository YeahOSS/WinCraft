using WinCraft.UI;

namespace WinCraft.Gallery.Models;

public sealed class GalleryHomeItem(
    string title,
    string description,
    IconGlyph icon,
    GalleryHomeDestination destination)
{
    public string Title { get; } = title;

    public string Description { get; } = description;

    public IconGlyph Icon { get; } = icon;

    public GalleryHomeDestination Destination { get; } = destination;
}

public enum GalleryHomeDestination
{
    Ui,
    Features,
    About
}
