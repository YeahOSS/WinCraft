# Publish Workspace

Build scripts and packaging modules.  For deployment commands see
[docs/installer-guide.md](../docs/installer-guide.md); for page flows
and implementation details see [docs/installer-flow.md](../docs/installer-flow.md).

## Layout
- `build.ps1` — build entry point
- `release.ps1` — version bump, build, commit, tag
- `version.props` — three-part version number
- `modules/` — PowerShell modules
- `nsis/` — NSIS scripts
- `wix/` — WiX MSI source and UI assets
- `output/` — distributable files

## Usage
Run the scripts from the repository root:

```powershell
powershell -ExecutionPolicy Bypass -File .\publish\build.ps1
```

| Switch | Effect |
| --- | --- |
| *(none)* | Full build: standalone EXEs + NSIS installer + MSI |
| `-BuildOnly` | Compile only; skip overlay compression and all packaging |
| `-SkipNSIS` | Skip the NSIS installer |
| `-SkipMSI` | Skip the MSI |

The full build requires both NSIS and WiX v4.  Without the required tools,
use the corresponding skip switch — the build fails otherwise.

For a tagged release:

```powershell
powershell -ExecutionPolicy Bypass -File .\publish\release.ps1 -Version 1.2.3
```

`release.ps1` expects a clean git working tree and configured `git user.name` / `git user.email`.
It creates the local release commit and local tag, but it does not push them to the remote repository.

## Prerequisites

### NSIS
The full build requires NSIS 3.x (`makensis`).  Download the `nsis-3.x.zip`
from [SourceForge](https://sourceforge.net/projects/nsis/files/) and extract
it to `tools\nsis\` under the repository root — no installer needed.

All `.nsi` and `.nsh` files must be saved as **UTF-8 with BOM**.  NSIS on
Windows relies on the BOM to detect UTF-8; without it non-ASCII characters
(such as the SimpChinese `LangString` entries) will be parsed as the system
codepage, producing garbled installer text or compile errors.  The build
script validates this before invoking `makensis`.

### WiX (MSI)
Install via the .NET global tool:

```powershell
dotnet tool install --global wix
$wixVersion = (wix --version).Split('+')[0]
wix extension add -g "WixToolset.UI.wixext/$wixVersion"
wix extension add -g "WixToolset.Netfx.wixext/$wixVersion"
wix extension add -g "WixToolset.Util.wixext/$wixVersion"
```

## Packaging

NSIS uninstallers embed a merged file manifest at compile time — no external
file or registry dependency at uninstall time.  The MSI uses standard
Windows Installer file tracking and cleanup custom actions.

Packaging scripts write temporary staging files under `publish/output/staging/`.
That directory is removed at the start of each build and after a successful
build.  The installer license RTF is generated from the repository `LICENSE`;
Markdown documentation is packaged unchanged.

## Output
`publish/build.ps1` creates these files in `publish/output/`:

- `WinCraft-Legacy.exe`
- `WinCraft-Standard.exe`
- `WinCraft-Setup.exe`
- `WinCraft-Setup.msi`
