# Installer Guide

How to deploy, configure, and uninstall WinCraft.

## Install Paths

| Installer | Mode | Default path | Requires admin |
|---|---|---|---|
| Setup (WinCraft-Setup.exe) | Current user | `%LOCALAPPDATA%\WinCraft` | No |
| Setup (WinCraft-Setup.exe) | All users | `%PROGRAMFILES%\WinCraft` | Yes |
| MSI (WinCraft-Setup.msi) | Current user | `%LOCALAPPDATA%\WinCraft` | No |
| MSI (WinCraft-Setup.msi) | All users | `%ProgramFiles%\WinCraft` (32-bit); `%ProgramFiles(x86)%\WinCraft` (64-bit) | Yes |

> [!NOTE]
> The MSI is an x86 package, but the application is **AnyCPU** — on 64-bit
> Windows it runs as a native 64-bit process regardless of the install folder.

Both installers carry the Standard (`.NET 4.5+`) and Legacy (`.NET 3.0`)
product lines and auto-detect which to install based on the system's .NET
Framework version.

## Interactive Behaviour

**Shortcuts.**  Desktop and Start Menu shortcuts are offered during install;
both are selected by default.

**Post-install.**  The MSI finish page offers a "Launch WinCraft" checkbox
(checked by default).  The NSIS finish page includes the standard Run and
Show Readme checkboxes.

**Uninstall cleanup.**  Uninstalls ask whether to keep configuration (default:
keep) and delete logs/crash dumps (default: delete).  Upgrade reinstalls
preserve all runtime data.

**Coexistence.**  NSIS and MSI installations can coexist on the same machine.
Both installers register in the standard Windows uninstall location.  The
NSIS installer reads `UninstallString` from the NSIS-created
`SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\WinCraft` key;
the MSI uses its own product-code key and standard `MajorUpgrade`
detection.

## Silent Deployment

### Setup (WinCraft-Setup.exe)

```powershell
# Current-user install
.\WinCraft-Setup.exe /S

# All-users install (run elevated)
.\WinCraft-Setup.exe /S /allusers

# Current-user uninstall
"%LOCALAPPDATA%\WinCraft\Uninstall.exe" /S /currentuser

# All-users uninstall
"%PROGRAMFILES%\WinCraft\Uninstall.exe" /S /allusers
```

Silent all-users installation must be launched from an elevated process.
Without elevation it exits with code `740` — deployment tools can elevate
and retry.

### MSI (WinCraft-Setup.msi)

```powershell
# Current-user install
msiexec /i WinCraft-Setup.msi ALLUSERS=2 MSIINSTALLPERUSER=1 /quiet

# All-users install (run elevated)
msiexec /i WinCraft-Setup.msi ALLUSERS=1 MSIINSTALLPERUSER="" /quiet

# Current-user uninstall (keeps configuration, deletes logs/dumps)
msiexec /x WinCraft-Setup.msi /quiet

# Also delete configuration
msiexec /x WinCraft-Setup.msi REMOVE_CONFIG=1 /quiet

# Keep logs and crash dumps
msiexec /x WinCraft-Setup.msi KEEP_LOGS_DUMPS=1 /quiet

# All-users uninstall (run elevated)
msiexec /x WinCraft-Setup.msi /quiet

# Install with verbose log
msiexec /i WinCraft-Setup.msi /quiet /l*v install.log
```

### MSI Cleanup Properties

| Property | Effect |
|---|---|
| `REMOVE_CONFIG=1` | Remove the entire `%LOCALAPPDATA%\WinCraft` tree |
| `KEEP_LOGS_DUMPS=1` | Skip deleting `Logs\` and `Dumps\` subdirectories |
