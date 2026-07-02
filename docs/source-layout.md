# WinCraft Directory Guide

## Purpose
This project should group code by product feature first and by low-level capability second.
Do not use broad dump folders such as `Helpers`, `Utils`, or `Managers`.
This document defines code placement and directory boundaries only. Keep development behavior and naming policy in `docs/coding-style.md`; compatibility and interop policy live in their dedicated docs.

## Project Boundaries

`src/WinCraft/` is the thin executable project. Keep it limited to WPF-specific
assets and types, the executable entry point, app manifest/configuration, and
the overlay assembly resolver that must run before bundled dependencies are
loaded.

`src/WinCraft.Core/` owns product logic, startup orchestration beneath the thin
entry point, compatibility shims, Win32 interop call sites, diagnostics,
security, IPC, registry access, and other reusable services. Moving non-WPF
logic into Core keeps the executable PE small so publish packaging can bundle
Core into the compressed overlay.

## Main Directories

| Directory | Purpose |
|-----------|---------|
| `Compatibility/` | Framework compatibility shims — only code bridging `net30` ↔ `net45` gaps |
| `UI/` | Windows, dialogs, view models, presentation-layer files |
| `Features/` | Business logic grouped by product area (file associations, context menus, Explorer, system settings) |
| `Startup/` | Process-mode routing and startup composition below the thin executable entry point |
| `Infrastructure/` | Reusable low-level services (registry access, file system, diagnostics, security) |
| `Infrastructure/Ipc/` | Cross-process contracts, endpoints, transport helpers |
| `Interop/` | Hand-written Win32 COM interfaces, `[ComImport]` coclasses, P/Invoke CsWin32 can't generate |
| `Constants/` | Shared constants (registry paths, ProgIDs, CLSIDs, system option names) |
| `src/third_party/LzmaSdk/` | Vendored LZMA SDK source subset |
| `src/WinCraft/` | Thin executable project: WPF assets, entry point, overlay resolver |

## Placement Rules

| What | Where |
|------|-------|
| Framework-gap code | `Compatibility/` |
| Registry read/write primitives | `Infrastructure/RegistryAccess/` |
| Cross-process contracts and endpoints | `Infrastructure/Ipc/` |
| Elevation, token, permission helpers | `Infrastructure/Security/` |
| Shell-command formatting or parsing | `Infrastructure/Shell/` |
| Process-mode routing, UI startup composition | `Startup/` |
| Feature-specific registry rules | Feature that owns them |
| UI event handling, presentation logic | `UI/` |
| Product behavior (even touching registry/Win32) | `Features/` |
| High-level interop usage | Product code; configure bindings via project-level `NativeMethods.txt` / `NativeMethods.json` |

