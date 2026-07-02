Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Import-Module (Join-Path $PSScriptRoot "common.psm1")

$script:RepoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$script:PublishRoot = Split-Path -Parent $PSScriptRoot
$script:SourceRoot = Join-Path $script:RepoRoot "src"
$script:OutputPath = Join-Path $script:PublishRoot "output"
$script:StagingPath = Join-Path $script:OutputPath "staging"

# ------------------------------------------------------------------
# WiX tool resolution
# ------------------------------------------------------------------

function Get-WixExePath {
    <#
    .SYNOPSIS
    Returns the path to wix.exe, or throws with setup instructions.
    #>

    $wixCmd = Get-Command wix -ErrorAction SilentlyContinue
    if ($null -ne $wixCmd) {
        return $wixCmd.Source
    }

    $message = @(
        "WiX v4 build tools were not found.",
        "",
        "Install via dotnet global tool:",
        "  dotnet tool install --global wix"
    ) -join [Environment]::NewLine
    throw $message
}

function Get-WixToolsetVersion {
    param(
        [string]$WixExe
    )

    $versionText = & $WixExe --version
    if ($LASTEXITCODE -ne 0) {
        throw "Could not determine WiX Toolset version."
    }

    return ($versionText -split '\+')[0]
}

function Test-WixExtensionInstalled {
    param(
        [string]$WixExe,
        [string]$ExtensionName,
        [string]$Version
    )

    $extensionOutput = & $WixExe extension list -g 2>&1
    if ($LASTEXITCODE -ne 0) {
        return $false
    }

    $pattern = '^{0}\s+{1}\b' -f [System.Text.RegularExpressions.Regex]::Escape($ExtensionName),
        [System.Text.RegularExpressions.Regex]::Escape($Version)
    return ($extensionOutput -match $pattern)
}

# ------------------------------------------------------------------
# WiX source generation (replaces heat.exe)
# ------------------------------------------------------------------

function Get-StableWixGuid {
    param(
        [string]$Key
    )

    $md5 = [System.Security.Cryptography.MD5]::Create()
    try {
        $bytes = [System.Text.Encoding]::UTF8.GetBytes("WinCraft MSI component:$Key")
        $hash = $md5.ComputeHash($bytes)
    }
    finally {
        $md5.Dispose()
    }

    $hex = -join ($hash | ForEach-Object { $_.ToString("x2") })
    return ("{0}-{1}-{2}-{3}-{4}" -f
        $hex.Substring(0, 8),
        $hex.Substring(8, 4),
        $hex.Substring(12, 4),
        $hex.Substring(16, 4),
        $hex.Substring(20, 12)).ToUpperInvariant()
}

function ConvertTo-WixId {
    param(
        [string]$Value
    )

    $id = [System.Text.RegularExpressions.Regex]::Replace($Value, '[^a-zA-Z0-9_.]', '_')
    if ($id -match '^[0-9]') {
        $id = "_$id"
    }

    return $id
}

function ConvertTo-WixShortFileNamePart {
    param(
        [string]$Value
    )

    $part = [System.Text.RegularExpressions.Regex]::Replace($Value.ToUpperInvariant(), '[^A-Z0-9_$~]', '')
    if ([string]::IsNullOrEmpty($part)) {
        return "FILE"
    }

    return $part
}

function New-WixShortFileNameMap {
    param(
        [string[]]$FileNames
    )

    $md5 = [System.Security.Cryptography.MD5]::Create()
    try {
        $usedShortNames = @{}
        $map = @{}
        foreach ($fileName in @($FileNames | Sort-Object -Unique)) {
            $extension = [System.IO.Path]::GetExtension($fileName).TrimStart('.')
            $shortExtension = ConvertTo-WixShortFileNamePart -Value $extension
            if ($shortExtension.Length -gt 3) {
                $shortExtension = $shortExtension.Substring(0, 3)
            }

            $hashBytes = $md5.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($fileName.ToUpperInvariant()))
            $hash = -join ($hashBytes | ForEach-Object { $_.ToString("X2") })
            $offset = 0
            do {
                $candidateBase = "W" + $hash.Substring($offset, 7)
                $candidate = if ($shortExtension.Length -gt 0) { "$candidateBase.$shortExtension" } else { $candidateBase }
                $offset++
            } while ($usedShortNames.ContainsKey($candidate) -and $offset -le ($hash.Length - 7))

            if ($usedShortNames.ContainsKey($candidate)) {
                throw "Could not generate a unique 8.3 short file name for $fileName."
            }

            $usedShortNames[$candidate] = $true
            $map[$fileName] = $candidate
        }

        return $map
    }
    finally {
        $md5.Dispose()
    }
}

function New-WixHarvestFragment {
    <#
    .SYNOPSIS
    Writes a WiX v4 source fragment that defines one Component per file
    in the staging directory, grouped into a single ComponentGroup.
    This replaces heat.exe dir and has zero external dependencies.
    #>
    param(
        [array]$FileSets,
        [string]$OutputPath,
        [string]$ComponentGroupName,
        [string]$DirectoryId
    )

    $allFileNames = [System.Collections.Generic.List[string]]::new()
    $totalFileCount = 0
    foreach ($fileSet in $FileSets) {
        $files = @(Get-ChildItem -Path $fileSet.Directory -File -ErrorAction SilentlyContinue)
        $totalFileCount += $files.Count
        foreach ($file in $files) {
            $allFileNames.Add($file.Name)
        }
    }

    if ($totalFileCount -eq 0) {
        throw "No files found in MSI staging directories."
    }

    $sb = New-Object System.Text.StringBuilder
    [void]$sb.AppendLine('<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">')
    [void]$sb.AppendLine('  <Fragment>')
    [void]$sb.AppendFormat("    <ComponentGroup Id=""{0}"" Directory=""{1}"">", $ComponentGroupName, $DirectoryId)
    [void]$sb.AppendLine()

    $usedIds = @{}
    $shortFileNames = New-WixShortFileNameMap -FileNames $allFileNames.ToArray()
    foreach ($fileSet in $FileSets) {
        $files = @(Get-ChildItem -Path $fileSet.Directory -File -ErrorAction SilentlyContinue)
        foreach ($file in $files) {
            $sanitised = ConvertTo-WixId -Value "$($fileSet.Name)_$($file.Name)"
            $fileId = "File_" + $sanitised
            $compId = "Comp_" + $sanitised

            $idSuffix = 1
            while ($usedIds.ContainsKey($fileId) -or $usedIds.ContainsKey($compId)) {
                $idSuffix++
                $fileId = "File_$($sanitised)_$idSuffix"
                $compId = "Comp_$($sanitised)_$idSuffix"
            }
            $usedIds[$fileId] = $true
            $usedIds[$compId] = $true

            $guid = Get-StableWixGuid -Key "$($fileSet.Name)\$($file.Name)"
            $source = [System.Security.SecurityElement]::Escape("`$(var.SourceDir)\$($fileSet.Name)\" + $file.Name)
            $shortName = [System.Security.SecurityElement]::Escape($shortFileNames[$file.Name])

            # Mark the .exe as KeyPath so MSI treats it as the component identity.
            $keyPathAttr = ''
            if ($file.Extension -eq '.exe') {
                $keyPathAttr = ' KeyPath="yes"'
            }

            $conditionAttr = ''
            if (-not [string]::IsNullOrWhiteSpace($fileSet.Condition)) {
                $conditionAttr = ' Condition="' + [System.Security.SecurityElement]::Escape($fileSet.Condition) + '"'
            }

            [void]$sb.AppendFormat("      <Component Id=""{0}"" Guid=""{1}""{2}>", $compId, $guid, $conditionAttr)
            [void]$sb.AppendLine()
            [void]$sb.AppendFormat("        <File Id=""{0}"" Source=""{1}"" ShortName=""{2}""{3} />", $fileId, $source, $shortName, $keyPathAttr)
            [void]$sb.AppendLine()
            [void]$sb.AppendLine('      </Component>')
        }
    }

    [void]$sb.AppendLine('    </ComponentGroup>')
    [void]$sb.AppendLine('  </Fragment>')
    [void]$sb.AppendLine('</Wix>')

    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($OutputPath, $sb.ToString(), $utf8NoBom)
}

# ------------------------------------------------------------------
# WiX UI support files
function Invoke-MsiSql {
    param(
        [object]$Database,
        [string]$Query
    )

    $view = $Database.GetType().InvokeMember("OpenView", "InvokeMethod", $null, $Database, @($Query))
    try {
        $view.GetType().InvokeMember("Execute", "InvokeMethod", $null, $view, $null) | Out-Null
    }
    finally {
        if ($null -ne $view) {
            $view.GetType().InvokeMember("Close", "InvokeMethod", $null, $view, $null) | Out-Null
            [System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($view) | Out-Null
        }
    }
}

function Update-WinCraftMsiUiTables {
    <#
    .SYNOPSIS
    Removes default WixUI navigation rows that conflict with WinCraft pages.
    #>
    param(
        [string]$MsiPath
    )

    $installer = $null
    $database = $null
    $installer = New-Object -ComObject WindowsInstaller.Installer
    $database = $installer.GetType().InvokeMember("OpenDatabase", "InvokeMethod", $null, $installer, @($MsiPath, 1))

    try {
        $deleteControlEvents = @(
            @{ Dialog = "LicenseAgreementDlg"; Control = "Next"; Event = "NewDialog"; Argument = "InstallDirDlg" },
            @{ Dialog = "InstallDirDlg"; Control = "Back"; Event = "NewDialog"; Argument = "LicenseAgreementDlg" },
            @{ Dialog = "InstallDirDlg"; Control = "Next"; Event = "NewDialog"; Argument = "VerifyReadyDlg" },
            @{ Dialog = "VerifyReadyDlg"; Control = "Back"; Event = "NewDialog"; Argument = "InstallDirDlg" },
            @{ Dialog = "MaintenanceWelcomeDlg"; Control = "Next"; Event = "NewDialog"; Argument = "MaintenanceTypeDlg" },
            @{ Dialog = "VerifyReadyDlg"; Control = "Back"; Event = "NewDialog"; Argument = "MaintenanceTypeDlg" }
        )

        foreach ($row in $deleteControlEvents) {
            Invoke-MsiSql -Database $database -Query (
                "DELETE FROM ``ControlEvent`` WHERE ``Dialog_``='{0}' AND ``Control_``='{1}' AND ``Event``='{2}' AND ``Argument``='{3}'" -f
                $row.Dialog,
                $row.Control,
                $row.Event,
                $row.Argument
            )
        }

        Invoke-MsiSql -Database $database -Query (
            "UPDATE ``InstallUISequence`` SET ``Condition``='Installed AND RESUME' WHERE ``Action``='ResumeDlg' AND ``Condition``='Installed AND (RESUME OR Preselected)'"
        )

        $database.GetType().InvokeMember("Commit", "InvokeMethod", $null, $database, $null) | Out-Null
    }
    finally {
        if ($null -ne $database) {
            [System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($database) | Out-Null
        }
        if ($null -ne $installer) {
            [System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($installer) | Out-Null
        }
    }
}

# ------------------------------------------------------------------
# MSI build
# ------------------------------------------------------------------

function New-MSIInstaller {
    param(
        [string]$Configuration,
        [string]$ProjectRoot,
        [string]$ArtifactName
    )

    Write-Step "Packaging MSI installer"

    # -- Resolve WiX ----------------------------------------------------
    $wixExe = Get-WixExePath

    # -- Prepare version ------------------------------------------------
    # WiX v4 requires a 4-part version (x.x.x.x).
    # version.props carries a 3-part string — append .0.
    $version = Get-VersionString
    $packageVersion = "$version.0"
    $wixToolsetVersion = Get-WixToolsetVersion -WixExe $wixExe
    $wixUiExtensionRef = "WixToolset.UI.wixext/$wixToolsetVersion"
    $wixNetFxExtensionRef = "WixToolset.Netfx.wixext/$wixToolsetVersion"
    $wixUtilExtensionRef = "WixToolset.Util.wixext/$wixToolsetVersion"
    foreach ($extensionRef in @($wixUiExtensionRef, $wixNetFxExtensionRef, $wixUtilExtensionRef)) {
        $extensionName = ($extensionRef -split '/')[0]
        if (-not (Test-WixExtensionInstalled -WixExe $wixExe -ExtensionName $extensionName -Version $wixToolsetVersion)) {
            throw "WiX extension $extensionRef was not found. Install it with: wix extension add -g `"$extensionRef`""
        }
    }

    # -- Stage files ----------------------------------------------------
    # Match the NSIS installer: prefer the Standard net45 line when .NET
    # Framework 4.5+ is available, otherwise install the Legacy net30 line.

    $stagingDir = Join-Path $script:StagingPath "msi"
    $staging = New-InstallerStaging -Configuration $Configuration -ProjectRoot $ProjectRoot -StagingRoot $stagingDir -Label "MSI"
    $standardDirectory = $staging.StandardDirectory
    $legacyDirectory   = $staging.LegacyDirectory
    $commonDirectory   = $staging.CommonDirectory
    $fileCount         = $staging.FileCount
    $licenseRtf        = $staging.LicenseRtfPath

    # -- Generate WiX source fragment -----------------------------------
    # Pure PowerShell — no heat.exe required.
    $filesWxs = Join-Path $stagingDir "Files.wxs"
    $fileSets = @(
        [pscustomobject]@{
            Name = "Common"
            Directory = $commonDirectory
            Condition = ""
        },
        [pscustomobject]@{
            Name = "Standard"
            Directory = $standardDirectory
            Condition = "WIX_IS_NETFRAMEWORK_45_OR_LATER_INSTALLED"
        },
        [pscustomobject]@{
            Name = "Legacy"
            Directory = $legacyDirectory
            Condition = "NOT WIX_IS_NETFRAMEWORK_45_OR_LATER_INSTALLED"
        }
    )

    New-WixHarvestFragment -FileSets $fileSets `
                           -OutputPath $filesWxs `
                           -ComponentGroupName "ProductFiles" `
                           -DirectoryId "APPLICATIONFOLDER"

    Assert-PathExists -Path $filesWxs -Description "Generated file fragment"

    # -- Build MSI with wix ---------------------------------------------
    $productWxs = Join-Path $script:PublishRoot "wix\product.wxs"
    $outputMsi  = Join-Path $script:OutputPath $ArtifactName
    $wixIntermediate = Join-Path $stagingDir "wix"
    $wixPdb = Join-Path $stagingDir "$([System.IO.Path]::GetFileNameWithoutExtension($ArtifactName)).wixpdb"
    $dialogBitmap = Join-Path $script:RepoRoot "assets\wix-dialog.bmp"
    $bannerBitmap = Join-Path $script:RepoRoot "assets\wix-banner.bmp"

    Assert-PathExists -Path $productWxs -Description "WiX package source"
    Assert-PathExists -Path $dialogBitmap -Description "WiX dialog bitmap"
    Assert-PathExists -Path $bannerBitmap -Description "WiX banner bitmap"

    $stringsWxl = Join-Path $script:PublishRoot "wix\strings.wxl"
    Assert-PathExists -Path $stringsWxl -Description "WixUI strings override"

    if (Test-Path -LiteralPath $outputMsi) {
        Remove-Item -LiteralPath $outputMsi -Force -ErrorAction SilentlyContinue
    }

    $buildArgs = @(
        "build",
        "-o", $outputMsi,
        "-arch", "x86",
        "-d", "Version=$packageVersion",
        "-d", "DisplayVersion=$version",
        "-d", "SourceDir=$stagingDir",
        "-d", "RepoRoot=$($script:RepoRoot)",
        "-d", "LicenseRtf=$licenseRtf",
        "-d", "DialogBitmap=$dialogBitmap",
        "-d", "BannerBitmap=$bannerBitmap",
        "-ext", $wixUiExtensionRef,
        "-ext", $wixNetFxExtensionRef,
        "-ext", $wixUtilExtensionRef,
        "-loc", $stringsWxl,
        "-intermediatefolder", $wixIntermediate,
        "-pdb", $wixPdb,
        "-dcl", "high",
        $productWxs,
        $filesWxs
    )

    $wixOutput = & $wixExe $buildArgs 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host ($wixOutput | Out-String)
        throw "wix build failed (exit code $LASTEXITCODE)."
    }
    Assert-PathExists -Path $outputMsi -Description "MSI artifact"
    Update-WinCraftMsiUiTables -MsiPath $outputMsi
}

Export-ModuleMember -Function New-MSIInstaller
