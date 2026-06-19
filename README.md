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
Download release artifacts from the repository Releases page.

Current package lines:

| Package | Best for | Directly usable on a clean system | Notes |
| --- | --- | --- | --- |
| `WinCraft-Legacy.exe` | Older Windows installations | Windows Vista and Windows 7 | Built on `.NET Framework 3.0` for the legacy compatibility line |
| `WinCraft-Standard.exe` | Modern Windows installations | Windows 8, Windows 8.1, Windows 10, and Windows 11 | Built on `.NET Framework 4.5` for the standard line |
| `WinCraft-Full.zip` | Maximum deployment fallback | Windows Vista through Windows 11 | Contains the Legacy executable plus `WinCraft.exe.config` |

## Security Software Notice
Because WinCraft modifies Windows settings and the release pipeline produces merged single-file executables, some antivirus or security products may raise false positives.

Before running a downloaded build, add the WinCraft folder or executable to your security software allowlist if it gets quarantined or blocked.

## Build From Source
Build and release workflow details live in [publish/README.md](publish/README.md).

## Status
The current repository is mainly the foundation for the product:
- SDK-style multi-target project for `.NET Framework 3.0` and `.NET Framework 4.5`
- Compatibility helpers for APIs that differ across framework versions
- Single-file release packaging for the Legacy and Standard lines
- Versioned local release workflow for future public releases
