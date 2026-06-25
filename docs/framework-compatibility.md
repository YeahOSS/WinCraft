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

Do not treat a standalone `dotnet build` failure as authoritative for `net30`,
especially when it reports missing `.NET Framework 3.0` reference assemblies.
For this repository, Visual Studio MSBuild and `publish/build.ps1` are the
authoritative validation path for `net30`, and they may succeed even when the
standalone `dotnet` SDK build does not.

In code review findings, do not report a standalone `dotnet build` `net30`
failure as a bug, regression, or testing gap by itself. First verify with
Visual Studio MSBuild or `publish/build.ps1`, and only report a build problem
when that authoritative path fails or when the change actually breaks the
repository's supported build workflow.

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
