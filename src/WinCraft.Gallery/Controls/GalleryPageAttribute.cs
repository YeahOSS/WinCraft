using System;
using WinCraft.UI;

namespace WinCraft.Gallery.Controls;

[AttributeUsage(AttributeTargets.Class)]
public sealed class GalleryPageAttribute(string name, string description, IconGlyph icon) : Attribute
{
    public string Name { get; } = name;

    public string Description { get; } = description;

    public IconGlyph Icon { get; } = icon;
}
