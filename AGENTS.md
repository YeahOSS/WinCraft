# WinCraft Agent Guide

## Document Map
- `AGENTS.md`: collaboration, implementation, compatibility, naming, and documentation rules.
- `docs/source-layout.md`: code placement and directory boundaries.
- `docs/elevated-agent-ipc.md`: dual-process architecture and IPC design rationale.
- `publish/README.md`: build and release workflow details.

## Working Rules
- Use commit messages in the format `<type>: <short English summary>`.
- Do not add `Co-Authored-By` trailers on behalf of the AI — the rule does not restrict human contributors.
- Keep code comments, script output, and developer-facing notes in English.
- Do not remove existing comments unless the related code change makes them incorrect.
- Prefer UTF-8 when reading or writing text files. Do not change BOM, line endings, or file encoding unless the task explicitly requires it.
- Prefer small, verifiable PowerShell and Git commands instead of long chained commands.
- Run Git write operations serially. Do not overlap `git add`, `git commit`, `git merge`, `git rebase`, or branch-changing commands.
- Follow `docs/source-layout.md` for code placement and directory boundaries.

## Project Structure
- The main project is `src/WinCraft/WinCraft.csproj` and uses the SDK-style project format.
- The main project multi-targets both framework lines:
  - `net30` for the Legacy line
  - `net45` for the Standard line
- Shared version numbers live in `src/Version.props` as `major.minor.build`.
- `publish/build.ps1` is the repeatable artifact build entry point.
- `publish/release.ps1` handles version bump, build, local commit, and local tag creation.

## Framework Compatibility
- `Theraot.Core` backfills APIs absent in `net30`. Key areas: delegates (`Func<T1..T16>`, `Action<T1..T16>`), LINQ (`Enumerable`, `IQueryable<T>`, expression trees), async (`Task`, `ValueTask`, `await` infrastructure), collections (`HashSet<T>`, `ISet<T>`, `IReadOnlyList<T>`, `ConcurrentDictionary`), tuples (`Tuple`, `ValueTuple`), lazy/observable (`Lazy<T>`, `IObservable<T>`), caller info (`CallerFilePathAttribute`, etc.), and `System.Dynamic`.
- On `net45`, Theraot supplements post-4.5 APIs required by newer C# versions: `IsExternalInit` (`init`), `IsReadOnlyAttribute`, `Index`, `Range`, `HashCode`, `IAsyncDisposable`, `IReadOnlySet<T>`, nullable annotation attributes, and others. Both targets need it — the package reference is unconditional for this reason.
- Do not treat a standalone `dotnet build` failure as authoritative for `net30`, especially when it reports missing `.NET Framework 3.0` reference assemblies. For this repository, Visual Studio MSBuild and `publish/build.ps1` are the authoritative validation path for `net30`, and they may succeed even when the standalone `dotnet` SDK build does not.
- In code review findings, do not report a standalone `dotnet build` `net30` failure as a bug, regression, or testing gap by itself. First verify with Visual Studio MSBuild or `publish/build.ps1`, and only report a build problem when that authoritative path fails or when the change actually breaks the repository's supported build workflow.
- Prefer `CsWin32` for Win32 API declarations, constants, and enums over hand-written P/Invoke or manual numeric values. Add entries to `src/WinCraft/NativeMethods.txt` first — use the undecorated API name (e.g. `CreateProcessWithTokenW`). Then build and inspect the generated source under `obj/<config>/<tfm>/generated/Microsoft.Windows.CsWin32/` for the exact C# signature and namespace before writing the call site. Do not guess these from memory.
- When a CsWin32 API offers a friendly wrapper overload (`SafeFileHandle`, `out`, nullable value types), prefer it over the raw pointer overload. When only the raw overload exists, note why with a short comment.
- When calling Win32 APIs, use the CsWin32-generated P/Invoke wrappers exclusively. Do not mix BCL wrappers such as `Process.GetCurrentProcess()` or `Process.MainModule` with raw Win32 calls — the BCL wrappers often allocate extra objects or surface different error semantics.
- When introducing or changing Win32 API usage, explicitly consider Windows version compatibility. Do not assume that targeting `.NET Framework 3.0` implies support for pre-Vista Windows behavior. Document or guard APIs whose semantics depend on Vista-era features such as UAC, split tokens, or elevation metadata.
- When an API might differ across target frameworks, search `WinCraft.Compatibility` for an existing helper before adding one, and prefer it over scattering `#if` blocks across business code.

## Implementation Preferences
- Prefer one clear compatibility entry point over multiple equivalent wrappers.
- If you add more compatibility helpers, keep them in `WinCraft.Compatibility` and document the intent with short English comments when the code is not obvious.
- The repository language version is fixed in the project file. Do not switch it to `latest` or `preview` unless the user explicitly requests that change.
- Newer C# syntax may be used when supported by the repository `LangVersion`, but only when it clearly improves readability, removes duplication, or reduces brittleness in this mixed-framework codebase.
- Do not introduce newer syntax only because it exists. Prefer explicit, stable code over novelty, especially in compatibility-sensitive or infrastructure-heavy paths.
- Prefer `nameof(...)` over hardcoded symbol-name strings, especially when throwing argument/object-state exceptions or building diagnostic messages that reference symbols.

## Naming Rules
- Avoid namespace or type names that collide with common .NET, WPF, or Win32 framework types.
- Prefer role-based or capability-based names such as `RegistryAccess`, `PrivilegeBroker`, `ShellCommandBuilder`, `Host`, or `Client` over raw platform nouns.
- Do not introduce project namespaces named exactly like framework surface areas such as `Registry`, `Task`, `Process`, `Application`, `Path`, `File`, `Directory`, or `Window`.

## Documentation Rules
- Keep layout and directory-boundary guidance in `docs/source-layout.md`, not in `AGENTS.md`.
- Keep `docs/source-layout.md` focused on durable structure. Do not update it for routine class additions, one-off helper moves, or implementation-level refactors that still fit the existing rules.

## Event Subscription
- Use a lambda when the handler is 5 lines or fewer, subscribed in one place, and not part of the class contract.
- Use a named method when the handler is longer, reused, required by inheritance or interfaces, or represents an extensibility point.
- Avoid extracting a trivial lambda into a named method, and avoid leaving a long inline lambda in place.

## Test Project
- Tests live under `src/WinCraft.Tests/`, target `net45`, and use NUnit with the NUnitLite self-executing runner.
- Run with `dotnet build src/WinCraft.Tests/WinCraft.Tests.csproj && src/WinCraft.Tests/bin/Debug/net45/WinCraft.Tests.exe`.
- Test only pure logic that does not require Windows privileges (parsing, serialization, operation routing, result mapping).
- When a test needs access to an internal member, make that member `internal` — never `public` — and rely on the project-level `InternalsVisibleTo` already wired to `WinCraft.Tests`.
- The tested code has no TFM-conditional branches, so a single `net45` test run is sufficient. Add a `dotnet build` for `net30` only when adding TFM-specific code.
- Keep tests in sync with the production code they cover. When adding an operation, changing a factory method, or adjusting error routing, add or update the corresponding test in the same commit.
