# Publish Workspace

## Layout
- `build.ps1` is the publish entry point.
- `release.ps1` updates the version, builds artifacts, creates the release commit, and creates the git tag.
- `output/` contains the final distributable files.

## Usage
Run the scripts from the repository root:

```powershell
powershell -ExecutionPolicy Bypass -File .\publish\build.ps1
```

For a tagged release:

```powershell
powershell -ExecutionPolicy Bypass -File .\publish\release.ps1 -Version 1.2.3
```

`release.ps1` expects a clean git working tree and configured `git user.name` / `git user.email`.
It creates the local release commit and local tag, but it does not push them to the remote repository.

## Packaging

Both targets use PE overlay packaging: dependency DLLs are bundled into a
flat binary container, Deflate-compressed, and appended after the last PE
section of the executable.  The PE header and sections stay in their original
form, avoiding antivirus false positives.

At runtime `OverlayAssemblyResolver` registers an `AssemblyResolve` handler
that reads the container from the exe file, decompresses it in memory, and
serves assemblies on demand.

Most application code lives in `WinCraft.Core.dll`.  The executable project is
kept as a thin WPF/entry-point shell so Core can be bundled and compressed in
the overlay instead of expanding the executable PE sections.

The main project restores NuGet dependencies through `PackageReference`.
Version numbers come from `src/Version.props` and are applied to assembly metadata.

## Output
`publish/build.ps1` creates these files in `publish/output/`:

- `WinCraft-Legacy.exe`
- `WinCraft-Standard.exe`
- `WinCraft-Full.zip`
