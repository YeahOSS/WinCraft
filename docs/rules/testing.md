# Testing

## Test Project

Tests under `src/WinCraft.Tests/`, target `net45`, use NUnitLite as a WPF-enabled console EXE.
Internal access via `InternalsVisibleTo` (guarded by `#if DEBUG`).
The solution excludes the test project from Release builds; build and run tests in Debug.

```powershell
dotnet build -f net45 src/WinCraft.Tests/WinCraft.Tests.csproj
src/bin/Debug/net45/WinCraft.Tests.exe
```

## Test Categories

| Category | Key technique |
|---|---|
| Pure logic | No privileges, no STA, no Window. |
| Windows integration | Requires Windows but not admin (pipes, HKCU, COM). |
| STA / WPF | `[Apartment(ApartmentState.STA)]`; create hidden `Window` with `WindowInteropHelper.EnsureHandle()` when HWND is needed. |
| Administrator-gated | `[Explicit]` + `Assert.Ignore` guard in `[OneTimeSetUp]`. |

## What NOT to test

- Trivial code that existing tests already cover.  Before adding a test, ask:
  can this code produce a wrong result that existing tests wouldn't catch?
- Visible desktop, mouse simulation, blocking WPF drag-drop loops — test the
  underlying COM layer instead.
- Specific service states (TrustedInstaller) or network I/O.
- **Don't complicate production code to enable a test.**  Interfaces,
  overloads, or settable static flags added solely for testing are forbidden.

## When to test

Add or update a test when the code contains non-trivial logic: parsing,
mapping, state machines, validation, algorithms, or structured data transformation.

## TFM coverage

Run tests on `net45`.  Build with `-f net30` only when adding TFM-specific code.
