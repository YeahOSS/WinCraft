<#
.SYNOPSIS
Kills running app processes and waits until their file locks are released.
Called from Directory.Build.targets before Build and Clean because the
shared output directory means a running app instance can lock any DLL.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string[]]$ProcessNames
)

$ErrorActionPreference = "Stop"

$hasTerminatedProcess = $false

foreach ($process in @(Get-Process -Name $ProcessNames -ErrorAction SilentlyContinue)) {
    try {
        if ($process.HasExited) { continue }

        $process.Kill()
        if (-not $process.WaitForExit(5000)) {
            Write-Warning "Timed out stopping $($process.ProcessName) (PID $($process.Id))."
            continue
        }

        $hasTerminatedProcess = $true
    }
    catch [InvalidOperationException] { }
    catch {
        Write-Warning "Could not stop $($process.ProcessName) (PID $($process.Id)): $($_.Exception.Message)"
    }
}

if ($hasTerminatedProcess) { Start-Sleep -Milliseconds 250 }
