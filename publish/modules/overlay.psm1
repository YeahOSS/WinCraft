Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Import-Module (Join-Path $PSScriptRoot "common.psm1")

$script:RepoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$script:SourceRoot = Join-Path $script:RepoRoot "src"
$script:OutputPath = Join-Path (Split-Path -Parent $PSScriptRoot) "output"

function Initialize-LzmaSdk {
    if ($null -ne ("SevenZip.Compression.LZMA.Encoder" -as [type])) {
        return
    }

    $sdkPath = Join-Path $script:SourceRoot "third_party\LzmaSdk"
    Assert-PathExists -Path $sdkPath -Description "LZMA SDK source directory"

    $sourceFiles = @(
        (Join-Path $sdkPath "ICoder.cs"),
        (Join-Path $sdkPath "Common\CRC.cs"),
        (Join-Path $sdkPath "Compress\LZ\IMatchFinder.cs"),
        (Join-Path $sdkPath "Compress\LZ\LzBinTree.cs"),
        (Join-Path $sdkPath "Compress\LZ\LzInWindow.cs"),
        (Join-Path $sdkPath "Compress\LZ\LzOutWindow.cs"),
        (Join-Path $sdkPath "Compress\LZMA\LzmaBase.cs"),
        (Join-Path $sdkPath "Compress\LZMA\LzmaEncoder.cs"),
        (Join-Path $sdkPath "Compress\RangeCoder\RangeCoder.cs"),
        (Join-Path $sdkPath "Compress\RangeCoder\RangeCoderBit.cs"),
        (Join-Path $sdkPath "Compress\RangeCoder\RangeCoderBitTree.cs")
    )
    foreach ($sourceFile in $sourceFiles) {
        Assert-PathExists -Path $sourceFile -Description "LZMA SDK source file"
    }

    Add-Type -Path $sourceFiles
}

function Compress-Lzma {
    param(
        [byte[]]$RawBytes
    )

    Initialize-LzmaSdk

    $encoder = New-Object SevenZip.Compression.LZMA.Encoder
    $propIDs = [SevenZip.CoderPropID[]]@(
        [SevenZip.CoderPropID]::DictionarySize,
        [SevenZip.CoderPropID]::PosStateBits,
        [SevenZip.CoderPropID]::LitContextBits,
        [SevenZip.CoderPropID]::LitPosBits,
        [SevenZip.CoderPropID]::Algorithm,
        [SevenZip.CoderPropID]::NumFastBytes,
        [SevenZip.CoderPropID]::MatchFinder,
        [SevenZip.CoderPropID]::EndMarker
    )
    $properties = [object[]]@(
        [int](1 -shl 23),
        [int]2,
        [int]3,
        [int]0,
        [int]2,
        [int]128,
        "bt4",
        $false
    )

    $encoder.SetCoderProperties($propIDs, $properties)

    $inputStream = New-Object System.IO.MemoryStream -ArgumentList (,$RawBytes)
    $compressedStream = New-Object System.IO.MemoryStream
    $propertiesStream = New-Object System.IO.MemoryStream

    try {
        $encoder.WriteCoderProperties($propertiesStream)
        $encoder.Code($inputStream, $compressedStream, [int64]-1, [int64]-1, $null)

        return [pscustomobject]@{
            CompressedBytes = $compressedStream.ToArray()
            Properties = $propertiesStream.ToArray()
        }
    }
    finally {
        $propertiesStream.Dispose()
        $compressedStream.Dispose()
        $inputStream.Dispose()
    }
}

function New-LzmaPayload {
    param(
        [byte[]]$RawBytes
    )

    $lzmaResult = Compress-Lzma -RawBytes $RawBytes
    $compressedBytes = $lzmaResult.CompressedBytes
    $properties = $lzmaResult.Properties
    $rawLenBytes = [System.BitConverter]::GetBytes([int64]$RawBytes.Length)

    $payloadStream = New-Object System.IO.MemoryStream
    try {
        $payloadStream.Write($properties, 0, $properties.Length)
        $payloadStream.Write($rawLenBytes, 0, $rawLenBytes.Length)
        $payloadStream.Write($compressedBytes, 0, $compressedBytes.Length)
        return $payloadStream.ToArray()
    }
    finally {
        $payloadStream.Dispose()
    }
}

function Compress-Deflate {
    param(
        [byte[]]$RawBytes
    )

    $compressedStream = New-Object System.IO.MemoryStream
    $deflateStream = New-Object System.IO.Compression.DeflateStream -ArgumentList @(
        $compressedStream,
        [System.IO.Compression.CompressionMode]::Compress,
        $true
    )

    try {
        $deflateStream.Write($RawBytes, 0, $RawBytes.Length)
    }
    finally {
        $deflateStream.Dispose()
    }

    try {
        return $compressedStream.ToArray()
    }
    finally {
        $compressedStream.Dispose()
    }
}

function New-DependencyContainer {
    param(
        [string[]]$FilePaths,
        [hashtable]$Entries = @{}
    )

    $entryNames = @($Entries.Keys | Sort-Object)
    $containerStream = New-Object System.IO.MemoryStream
    $writer = New-Object System.IO.BinaryWriter($containerStream, [System.Text.Encoding]::UTF8)
    $writer.Write([int32]($FilePaths.Count + $entryNames.Count))

    foreach ($dllPath in $FilePaths) {
        $name = Split-Path $dllPath -Leaf
        $nameBytes = [System.Text.Encoding]::UTF8.GetBytes($name)
        $data = [System.IO.File]::ReadAllBytes($dllPath)
        $writer.Write([int16]$nameBytes.Length)
        $writer.Write($nameBytes)
        $writer.Write([int32]$data.Length)
        $writer.Write($data)
    }

    foreach ($entryName in $entryNames) {
        $nameBytes = [System.Text.Encoding]::UTF8.GetBytes($entryName)
        $data = [byte[]]$Entries[$entryName]
        $writer.Write([int16]$nameBytes.Length)
        $writer.Write($nameBytes)
        $writer.Write([int32]$data.Length)
        $writer.Write($data)
    }

    $writer.Flush()
    $rawBytes = $containerStream.ToArray()
    $writer.Dispose()
    $containerStream.Dispose()
    return $rawBytes
}

# Builds a single-file executable by appending a compressed container of
# dependency DLLs as a PE overlay.  At runtime OverlayAssemblyResolver reads
# the overlay, decompresses the container, and serves assemblies from memory.
function New-OverlayExe {
    param(
        [string]$BuildLabel,
        [string]$Configuration,
        [string]$ProjectRoot,
        [string]$TargetSubdirectory,
        [string]$ArtifactName
    )

    Write-Step "Building $BuildLabel single-file executable with dependency overlay"

    $buildOutputDirectory = Join-Path $ProjectRoot "bin\$Configuration\$TargetSubdirectory"
    $exePath = Join-Path $buildOutputDirectory "WinCraft.exe"
    $artifactPath = Join-Path $script:OutputPath $ArtifactName

    Assert-PathExists -Path $exePath -Description "$BuildLabel executable"

    $dllPaths = @(Get-ChildItem -Path $buildOutputDirectory -Filter "*.dll" -File |
        Sort-Object Name |
        Select-Object -ExpandProperty FullName)
    $lzmaDllPaths = @($dllPaths | Where-Object { [System.IO.Path]::GetFileName($_) -ieq "WinCraft.Lzma.dll" })
    if ($lzmaDllPaths.Count -ne 1) {
        throw "Expected exactly one WinCraft.Lzma.dll in $buildOutputDirectory; found $($lzmaDllPaths.Count)."
    }

    $innerDllPaths = @($dllPaths | Where-Object { [System.IO.Path]::GetFileName($_) -ine "WinCraft.Lzma.dll" })
    $innerRawBytes = New-DependencyContainer -FilePaths $innerDllPaths
    $innerLzmaPayload = New-LzmaPayload -RawBytes $innerRawBytes

    $rawBytes = New-DependencyContainer -FilePaths $lzmaDllPaths -Entries @{
        "__dependencies.wclz" = $innerLzmaPayload
    }
    $compressedBytes = Compress-Deflate -RawBytes $rawBytes
    $dllCount = $dllPaths.Count
    $innerRatio = [math]::Round($innerLzmaPayload.Length / [math]::Max(1, $innerRawBytes.Length) * 100, 1)
    $outerRatio = [math]::Round($compressedBytes.Length / [math]::Max(1, $rawBytes.Length) * 100, 1)
    Write-Host "==> Overlay: $dllCount DLL(s), inner LZMA $($innerRawBytes.Length) -> $($innerLzmaPayload.Length) bytes (${innerRatio}%), outer Deflate $($rawBytes.Length) -> $($compressedBytes.Length) bytes (${outerRatio}%)"

    # Append overlay: [Deflate data] [8 bytes raw len] [4 bytes compressed len] [4 bytes magic "WOHY"]
    $magic = [System.BitConverter]::GetBytes([uint32]0x59484F57)
    $rawLenBytes = [System.BitConverter]::GetBytes([int64]$rawBytes.Length)
    $lenBytes = [System.BitConverter]::GetBytes([int32]$compressedBytes.Length)

    if (Test-Path -LiteralPath $artifactPath) {
        Remove-Item -LiteralPath $artifactPath -Force
    }

    Copy-Item -LiteralPath $exePath -Destination $artifactPath -Force

    $exeBytes = [System.IO.File]::ReadAllBytes($artifactPath)
    $footerSize = 16
    $overlayOffset = $exeBytes.Length
    [Array]::Resize([ref]$exeBytes, $overlayOffset + $compressedBytes.Length + $footerSize)
    [Array]::Copy($compressedBytes, 0, $exeBytes, $overlayOffset, $compressedBytes.Length)
    [Array]::Copy($rawLenBytes, 0, $exeBytes, $overlayOffset + $compressedBytes.Length, 8)
    [Array]::Copy($lenBytes, 0, $exeBytes, $overlayOffset + $compressedBytes.Length + 8, 4)
    [Array]::Copy($magic, 0, $exeBytes, $overlayOffset + $compressedBytes.Length + 12, 4)
    [System.IO.File]::WriteAllBytes($artifactPath, $exeBytes)

    Assert-PathExists -Path $artifactPath -Description "$BuildLabel single-file artifact"
}

Export-ModuleMember -Function New-OverlayExe
