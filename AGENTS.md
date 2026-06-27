# WinCraft Agent Guide

## Required References

**Do not act in any of these areas until you have Read the linked document.**
The linked docs are authoritative for their domain; the guardrails below only
call out the most-repeated violations to prevent the agent from guessing.

- **Before any Win32/native API call (including constants, structs, enum values, or COM interfaces):**
  Read `docs/win32-interop.md`.
  Common violation: hand-writing P/Invoke without first checking whether CsWin32
  can emit the API. The CsWin32 workflow is a fixed sequence — do not skip steps.
- **Before creating a file or choosing which directory a class belongs in:**
  Read `docs/source-layout.md`.
  Common violation: placing a capability class in `Compatibility/` when it does
  not bridge a net30↔net45 gap. Directory assignment follows capability type,
  not convenience — the doc defines which type goes where.
- **Before building or reviewing `net30`-specific failures:**
  Read `docs/framework-compatibility.md`.
  Common violation: reporting a standalone `dotnet build` `net30` failure as a bug.
- **Before adding TFM conditionals, compatibility wrappers, or using post-net45 C# syntax:**
  Read `docs/framework-compatibility.md`.
  Common violation: scattering `#if` blocks instead of using a `WinCraft.Compatibility` helper.
- **Before writing or modifying tests:**
  Read `docs/testing.md`.
- **Before touching startup routing, WPF startup, or single-instance behavior:**
  Read `docs/startup-lifecycle.md`.
- **Before touching privileged process launch or IPC between processes:**
  Read `docs/elevated-agent-ipc.md`.
- **Before touching Shell drag-and-drop, COM drag helpers, or ShellDataObject:**
  Read `docs/shell-drag-drop.md`.
- **Before changing the publish or release workflow:**
  Read `publish/README.md`.

## Working Rules
- Use commit messages in the format `<type>: <short English summary>`.  Prefer including a brief body describing what changed and why; title-only commits are acceptable for trivial or self-explanatory changes.
- Do not add `Co-Authored-By` trailers on behalf of the AI; the rule does not restrict human contributors.
- Prefer UTF-8 when reading or writing text files. Do not change BOM, line endings, or file encoding unless the task explicitly requires it.
- Prefer small, verifiable PowerShell and Git commands instead of long chained commands.
- Run Git write operations serially. Do not overlap `git add`, `git commit`, `git merge`, `git rebase`, or branch-changing commands.

## Implementation Preferences
- Prefer `nameof(...)` over hardcoded symbol-name strings, especially when throwing argument/object-state exceptions or building diagnostic messages that reference symbols.

## Naming Rules
- Avoid namespace or type names that collide with common .NET, WPF, or Win32 framework types.
- Prefer role-based or capability-based names such as `RegistryAccess`, `PrivilegeBroker`, `ShellCommandBuilder`, `Host`, or `Client` over raw platform nouns.
- Do not introduce project namespaces named exactly like framework surface areas such as `Registry`, `Task`, `Process`, `Application`, `Path`, `File`, `Directory`, or `Window`.

## Documentation Rules
- Keep layout and directory-boundary guidance in `docs/source-layout.md`, not in `AGENTS.md`.
- Keep `docs/source-layout.md` focused on durable structure. Do not update it for routine class additions, one-off helper moves, or implementation-level refactors that still fit the existing rules.
- All comments and developer-facing text must be written in English.
- Default to no comment.  Add one only when the code is surprising, works around an external constraint, or reflects a non-obvious design choice.  Never restate what the line of code already says.
- Public and internal types/methods get a one-line `<summary>` describing purpose.  Omit `<param>`, `<returns>`, and `<remarks>` unless the behaviour is genuinely unexpected.
- Remove noise comments during any edit that touches the same file.  Existing comments that are still correct and non-obvious should stay.

## Code Review Rules
- Treat `src/third_party/LzmaSdk/` as vendored third-party LZMA SDK code. Review guidance and source details live in `src/third_party/LzmaSdk/README.md`. Do not review these files for style, naming, modernization, analyzer cleanup, or refactoring.

## Event Subscription
- Use a lambda when the handler is 5 lines or fewer, subscribed in one place, and not part of the class contract.
- Use a named method when the handler is longer, reused, required by inheritance or interfaces, or represents an extensibility point.
- Avoid extracting a trivial lambda into a named method, and avoid leaving a long inline lambda in place.
