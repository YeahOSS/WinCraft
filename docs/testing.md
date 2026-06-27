# Testing

## Test Project

Tests live under `src/WinCraft.Tests/`, target `net45`, and use NUnit with the
NUnitLite self-executing runner.  The project is a WPF-enabled console EXE
(UseWPF, Microsoft.NET.Sdk.WindowsDesktop) so that STA-dependent and pure-logic
tests coexist in a single harness.

When a test needs access to an internal member, make that member `internal`,
never `public`, and rely on the project-level `InternalsVisibleTo` already wired
to `WinCraft.Tests` (in `WinCraft\Properties\AssemblyInfo.Shared.cs`, guarded by
`#if DEBUG`).

Run with:

```powershell
dotnet build -f net45 src/WinCraft.Tests/WinCraft.Tests.csproj
src/WinCraft.Tests/bin/Debug/net45/WinCraft.Tests.exe
```

Results are written to `TestResult.xml` next to the executable.  Supply
`--result` to override the path.

## Test Categories

| Category | Key technique |
|---|---|
| Pure logic | No privileges, no STA, no Window. Fastest and most reliable. |
| Windows integration | Needs Windows but not admin or a visible window (pipes, HKCU, COM). |
| STA / WPF | `[Apartment(ApartmentState.STA)]`; create a hidden `Window` with `WindowInteropHelper.EnsureHandle()` when an HWND is needed. |
| Administrator-gated | `[Explicit]` + `Assert.Ignore` guard in `[OneTimeSetUp]`. Run from an elevated terminal. |

## What NOT to test in the test project

- Operations requiring a visible desktop, mouse simulation, or a blocking WPF
  drag-drop loop.  Test the underlying COM layer directly instead
  (`IDragSourceHelper`, `IDropTargetHelper`).
- Environments that depend on specific service states (TrustedInstaller).
- Network I/O (flaky; prefer hand-testing or a separate harness).

## TFM coverage

The tested code has no TFM-conditional branches, so a single `net45` test run is
sufficient.  Add a `dotnet build -f net30` only when adding TFM-specific code.
