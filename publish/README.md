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

The main project restores NuGet dependencies through `PackageReference`, including the ILRepack MSBuild task, so no tracked merge tool is required in the repository.
The custom merge configuration lives in `src/WinCraft/ILRepack.targets`.
It auto-discovers all `.dll` files in the build output directory — no manual updates are needed when NuGet dependencies change.
Version numbers come from `src/Version.props` and are applied to assembly metadata.

After merge, `build.ps1` verifies that the merged executable references only
GAC-resident framework assemblies. If a non-GAC assembly is still referenced,
the build fails with the name and version of the missing dependency.

## Output
`publish/build.ps1` creates these files in `publish/output/`:

- `WinCraft-Legacy.exe`
- `WinCraft-Standard.exe`
- `WinCraft-Full.zip`
