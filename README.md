# WinCraft

English | [简体中文](docs/i18n/README.zh-CN.md)

WinCraft is a Windows tuning toolbox focused on system configuration and everyday usability improvements.

The project is still in an early scaffold stage. The repository already contains the multi-target build pipeline, release workflow, and compatibility layer, while the end-user feature set is still being built out.

## Planned Features
- Context menu management
- File association management
- File Explorer tweaks and cleanup
- Additional Windows configuration improvements for a smoother daily experience

## Downloads
Download release artifacts from the repository [Releases](https://github.com/YeahOSS/WinCraft/releases) page.

### Installer (recommended) — `WinCraft-Setup.exe`

- **Automatic .NET Framework adaptation.** Detects the installed .NET Framework version at install time: prefers `net45` when .NET 4.5+ is available, falls back to `net30` for older built-in-framework systems.
- **Better startup performance.** Release builds do not carry overlay decompression code, reducing both binary size and startup path.

### Portable — `WinCraft-Standard.exe` / `WinCraft-Legacy.exe`

Single-file executables for users who prefer a no-installer experience or need to deploy to a specific framework line directly:

| Package | Target Framework | Supported Windows |
| --- | --- | --- |
| `WinCraft-Standard.exe` | .NET Framework 4.5 | Windows 8, 8.1, 10, 11 |
| `WinCraft-Legacy.exe` | .NET Framework 3.0 | Windows Vista, 7 |

## Security Software Notice
Because WinCraft modifies Windows settings through system APIs, some antivirus or security products may raise false positives — particularly on older release lines where legacy framework heuristics are stricter.

Before running a downloaded build, add the WinCraft folder or executable to your security software allowlist if it gets quarantined or blocked.

## Build From Source
Build and release workflow details live in [publish/README.md](publish/README.md).

## Status
The current repository is mainly the foundation for the product:
- SDK-style multi-target project for `.NET Framework 3.0` and `.NET Framework 4.5`
- Compatibility helpers for APIs that differ across framework versions
- PE overlay single-file packaging for the standalone lines
- NSIS installer with automatic OS adaptation for the recommended distribution
- Versioned local release workflow
