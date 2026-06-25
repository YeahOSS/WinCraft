Set-StrictMode -Version Latest

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

Export-ModuleMember -Function Write-Step, Assert-PathExists, Assert-CommandExists,
                          Get-VersionString, Get-VersionParts
