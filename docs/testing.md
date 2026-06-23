# Testing

## Test Project

Tests live under `src/WinCraft.Tests/`, target `net45`, and use NUnit with the
NUnitLite self-executing runner.

Run with:

```powershell
dotnet build src/WinCraft.Tests/WinCraft.Tests.csproj
src/WinCraft.Tests/bin/Debug/net45/WinCraft.Tests.exe
```

The test runner writes results next to the test executable by default, for
example `src/WinCraft.Tests/bin/Debug/net45/TestResult.xml`. Supply `--result`
to override the output path.

## Scope

Test only pure logic that does not require Windows privileges, such as parsing,
serialization, operation routing, and result mapping.

When a test needs access to an internal member, make that member `internal`,
never `public`, and rely on the project-level `InternalsVisibleTo` already wired
to `WinCraft.Tests`.

The tested code has no TFM-conditional branches, so a single `net45` test run is
sufficient. Add a `dotnet build` for `net30` only when adding TFM-specific code.

Keep tests in sync with the production code they cover. When adding an
operation, changing a factory method, or adjusting error routing, add or update
the corresponding test in the same commit.
