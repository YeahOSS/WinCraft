# WinCraft Agent Guide

## Required Reading

| Area | Document | Common mistake |
|------|----------|---------------|
| Win32 interop | `docs/win32-interop.md` | Hand-writing P/Invoke without checking CsWin32 |
| Source layout | `docs/source-layout.md` | Placing capability code in `Compatibility/` |
| Framework compatibility | `docs/framework-compatibility.md` | Scattering `#if` instead of using `Compatibility/` helpers |
| Coding conventions | `docs/coding-style.md` | Hardcoding strings instead of `nameof()` |
| Commit conventions | `docs/commit-conventions.md` | Omitting `<type>:` prefix in commit messages |
| Documentation | `docs/documentation.md` | Restating what the code already says |
| Testing | `docs/testing.md` | Adding a new module without creating its test file |

## Workflow

- Do not touch a domain until you have read its doc.  Scan `docs/` and match
  by filename to the area you are working on.
- Build with `dotnet build -f net45`; net30 validation runs automatically.
