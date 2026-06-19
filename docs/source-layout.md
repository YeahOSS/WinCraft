# WinCraft Directory Guide

## Purpose
This project should group code by product feature first and by low-level capability second.
Do not use broad dump folders such as `Helpers`, `Utils`, or `Managers`.

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
- `Interop/`
  Stores COM definitions, CLSID-related interop, and P/Invoke declarations grouped by native library or Windows subsystem.
- `Constants/`
  Stores shared constants such as known registry paths, ProgIDs, CLSIDs, and system option names.

## Placement Rules
- Put framework-gap code in `Compatibility/`, not in `Infrastructure/` or `Features/`.
- Put registry read/write primitives in `Infrastructure/Registry/`.
- Put feature-specific registry rules near the feature that owns them.
- Put P/Invoke signatures in `Interop/NativeMethods/` grouped by DLL.
- Put COM interfaces and activation helpers in `Interop/Com/`.
- Put UI event handling and presentation logic in `UI/`.
- Put product behavior in `Features/`, even when it touches registry, shell, COM, or file paths.

## Examples
- File association behavior: `Features/FileAssociations/`
- Context menu behavior: `Features/ContextMenu/`
- Explorer tweaks: `Features/Explorer/`
- Shared registry wrapper: `Infrastructure/Registry/RegistryValueWriter.cs`
- Shell32 imports: `Interop/NativeMethods/Shell32.cs`
- Missing string APIs: `Compatibility/StringCompat.cs`
