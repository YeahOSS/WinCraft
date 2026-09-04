using System;
using WinCraft.UI;

namespace WinCraft.Gallery.Models;

public sealed class NavItem(string name, string description, IconGlyph icon, Type pageType)
{
    public string Name { get; } = name;

    public string Description { get; } = description;

    public IconGlyph Icon { get; } = icon;

    public Type PageType { get; } = pageType;

}
