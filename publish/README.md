# Publish Workspace

## Layout
- `build.ps1` — build entry point
- `release.ps1` — version bump, build, commit, tag
- `version.props` — three-part version number
- `modules/` — PowerShell modules
- `nsis/` — NSIS scripts
- `output/` — distributable files

## Usage
Run the scripts from the repository root:

```powershell
# Full build with overlay packaging and NSIS installer
powershell -ExecutionPolicy Bypass -File .\publish\build.ps1

# Compile-only (skip overlay compression and NSIS packaging)
powershell -ExecutionPolicy Bypass -File .\publish\build.ps1 -BuildOnly
```

For a tagged release:

```powershell
powershell -ExecutionPolicy Bypass -File .\publish\release.ps1 -Version 1.2.3
```

`release.ps1` expects a clean git working tree and configured `git user.name` / `git user.email`.
It creates the local release commit and local tag, but it does not push them to the remote repository.

The full build requires NSIS 3.x (`makensis`).  Download the `nsis-3.x.zip`
from [SourceForge](https://sourceforge.net/projects/nsis/files/) and extract
it to `tools\nsis\` under the repository root — no installer needed.

## Packaging

Both uninstallers embed a merged file manifest at compile time — no external
file or registry dependency at uninstall time.

## NSIS Installer

| Mode | Default path | Registry | Requires admin |
|---|---|---|---|
| Current user | `%LOCALAPPDATA%\WinCraft` | HKCU | No |
| All users | `%PROGRAMFILES%\WinCraft` | HKLM | Yes |

## Silent Deployment

```powershell
# Per-user
.\WinCraft-Setup.exe /S

# All-users (run elevated)
.\WinCraft-Setup.exe /S /allusers

# Uninstall
"%LOCALAPPDATA%\WinCraft\Uninstall.exe" /S /currentuser
"%PROGRAMFILES%\WinCraft\Uninstall.exe" /S /allusers
```

Silent all-users installation must be launched from an elevated process.  If
`WinCraft-Setup.exe /S /allusers` is started without administrator rights, it
does not show UAC or relaunch itself; it exits with code `740` so deployment
tools can elevate and retry explicitly.

## Output
`publish/build.ps1` creates these files in `publish/output/`:

- `WinCraft-Legacy.exe`
- `WinCraft-Standard.exe`
- `WinCraft-Setup.exe`
