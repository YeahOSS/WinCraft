# Icon Font

WinCraft embeds a subset of Fluent System Icons as a TrueType font,
exposed through `IconGlyph` enum values and the `IconBlock` control.

## Sources

| File | Role |
|------|------|
| `assets/iconfont/FluentSystemIcons-Regular.json` | Metadata mapping icon names to Unicode code points |
| `assets/iconfont/FluentSystemIcons-Regular.ttf` | Regular weight source font |
| `assets/iconfont/FluentSystemIcons-Filled.ttf` | Filled weight source font |
| `assets/iconfont/manifest.json` | Upstream version, expected metadata counts, and SHA-256 hashes |

Fonts are from [microsoft/fluentui-system-icons](https://github.com/microsoft/fluentui-system-icons),
MIT licensed.  The TTF and JSON metadata are distributed directly in the repo's
[`fonts/`](https://github.com/microsoft/fluentui-system-icons/tree/main/fonts) directory.

**Download links (latest `main`):**

| Variant | TTF | JSON |
|---------|-----|------|
| Regular | [`FluentSystemIcons-Regular.ttf`](https://raw.githubusercontent.com/microsoft/fluentui-system-icons/main/fonts/FluentSystemIcons-Regular.ttf) | [`FluentSystemIcons-Regular.json`](https://raw.githubusercontent.com/microsoft/fluentui-system-icons/main/fonts/FluentSystemIcons-Regular.json) |
| Filled  | [`FluentSystemIcons-Filled.ttf`](https://raw.githubusercontent.com/microsoft/fluentui-system-icons/main/fonts/FluentSystemIcons-Filled.ttf)  | [`FluentSystemIcons-Filled.json`](https://raw.githubusercontent.com/microsoft/fluentui-system-icons/main/fonts/FluentSystemIcons-Filled.json)   |

## Pipeline

```
FluentSystemIcons-Regular.json ────┐
FluentSystemIcons-Filled.json  ─────┤
WinCraft .cs / .xaml sources   ─────┼──> iconfont.exe ──> IconGlyph.g.cs + IconGlyphMap.g.cs
FluentSystemIcons-Regular.ttf ─────┤
FluentSystemIcons-Filled.ttf  ──────┘                     + iconfont.ttf + iconfont-filled.ttf (Release)
```

- **Full-font build**: source fonts + full enum/map; the tool generates code only.
- **Subset build**: a Release build with `IconFontSubset=true` scans `.cs` / `.xaml`,
  then emits a used-only enum/map and subsets Regular and Filled fonts to the
  same icon set.
- The publish workflow passes `IconFontSubset=true` globally while building
  `WinCraft.Portable` and adds the host project root to the scan closure.
  Ordinary solution/library builds use one shared full enum/font `WinCraft.dll`,
  avoiding configuration-dependent public API for consumers.
- `WinCraft.Gallery` rejects subset mode because its icon browser displays the
  entire catalog.
- Debug always uses full fonts, even if `IconFontSubset=true` is supplied.
- `IconFont.targets` builds and invokes `tools/IconFontTool`; no downloaded binary
  or setup step is required.
- Font hashes, metadata counts, and codepoint coverage are verified incrementally
  before generation.

## Subsetter scope

| Supported | Behavior |
|-----------|----------|
| sfnt TrueType with `glyf`/`loca` outlines | Remaps simple and compound glyphs, cmap, hmtx, hhea, maxp, head, and OS/2 character bounds |
| Unicode cmap formats 4 and 12 | Preserves supported Unicode encoding records |
| `name`, `cvt `, `fpgm`, `prep`, `gasp` | Passed through; `post` is normalized to version 3 |
| Layout and unknown tables | Dropped because icon lookup is codepoint-based and does not use shaping |

The subsetter is a focused implementation for the repository's Fluent TrueType
assets. It does not provide text shaping or CFF/variable/color font subsetting.

## Tool commands

| Command | Use |
|---------|-----|
| `generate` | Scan sources, generate enum/map code, and create both subset fonts |
| `subset --input in.ttf --output out.ttf --unicodes F6AA,F86A` | Create a standalone TrueType subset |
| `verify` | Verify manifest hashes, metadata counts, and all metadata-to-font mappings |

## Usage

```xaml
<!-- Inline icon -->
<ui:IconBlock Icon="Settings24" />

<!-- Icon on a button -->
<Button ui:Design.Icon="Settings24" Content="Settings" />

<!-- Filled variant -->
<ui:IconBlock ui:Design.IsIconFilled="True" Icon="Settings24" />

<!-- Filled variant inherited by all children -->
<StackPanel ui:Design.IsIconFilled="True">
    <Button ui:Design.Icon="Home24" Content="Home" />
    <MenuItem ui:Design.Icon="Search24" Header="Search" />
</StackPanel>
```

If Fluent does not provide a matching Filled glyph, `IconBlock` keeps both the
Regular codepoint and Regular font instead of rendering an unrelated Filled glyph
that happens to share the same codepoint.

## Enum

`IconGlyph` is generated at `UI/IconGlyph.g.cs`. Each member's value is its
Unicode code point. Full builds expose the complete catalog; final publish builds
contain only members referenced by the WinCraft and host source closure:

```csharp
public enum IconGlyph
{
    None = 0,
    Settings24 = 61754,
    // ...
}
```

## Adding a new icon

1. Verify the icon exists in `FluentSystemIcons-Regular.json`.
2. Reference it in XAML or C#: `IconGlyph.NewIconName`.
3. On the next Release build the subset font is regenerated — no manual steps.

## Icon Browser

Use `WinCraft.Gallery` project's IconsPage to browse all available icons:
- Enumerates `IconGlyph` enum values via reflection
- Supports search by name or code point
- Toggle between Regular and Filled variants
- Click an icon name to copy to clipboard
- Size filter groups icons by pixel dimension

## Upgrading the icon set

1. Download the latest Regular / Filled TTFs and JSON from the links above.
2. Replace all files in `assets/iconfont/` with the downloaded ones.
3. Update the version, expected entry counts, and SHA-256 hashes in
   `assets/iconfont/manifest.json`, then rebuild. The build automatically verifies
   every metadata codepoint and regenerates the enum, map, and subset fonts.
