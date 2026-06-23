# WinCraft Directory Guide

## Purpose
This project should group code by product feature first and by low-level capability second.
Do not use broad dump folders such as `Helpers`, `Utils`, or `Managers`.
This document defines code placement and directory boundaries only. Keep development behavior and naming policy in `AGENTS.md`; compatibility and interop policy live in their dedicated docs.

## Main Directories
- `Compatibility/`
  Stores framework compatibility shims only.
  Put code here only when it exists to bridge differences between `net30` and `net45`.
- `UI/`
  Stores windows, dialogs, view models, and other presentation-layer files.
- `Features/`
  Stores business logic grouped by product area such as file associations, context menus, Explorer, and system settings.
- `Infrastructure/`
  Stores reusable low-level services such as registry access, file system access, diagnostics, and security helpers.
- `Infrastructure/Ipc/`
  Stores reusable cross-process contracts, endpoints, and transport helpers.
- `Constants/`
  Stores shared constants such as known registry paths, ProgIDs, CLSIDs, and system option names.

## Placement Rules
- Put framework-gap code in `Compatibility/`, not in `Infrastructure/` or `Features/`.
- Put registry read/write primitives in `Infrastructure/RegistryAccess/`.
- Put cross-process contracts and endpoint helpers in `Infrastructure/Ipc/`.
- Put elevation, token, and permission helpers in `Infrastructure/Security/`.
- Put reusable shell-command formatting or parsing helpers in `Infrastructure/Shell/`.
- Put feature-specific registry rules near the feature that owns them.
- Keep high-level interop usage in product code, but configure generated Win32 bindings through the project-level `NativeMethods.txt` and `NativeMethods.json` files. The CsWin32 workflow is documented in `docs/win32-interop.md`.
- Put UI event handling and presentation logic in `UI/`.
- Put product behavior in `Features/`, even when it touches registry, shell, Win32, or file paths.

## Examples
- File association behavior: `Features/FileAssociations/`
- Context menu behavior: `Features/ContextMenu/`
- Explorer tweaks: `Features/Explorer/`
- Shared registry wrapper: `Infrastructure/RegistryAccess/WindowsRegistryWriter.cs`
- Missing string APIs: `Compatibility/StringCompat.cs`
