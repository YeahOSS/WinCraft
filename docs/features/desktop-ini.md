# Desktop.ini

## Overview

`desktop.ini` is a hidden system file that customizes how a folder looks and behaves
in Explorer. It uses standard INI syntax but Explorer treats it specially — the folder
itself must be marked as a system folder for `desktop.ini` to take effect.

## Enabling desktop.ini

For Explorer to read `desktop.ini`, two conditions must be met:

1. `desktop.ini` must have `Hidden` + `System` attributes
2. The folder must have the `Read-Only` or `System` flag (via `PathMakeSystemFolder`)

You can trigger these together and notify Explorer with `SHChangeNotify(SHCNE_UPDATEDIR)`.

## Folder Custom Settings

Windows exposes several folder properties through `SHGetSetFolderCustomSettings`,
which reads and writes the corresponding `desktop.ini` entries:

| Mask Flag | desktop.ini Section.Key | Description |
|-----------|------------------------|-------------|
| `FCSM_ICONFILE` | `.ShellClassInfo.IconFile` + `IconIndex` | Custom folder icon |
| `FCSM_INFOTIP` | `.ShellClassInfo.InfoTip` | Hover tooltip text |
| `FCSM_LOGO` | `.ShellClassInfo.Logo` | Folder logo bitmap |
| `FCSM_CLSID` | `.ShellClassInfo.CLSID` | Custom folder GUID |
| `FCSM_VIEWID` | `.ShellClassInfo.UICLSID` | Custom view handler |
| `FCSM_WEBVIEWTEMPLATE` | `.ShellClassInfo.WebViewTemplate` | Web view template |
| `FCSM_FLAGS` | `.ShellClassInfo.Flags` | Shell folder flags |

The API uses a single `SHFOLDERCUSTOMSETTINGS` struct for both read and write.
Read calls use `FCS_READ`; write calls use `FCS_FORCEWRITE`. Each string field
(`pszIconFile`, `pszInfoTip`, `pszLogo`) has a corresponding `cch*` buffer size
field. Unused fields should be left null — the API writes only the fields indicated
by the mask.

## Localized File Names

The `[LocalizedFileNames]` section maps physical file names to localized display names
shown in Explorer:

```ini
[LocalizedFileNames]
MyDocument.txt=My Localized Document Display Name.lnk
```

This is a standard INI section — it uses `GetPrivateProfileString` / `WritePrivateProfileString`
rather than Shell APIs. Key names are the physical file name (not the display name).
Deleting the key removes the localization; the file's real name reappears.

## INI Format Conventions

Explorer's `desktop.ini` follows specific INI parsing rules:

- File is UTF-16 LE (Unicode), optionally with BOM
- Sections are `[SectionName]`; whitespace around names is stripped
- Keys take the form `KeyName=Value`; the value starts after the first `=`
- Comments start with `;` at the beginning of a line
- Indented `;` lines are **not** comments — they continue the previous value
