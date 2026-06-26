Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Import-Module (Join-Path $PSScriptRoot "common.psm1")

$script:RepoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$script:PublishRoot = Split-Path -Parent $PSScriptRoot
$script:OutputPath = Join-Path $script:PublishRoot "output"

function Get-NSISCompilerPath {
    $nsisPath = Join-Path $script:RepoRoot "tools\nsis\makensis.exe"
    if (Test-Path -LiteralPath $nsisPath) {
        return $nsisPath
    }

    throw "NSIS was not found at tools\nsis\makensis.exe. Download nsis-3.x.zip from SourceForge and extract it to tools\nsis\ under the repository root."
}

function Get-FullPackageFiles {
    param(
        [string]$Configuration,
        [string]$ProjectRoot,
        [string]$TargetSubdirectory
    )

    $buildOutputDirectory = Join-Path $ProjectRoot "bin\$Configuration\$TargetSubdirectory"
    Assert-PathExists -Path $buildOutputDirectory -Description "$TargetSubdirectory build output directory"

    $packagePaths = @(Get-ChildItem -Path $buildOutputDirectory -File |
        Where-Object { $_.Extension -in @(".exe", ".dll", ".config") } |
        Select-Object -ExpandProperty FullName)

    if ($packagePaths.Count -eq 0) {
        throw "No files were found for the $TargetSubdirectory Full package."
    }

    return $packagePaths
}

function Copy-FullPackageFiles {
    param(
        [string]$Configuration,
        [string]$ProjectRoot,
        [string]$TargetSubdirectory,
        [string]$DestinationDirectory
    )

    $packagePaths = Get-FullPackageFiles -Configuration $Configuration -ProjectRoot $ProjectRoot -TargetSubdirectory $TargetSubdirectory
    New-Item -ItemType Directory -Path $DestinationDirectory -Force | Out-Null

    foreach ($packagePath in $packagePaths) {
        Copy-Item -LiteralPath $packagePath -Destination (Join-Path $DestinationDirectory (Split-Path $packagePath -Leaf)) -Force
    }

    return $packagePaths.Count
}

function New-NSISInstaller {
    param(
        [string]$Configuration,
        [string]$ProjectRoot,
        [string]$ArtifactName
    )

    $makensisPath = Get-NSISCompilerPath

    Write-Step "Packaging Full NSIS installer"

    $stagingRoot = Join-Path $script:OutputPath "full-installer"
    if (Test-Path -LiteralPath $stagingRoot) {
        Remove-Item -LiteralPath $stagingRoot -Recurse -Force
    }

    New-Item -ItemType Directory -Path $stagingRoot -Force | Out-Null

    $standardDirectory = Join-Path $stagingRoot "Standard"
    $legacyDirectory = Join-Path $stagingRoot "Legacy"
    $commonDirectory = Join-Path $stagingRoot "Common"
    Copy-FullPackageFiles -Configuration $Configuration -ProjectRoot $ProjectRoot -TargetSubdirectory "net45" -DestinationDirectory $standardDirectory | Out-Null
    Copy-FullPackageFiles -Configuration $Configuration -ProjectRoot $ProjectRoot -TargetSubdirectory "net30" -DestinationDirectory $legacyDirectory | Out-Null

    # Deduplicate identical files across both TFM directories into Common.
    New-Item -ItemType Directory -Path $commonDirectory -Force | Out-Null
    $dedupCount = 0
    $standardFiles = @(Get-ChildItem -Path $standardDirectory -File)
    foreach ($file in $standardFiles) {
        $legacyPath = Join-Path $legacyDirectory $file.Name
        if ((Test-Path $legacyPath) -and ((Get-FileHash $file.FullName).Hash -eq (Get-FileHash $legacyPath).Hash)) {
            Move-Item -LiteralPath $file.FullName -Destination (Join-Path $commonDirectory $file.Name) -Force
            Remove-Item -LiteralPath $legacyPath -Force
            $dedupCount++
        }
    }

    if ($dedupCount -gt 0) {
        Write-Host "==> Deduplicated $dedupCount file(s) to Common staging"
    }

    # Bundle LICENSE, README and third-party notices alongside the installed files.
    # LICENSE has no extension — append .txt so Windows opens it with Notepad.
    $licensePath = Join-Path $script:RepoRoot "LICENSE"
    $readmePath = Join-Path $script:RepoRoot "README.md"
    $thirdPartyLicensesPath = Join-Path $script:RepoRoot "docs\OPEN-SOURCE-LICENSES.md"
    Copy-Item -LiteralPath $licensePath -Destination (Join-Path $commonDirectory "LICENSE.txt") -Force
    Copy-Item -LiteralPath $readmePath -Destination $commonDirectory -Force
    Copy-Item -LiteralPath $thirdPartyLicensesPath -Destination $commonDirectory -Force

    # Build a merged manifest of all possible files so the uninstaller can
    # clean up regardless of which build line (Standard / Legacy) was installed.
    # The manifest is embedded into both uninstallers at compile time via
    # the NSIS File instruction — no external dependency at uninstall time.
    $mergedNames = [System.Collections.Generic.SortedSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($dir in @($standardDirectory, $legacyDirectory, $commonDirectory)) {
        foreach ($file in Get-ChildItem -Path $dir -File -ErrorAction SilentlyContinue) {
            [void]$mergedNames.Add($file.Name)
        }
    }
    [void]$mergedNames.Add("Uninstall.exe")
    $mergedManifestPath = Join-Path $stagingRoot "merged-manifest.txt"
    Set-Content -LiteralPath $mergedManifestPath -Value ([string[]]$mergedNames) -Encoding ASCII

    $artifactPath = Join-Path $script:OutputPath $ArtifactName
    if (Test-Path -LiteralPath $artifactPath) {
        Remove-Item -LiteralPath $artifactPath -Force
    }

    $scriptPath = Join-Path $script:PublishRoot "nsis\installer.nsi"
    Assert-PathExists -Path $scriptPath -Description "NSIS installer script"
    $allUsersUninstallerScriptPath = Join-Path $script:PublishRoot "nsis\allusers-uninstaller.nsi"
    Assert-PathExists -Path $allUsersUninstallerScriptPath -Description "NSIS all-users uninstaller script"
    $iconPath = Join-Path $script:RepoRoot "assets\app.ico"
    Assert-PathExists -Path $iconPath -Description "Installer icon"

    $version = Get-VersionString
    $allUsersUninstallerPath = Join-Path $stagingRoot "Uninstall-AllUsers.exe"
    $currentUserUninstallerPath = Join-Path $stagingRoot "Uninstall-CurrentUser.exe"
    $currentUserUninstallerScriptPath = Join-Path $script:PublishRoot "nsis\currentuser-uninstaller.nsi"
    Assert-PathExists -Path $currentUserUninstallerScriptPath -Description "NSIS current-user uninstaller script"

    $currentUserUninstallerArgs = @(
        "/DSourceDir=$stagingRoot",
        "/DOutFile=$currentUserUninstallerPath",
        "/DIconPath=$iconPath",
        "/DVersion=$version"
    )

    & $makensisPath $currentUserUninstallerArgs $currentUserUninstallerScriptPath | Out-Host

    if ($LASTEXITCODE -ne 0) {
        throw "NSIS current-user uninstaller build failed."
    }

    $allUsersUninstallerArgs = @(
        "/DSourceDir=$stagingRoot",
        "/DOutFile=$allUsersUninstallerPath",
        "/DIconPath=$iconPath",
        "/DVersion=$version"
    )

    & $makensisPath $allUsersUninstallerArgs $allUsersUninstallerScriptPath | Out-Host

    if ($LASTEXITCODE -ne 0) {
        throw "NSIS all-users uninstaller build failed."
    }

    $nsisArgs = @(
        "/DSourceDir=$stagingRoot",
        "/DOutFile=$artifactPath",
        "/DIconPath=$iconPath",
        "/DVersion=$version",
        "/DAllUsersUninstallerPath=$allUsersUninstallerPath",
        "/DCurrentUserUninstallerPath=$currentUserUninstallerPath"
    )
    if (@(Get-ChildItem -Path $commonDirectory -File -ErrorAction SilentlyContinue).Count -gt 0) {
        $nsisArgs += "/DHasCommon"
    }
    & $makensisPath $nsisArgs $scriptPath | Out-Host

    if ($LASTEXITCODE -ne 0) {
        throw "NSIS installer build failed."
    }

    Remove-Item -LiteralPath $stagingRoot -Recurse -Force
}

Export-ModuleMember -Function New-NSISInstaller
