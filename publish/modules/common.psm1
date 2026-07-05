Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$script:RepoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$script:SourceRoot = Join-Path $script:RepoRoot "src"
$script:VersionPropsPath = Join-Path (Split-Path -Parent $PSScriptRoot) "version.props"

function Write-Step {
    param(
        [string]$Message
    )

    Write-Host ""
    Write-Host "==> $Message"
}

function Assert-PathExists {
    param(
        [string]$Path,
        [string]$Description
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "$Description was not found: $Path"
    }
}

function Assert-CommandExists {
    param(
        [string]$CommandName
    )

    $command = Get-Command $CommandName -ErrorAction SilentlyContinue

    if ($null -eq $command) {
        throw "Required command was not found: $CommandName"
    }
}

function Get-VersionString {
    Assert-PathExists -Path $script:VersionPropsPath -Description "Version props file"

    [xml]$versionDocument = Get-Content -LiteralPath $script:VersionPropsPath
    $propertyGroup = $versionDocument.Project.PropertyGroup

    if ($null -eq $propertyGroup) {
        throw "Version.props must define a PropertyGroup."
    }

    $major = [string]$propertyGroup.VersionMajor
    $minor = [string]$propertyGroup.VersionMinor
    $build = [string]$propertyGroup.VersionBuild

    if ([string]::IsNullOrWhiteSpace($major) `
        -or [string]::IsNullOrWhiteSpace($minor) `
        -or [string]::IsNullOrWhiteSpace($build)) {
        throw "Version.props must define VersionMajor, VersionMinor, and VersionBuild."
    }

    return "$major.$minor.$build"
}

function Get-VersionParts {
    param(
        [string]$Value
    )

    if ($Value -notmatch '^(?<major>\d+)\.(?<minor>\d+)\.(?<build>\d+)$') {
        throw "Version must use the format major.minor.build."
    }

    return @{
        Major = $Matches.major
        Minor = $Matches.minor
        Build = $Matches.build
    }
}

function ConvertTo-RtfEscapedText {
    param(
        [string]$Value
    )

    $sb = New-Object System.Text.StringBuilder
    foreach ($ch in $Value.ToCharArray()) {
        $code = [int][char]$ch
        switch ($ch) {
            '\' { [void]$sb.Append('\\'); break }
            '{' { [void]$sb.Append('\{'); break }
            '}' { [void]$sb.Append('\}'); break }
            default {
                if ($code -gt 127) {
                    if ($code -gt 32767) {
                        $code -= 65536
                    }

                    [void]$sb.Append('\u')
                    [void]$sb.Append($code)
                    [void]$sb.Append('?')
                }
                else {
                    [void]$sb.Append($ch)
                }
            }
        }
    }

    return $sb.ToString()
}

function ConvertTo-LicenseParagraphs {
    param(
        [string[]]$Lines
    )

    $paragraphs = [System.Collections.Generic.List[string]]::new()
    $current = [System.Collections.Generic.List[string]]::new()

    foreach ($line in $Lines) {
        $trimmed = $line.Trim()
        if ([string]::IsNullOrWhiteSpace($trimmed)) {
            if ($current.Count -gt 0) {
                $paragraphs.Add(($current -join ' '))
                $current.Clear()
            }
        }
        else {
            $current.Add($trimmed)
        }
    }

    if ($current.Count -gt 0) {
        $paragraphs.Add(($current -join ' '))
    }

    return $paragraphs.ToArray()
}

function New-LicenseRtf {
    param(
        [string]$SourcePath,
        [string]$OutputPath
    )

    Assert-PathExists -Path $SourcePath -Description "License file"

    $lines = [System.IO.File]::ReadAllLines($SourcePath, [System.Text.Encoding]::UTF8)
    $paragraphs = ConvertTo-LicenseParagraphs -Lines $lines
    if ($paragraphs.Count -eq 0) {
        throw "License file was empty: $SourcePath"
    }

    $sb = New-Object System.Text.StringBuilder
    [void]$sb.AppendLine('{\rtf1\ansi\deff0{\fonttbl{\f0 Segoe UI;}}\fs20')

    for ($i = 0; $i -lt $paragraphs.Count; $i++) {
        $paragraph = $paragraphs[$i]
        $escaped = ConvertTo-RtfEscapedText -Value $paragraph

        if ($i -eq 0) {
            [void]$sb.Append('\b\fs24 ')
            [void]$sb.Append($escaped)
            [void]$sb.AppendLine('\b0\fs20\par')
        }
        else {
            [void]$sb.Append($escaped)
            [void]$sb.AppendLine('\par')
        }
    }

    [void]$sb.AppendLine('}')

    $outputDirectory = Split-Path -Parent $OutputPath
    if (-not [string]::IsNullOrWhiteSpace($outputDirectory)) {
        New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
    }

    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($OutputPath, $sb.ToString(), $utf8NoBom)
}

function New-InstallerStaging {
    <#
    .SYNOPSIS
    Stages build output for both TFM lines, deduplicates identical files into
    a Common directory, and copies documentation files.  Shared by NSIS and
    MSI packaging.
    #>
    param(
        [string]$Configuration,
        [string]$ProjectRoot,
        [string]$StagingRoot,
        [string]$Label = ""
    )

    $standardDir = Join-Path $StagingRoot "Standard"
    $legacyDir   = Join-Path $StagingRoot "Legacy"
    $commonDir   = Join-Path $StagingRoot "Common"

    New-Item -ItemType Directory -Path $StagingRoot -Force | Out-Null
    foreach ($dir in @($standardDir, $legacyDir, $commonDir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }

    $standardBuildDir = Join-Path $script:SourceRoot "bin\$Configuration\net45"
    $legacyBuildDir   = Join-Path $script:SourceRoot "bin\$Configuration\net30"
    Assert-PathExists -Path $standardBuildDir -Description "net45 build output directory"
    Assert-PathExists -Path $legacyBuildDir -Description "net30 build output directory"

    $fileCount = 0
    foreach ($target in @(
        @{ Source = $standardBuildDir; Destination = $standardDir; Label = "net45" },
        @{ Source = $legacyBuildDir;   Destination = $legacyDir;   Label = "net30" }
    )) {
        $packageFiles = @(Get-ChildItem -LiteralPath $target.Source -File |
            Where-Object { $_.Extension -in @(".exe", ".dll", ".config") })
        if ($packageFiles.Count -eq 0) {
            throw "No .exe, .dll, or .config files found in $($target.Label) build output at $($target.Source). Verify that the project built successfully."
        }
        foreach ($file in $packageFiles) {
            Copy-Item -LiteralPath $file.FullName -Destination $target.Destination -Force
            $fileCount++
        }
    }

    # Deduplicate identical files across both TFM directories into Common.
    foreach ($file in Get-ChildItem -LiteralPath $standardDir -File) {
        $legacyPath = Join-Path $legacyDir $file.Name
        if ((Test-Path -LiteralPath $legacyPath) -and
            ((Get-FileHash -LiteralPath $file.FullName).Hash -eq (Get-FileHash -LiteralPath $legacyPath).Hash)) {
            Move-Item -LiteralPath $file.FullName -Destination (Join-Path $commonDir $file.Name) -Force
            Remove-Item -LiteralPath $legacyPath -Force
        }
    }

    # Bundle documentation files alongside the binaries.
    $licenseRtf = Join-Path $commonDir "LICENSE.rtf"
    New-LicenseRtf -SourcePath (Join-Path $script:RepoRoot "LICENSE") -OutputPath $licenseRtf
    $fileCount++
    Copy-Item -LiteralPath (Join-Path $script:RepoRoot "README.md") -Destination $commonDir -Force
    $fileCount++
    Copy-Item -LiteralPath (Join-Path $script:RepoRoot "docs\OPEN-SOURCE-LICENSES.md") -Destination $commonDir -Force
    $fileCount++

    return @{
        StandardDirectory = $standardDir
        LegacyDirectory   = $legacyDir
        CommonDirectory   = $commonDir
        FileCount         = $fileCount
        LicenseRtfPath    = $licenseRtf
    }
}

function Test-FileLocked {
    <#
    .SYNOPSIS
    Returns $true if the file exists and is locked by another process.
    #>
    param(
        [string]$FilePath
    )

    if (-not (Test-Path -LiteralPath $FilePath)) {
        return $false
    }

    try {
        $stream = [System.IO.File]::Open(
            $FilePath,
            [System.IO.FileMode]::Open,
            [System.IO.FileAccess]::Read,
            [System.IO.FileShare]::None
        )
        $stream.Dispose()
        return $false
    }
    catch {
        return $true
    }
}

Export-ModuleMember -Function Write-Step, Assert-PathExists, Assert-CommandExists,
                          Get-VersionString, Get-VersionParts, New-LicenseRtf,
                          New-InstallerStaging, Test-FileLocked
