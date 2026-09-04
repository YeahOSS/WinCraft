using System;
using WinCraft.UI;

namespace WinCraft.Gallery.Models;

/// <summary>
/// A tab in the gallery window.  <see cref="Content"/> is the page
/// <see cref="Type"/>; the view is resolved at render time by
/// <see cref="Controls.TypeToViewConverter"/>.
/// </summary>
public sealed class GalleryTab(Type pageType, string title, IconGlyph icon)
{
    public Type PageType { get; } = pageType;

    public string Title { get; } = title;

    public IconGlyph Icon { get; } = icon;

    /// <summary>The page <see cref="Type"/>, resolved to a view by the tab content template.</summary>
    public object Content => PageType;
}
