# WinCraft Agent Guide

## Working Rules
- Use commit messages in the format `<type>: <short English summary>`.
- Keep code comments, script output, and developer-facing notes in English.
- Do not remove existing comments unless the related code change makes them incorrect.
- Prefer UTF-8 when reading or writing text files. Do not change BOM, line endings, or file encoding unless the task explicitly requires it.
- Prefer small, verifiable PowerShell and Git commands instead of long chained commands.
- Run Git write operations serially. Do not overlap `git add`, `git commit`, `git merge`, `git rebase`, or branch-changing commands.

## Project Structure
- The main project is `src/WinCraft/WinCraft.csproj` and uses the SDK-style project format.
- `src/WinCraft.slnx` intentionally includes only that single main project for day-to-day development.
- The main project multi-targets both framework lines:
  - `net30` for the Legacy line
  - `net45` for the Standard line
- Shared version numbers live in `src/Version.props` as `major.minor.build`.
- The IL merge configuration lives in `src/WinCraft/ILRepack.targets`.
- Source layout and placement rules live in `docs/source-layout.md`.
- Publish workflow details live under `publish/README.md`.
- `publish/build.ps1` is the repeatable artifact build entry point.
- `publish/release.ps1` handles version bump, build, local commit, and local tag creation.

## Framework Compatibility
- `Theraot.Core` is restored through `PackageReference`. Do not reintroduce `packages.config` or hardcoded assembly hint paths.
- `Theraot.Core` is intentionally referenced so the codebase can use many newer APIs even when the build target is `.NET Framework 3.0`.
- Writing async code is allowed. Even on the `net30` variant, `Theraot` support means `Task`, `async`, and `await` style code can still be used when the referenced APIs are available through the package.
- When an API might differ across target frameworks, use the compatibility layer under `WinCraft.Compatibility` instead of scattering `#if` blocks across business code.
- The first shared entry point is `WinCraft.Compatibility.StringCompat.IsNullOrWhiteSpace(string value)`.

## Implementation Preferences
- Prefer one clear compatibility entry point over multiple equivalent wrappers.
- Keep framework-specific behavior centralized in compatibility helpers or build configuration.
- If you add more compatibility helpers, keep them in `WinCraft.Compatibility` and document the intent with short English comments when the code is not obvious.

## Event Subscription
- Use a lambda when the handler is 5 lines or fewer, subscribed in one place, and not part of the class contract.
- Use a named method when the handler is longer, reused, required by inheritance or interfaces, or represents an extensibility point.
- Avoid extracting a trivial lambda into a named method, and avoid leaving a long inline lambda in place.
