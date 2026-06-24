# LZMA SDK Vendor Notes

This directory contains the vendored C# source subset from the official
7-Zip LZMA SDK.

Source:
- https://www.7-zip.org/sdk.html
- https://www.7-zip.org/a/lzma2601.7z

Upstream package:
- LZMA SDK 26.01
- Published 2026-04-27

Layout:
- The directory structure keeps only the files this repository uses.
- The upstream `CS/7zip/` wrapper path is intentionally flattened because this repository keeps only the C# subset.
- Files kept for runtime decoding:
  - `ICoder.cs`
  - `Compress/LZ/LzOutWindow.cs`
  - `Compress/LZMA/LzmaBase.cs`
  - `Compress/LZMA/LzmaDecoder.cs`
  - `Compress/RangeCoder/*.cs`
- Additional files kept for publish-time encoding:
  - `Common/CRC.cs`
  - `Compress/LZ/IMatchFinder.cs`
  - `Compress/LZ/LzBinTree.cs`
  - `Compress/LZ/LzInWindow.cs`
  - `Compress/LZMA/LzmaEncoder.cs`
- Only the files required by WinCraft are copied here.

Usage in this repository:
- `src/WinCraft/WinCraft.csproj` links the decoder-related files it needs.
- `publish/build.ps1` loads the encoder-related files from the same vendored tree.
- `.editorconfig` in this directory disables analyzer diagnostics for the vendored code.

Update workflow:
- Download the current official SDK archive from `https://www.7-zip.org/a/lzma2601.7z` or a newer official version from `https://www.7-zip.org/sdk.html`.
- Replace only the files listed above from the official C# source tree.
- Keep the local flattened layout; do not restore the upstream `CS/7zip/` wrapper directories.
- Re-run `dotnet build src\WinCraft.Tests\WinCraft.Tests.csproj` and `powershell -ExecutionPolicy Bypass -File .\publish\build.ps1`.

License:
- The official 7-Zip SDK page states that LZMA SDK is placed in the public domain.
