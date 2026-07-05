# WinCraft Directory Guide

## Purpose
Group code by product feature first, by low-level capability second.
No broad dump folders (`Helpers`, `Utils`, `Managers`).

## Project Boundaries

`src/WinCraft/` — thin executable: WPF assets, entry point, manifest, overlay
assembly resolver.  Everything else lives in `src/WinCraft.Core/`.

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
| `src/WinCraft/` | Thin executable project |

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
| Product behaviour (even touching registry/Win32) | `Features/` |
