# WinCraft Directory Guide

## Purpose
Group code by product feature first, by low-level capability second.
No broad dump folders (`Helpers`, `Utils`, `Managers`).

## Project Boundaries

`src/WinCraft.Portable/` — portable single-file host: entry point and overlay
assembly resolver. Everything else lives in `src/WinCraft/`.

See `../features/startup-lifecycle.md` for how these projects interact at runtime.

## Main Directories

| Directory | Purpose |
|-----------|---------|
| `Compatibility/` | Framework compatibility shims (`net30` ↔ `net45` gaps only) |
| `UI/` | Windows, dialogs, view models, presentation-layer files |
| `Features/` | Business logic grouped by product area |
| `Startup/` | Process-mode routing, startup composition |
| `Infrastructure/` | Reusable low-level services (registry, file system, diagnostics, security) |
| `Infrastructure/Ipc/` | Cross-process contracts, endpoints, transport helpers |
| `Interop/` | Hand-written Win32 COM interfaces, `[ComImport]` coclasses, P/Invoke CsWin32 can't generate |
| `src/third_party/LzmaSdk/` | Vendored LZMA SDK source subset |
| `src/WinCraft.Portable/` | Portable single-file host project |

## Placement Rules

| What | Where |
|------|-------|
| Framework-gap code | `Compatibility/` |
| Registry read/write primitives | `Infrastructure/RegistryAccess/` |
| Cross-process contracts and endpoints | `Infrastructure/Ipc/` |
| Elevation, token, permission helpers | `Infrastructure/Security/` |
| Shell-command formatting or parsing | `Infrastructure/Shell/` |
| Process-mode routing, UI startup composition | `Startup/` |
| UI event handling, presentation logic | `UI/` |
| WPF application resources | Root `App.xaml` (kept at root for Visual Studio designer resource lookup) |
| Product behaviour (even touching registry/Win32) | `Features/` |

## UI Subdirectories

All C# files under `UI/` use namespace `WinCraft.UI`; subdirectories are physical grouping only.

| What | Where |
|------|-------|
| Windows / UserControls | `UI/Views/`; optional one-level feature subdirectory |
| ViewModels | `UI/ViewModels/`; optional one-level feature subdirectory |
| MVVM primitives | `UI/Mvvm/` |
| Converters | `UI/Converters/` |
| Business-neutral controls | `UI/Controls/` |
| Business-neutral styles, brushes, theme colors | `UI/Styles/` |
