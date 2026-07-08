# Framework Compatibility

## Target Frameworks

Multi-targets `net30` (Legacy) and `net45` (Standard).  Shared version
numbers live in `publish/version.props`.

## Theraot

`Theraot.Core` backfills APIs absent in `net30` (LINQ, tuples, delegates,
collections, caller info, expression trees) and supplements post-4.5 APIs
on `net45` (`HashCode`, `Index`, `Range`, `IsExternalInit`, nullable
attributes).  Both targets need it — the package reference is unconditional.

## Build Validation

`dotnet build` cannot resolve net30 reference assemblies.  Use these instead:

| Path | Command | Use |
|------|---------|-----|
| Quick validation | `src/WinCraft/validate.ps1` | Day-to-day CI |
| Quick + tests | `src/WinCraft/validate.ps1 -Test` | Pre-commit |
| Full publish | `publish/build.ps1 -BuildOnly` | Release readiness |

The post-build event in `WinCraft.csproj` validates net30 automatically
outside Visual Studio (`Net30ValidationBuild=true` guard prevents re-entrant
loops).

Do not report a standalone `dotnet build` net30 failure as a bug — only
`validate.ps1` or `publish/build.ps1` failures are actionable.

## Compatibility Helpers

Prefer `WinCraft.Compatibility` helpers over `#if` blocks.  Available:

| Helper | Backfills | Methods |
|--------|-----------|---------|
| `StringCompat` | `string` | `IsNullOrWhiteSpace(string)` |
| `EnumCompat` | `Enum` | `TryParse<TEnum>(string, bool, out TEnum)` |
| `GuidCompat` | `Guid` | `TryParse(string, out Guid)` |
| `PathCompat` | `Path` | `Combine` with 3+ parameters |
| `RegistryKeyCompat` | `RegistryKey` | `DeleteSubKeyTree(key, name)`, `DeleteSubKey(key, name)` |
| `ThrowCompat` | — | `IfNull<T>(T, string)` |
| `FrameworkCompat` | — | `IsNet30` (bool const) |

Add to this table when adding a new helper.

## Language Version

Fixed in the project file.  Do not switch to `latest` or `preview` unless
explicitly requested.
