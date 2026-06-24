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
flat binary container, LZMA-compressed, and appended after the last PE section
of the executable.  The PE header and sections stay in their original form,
avoiding antivirus false positives.

At runtime `OverlayAssemblyResolver` registers an `AssemblyResolve` handler
that reads the container from the exe file, decompresses it in memory, and
serves assemblies on demand.

The LZMA overlay ends with a fixed footer:

- 5 bytes of LZMA decoder properties.
- 8 bytes containing the uncompressed container length.
- 4 bytes containing the compressed payload length.
- 4 bytes containing the `WOLZ` magic value.

`OverlayAssemblyResolver` still recognizes the previous Deflate `WOVL` footer
for compatibility, but new publish artifacts are written as LZMA overlays.

Most application code lives in `WinCraft.Core.dll`.  The executable project is
kept as a thin WPF/entry-point shell so Core can be bundled and compressed in
the overlay instead of expanding the executable PE sections.

`src/third_party/LzmaSdk/` contains the vendored LZMA SDK source subset. Source,
version, file-selection, and update notes live in `src/third_party/LzmaSdk/README.md`.

The main project restores NuGet dependencies through `PackageReference`.
Version numbers come from `src/Version.props` and are applied to assembly metadata.

## Output
`publish/build.ps1` creates these files in `publish/output/`:

- `WinCraft-Legacy.exe`
- `WinCraft-Standard.exe`
- `WinCraft-Full.zip`
