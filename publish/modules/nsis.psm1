Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Import-Module (Join-Path $PSScriptRoot "common.psm1")

$script:RepoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$script:PublishRoot = Split-Path -Parent $PSScriptRoot
$script:OutputPath = Join-Path $script:PublishRoot "output"
$script:StagingPath = Join-Path $script:OutputPath "staging"

function Assert-NSISFileEncoding {
    param(
        [string]$FilePath
    )

    $bytes = [System.IO.File]::ReadAllBytes($FilePath)
    if ($bytes.Length -lt 3 -or $bytes[0] -ne 0xEF -or $bytes[1] -ne 0xBB -or $bytes[2] -ne 0xBF) {
        $fileName = Split-Path -Leaf $FilePath
        throw "$fileName is not UTF-8 with BOM. NSIS requires UTF-8 with BOM encoding for .nsi/.nsh files that contain non-ASCII characters. Save the file as UTF-8 with BOM and try again."
    }
}

function Get-NSISCompilerPath {
    $nsisPath = Join-Path $script:RepoRoot "tools\nsis\makensis.exe"
    if (Test-Path -LiteralPath $nsisPath) {
        return $nsisPath
    }

    throw "NSIS was not found at tools\nsis\makensis.exe. Download nsis-3.x.zip from SourceForge and extract it to tools\nsis\ under the repository root."
}

function New-NSISInstaller {
    param(
        [string]$Configuration,
        [string]$ProjectRoot,
        [string]$ArtifactName
    )

    $makensisPath = Get-NSISCompilerPath

    Write-Step "Packaging Full NSIS installer"

    $stagingRoot = Join-Path $script:StagingPath "nsis"
    $staging = New-InstallerStaging -Configuration $Configuration -ProjectRoot $ProjectRoot -StagingRoot $stagingRoot -Label "NSIS"
    $standardDirectory = $staging.StandardDirectory
    $legacyDirectory   = $staging.LegacyDirectory
    $commonDirectory   = $staging.CommonDirectory
    $licenseRtf        = $staging.LicenseRtfPath

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
    $currentUserUninstallerScriptPath = Join-Path $script:PublishRoot "nsis\currentuser-uninstaller.nsi"
    Assert-PathExists -Path $currentUserUninstallerScriptPath -Description "NSIS current-user uninstaller script"
    $iconPath = Join-Path $script:RepoRoot "assets\app.ico"
    Assert-PathExists -Path $iconPath -Description "Installer icon"
    $dialogBitmapPath = Join-Path $script:RepoRoot "assets\nsis-dialog.bmp"
    Assert-PathExists -Path $dialogBitmapPath -Description "NSIS dialog bitmap"
    $bannerBitmapPath = Join-Path $script:RepoRoot "assets\nsis-banner.bmp"
    Assert-PathExists -Path $bannerBitmapPath -Description "NSIS banner bitmap"

    # NSIS on Windows requires UTF-8 with BOM for scripts containing
    # non-ASCII characters (e.g. SimpChinese LangStrings).  Fail early
    # with a clear message instead of letting makensis produce garbled
    # output or a cryptic parse error.
    $nsisScriptPaths = @(
        $scriptPath,
        $allUsersUninstallerScriptPath,
        $currentUserUninstallerScriptPath,
        (Join-Path $script:PublishRoot "nsis\uninstall-common.nsh"),
        (Join-Path $script:PublishRoot "nsis\strings.nsh")
    )
    foreach ($nsiPath in $nsisScriptPaths) {
        Assert-NSISFileEncoding -FilePath $nsiPath
    }

    $version = Get-VersionString
    $allUsersUninstallerPath = Join-Path $stagingRoot "Uninstall-AllUsers.exe"
    $currentUserUninstallerPath = Join-Path $stagingRoot "Uninstall-CurrentUser.exe"

    $currentUserUninstallerArgs = @(
        "/DSourceDir=$stagingRoot",
        "/DOutFile=$currentUserUninstallerPath",
        "/DIconPath=$iconPath",
        "/DBannerBitmapPath=$bannerBitmapPath",
        "/DVersion=$version"
    )

    $nsisOutput = & $makensisPath $currentUserUninstallerArgs $currentUserUninstallerScriptPath 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host ($nsisOutput | Out-String)
        throw "NSIS current-user uninstaller build failed."
    }

    $allUsersUninstallerArgs = @(
        "/DSourceDir=$stagingRoot",
        "/DOutFile=$allUsersUninstallerPath",
        "/DIconPath=$iconPath",
        "/DBannerBitmapPath=$bannerBitmapPath",
        "/DVersion=$version"
    )

    $nsisOutput = & $makensisPath $allUsersUninstallerArgs $allUsersUninstallerScriptPath 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host ($nsisOutput | Out-String)
        throw "NSIS all-users uninstaller build failed."
    }

    $nsisArgs = @(
        "/DSourceDir=$stagingRoot",
        "/DOutFile=$artifactPath",
        "/DIconPath=$iconPath",
        "/DDialogBitmapPath=$dialogBitmapPath",
        "/DBannerBitmapPath=$bannerBitmapPath",
        "/DLicensePath=$licenseRtf",
        "/DVersion=$version",
        "/DAllUsersUninstallerPath=$allUsersUninstallerPath",
        "/DCurrentUserUninstallerPath=$currentUserUninstallerPath"
    )
    if (@(Get-ChildItem -Path $commonDirectory -File -ErrorAction SilentlyContinue).Count -gt 0) {
        $nsisArgs += "/DHasCommon"
    }
    $nsisOutput = & $makensisPath $nsisArgs $scriptPath 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host ($nsisOutput | Out-String)
        throw "NSIS installer build failed."
    }

}

Export-ModuleMember -Function New-NSISInstaller
