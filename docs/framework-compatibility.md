# Framework Compatibility

## Target Frameworks

The main project multi-targets both framework lines:

- `net30` for the Legacy line
- `net45` for the Standard line

Shared version numbers live in `publish/version.props` as `major.minor.build`.
Keep compatibility-sensitive behavior explicit and easy to verify.

## Theraot

`Theraot.Core` backfills APIs absent in `net30`. Key areas include delegates,
LINQ, expression trees, async infrastructure, collections, tuples, lazy and
observable types, caller info attributes, and `System.Dynamic`.

On `net45`, Theraot also supplements post-4.5 APIs required by newer C#
versions, including `IsExternalInit`, `IsReadOnlyAttribute`, `Index`, `Range`,
`HashCode`, `IAsyncDisposable`, `IReadOnlySet<T>`, nullable annotation
attributes, and related surface area. Both targets need it, so the package
reference is unconditional.

## Build Validation

`dotnet build` and `dotnet msbuild` use the .NET SDK's bundled MSBuild,
which cannot resolve net30 reference assemblies — even when Visual Studio
has the targeting packs installed.  The .NET SDK simply does not ship the
framework resolution logic for frameworks that old.

Use these validation paths instead:

| Path | Command | Use |
|------|---------|-----|
| Quick validation | `src/WinCraft.Core/validate.ps1` | Day-to-day CI check; locates VS MSBuild via `vswhere.exe` |
| Quick + tests | `src/WinCraft.Core/validate.ps1 -Test` | Pre-commit validation |
| Full publish build | `publish/build.ps1 -BuildOnly` | Release readiness |

When building outside Visual Studio (such as `dotnet build -f net45`), a
post-build event in `WinCraft.Core.csproj` validates net30 automatically.
The event carries a `Net30ValidationBuild=true` guard to prevent re-entrant
loops and is skipped inside Visual Studio since the IDE compiles both TFMs.

Do not report a standalone `dotnet build` net30 failure as a bug, regression,
or testing gap.  Only report a build problem when `src/WinCraft.Core/validate.ps1` or
`publish/build.ps1` fails.

## Compatibility Helpers

When an API might differ across target frameworks, search
`WinCraft.Compatibility` for an existing helper before adding one, and prefer
it over scattering `#if` blocks across business code.

Prefer one clear compatibility entry point over multiple equivalent wrappers.
If you add more compatibility helpers, keep them in `WinCraft.Compatibility`
and document the intent with short English comments when the code is not
obvious.

## Language Version

The repository language version is fixed in the project file. Do not switch it
to `latest` or `preview` unless the user explicitly requests that change.

Newer C# syntax may be used when supported by the repository `LangVersion`, but
only when it clearly improves readability, removes duplication, or reduces
brittleness in this mixed-framework codebase.

Do not introduce newer syntax only because it exists. Prefer explicit, stable
code over novelty, especially in compatibility-sensitive or infrastructure-heavy
paths.
