# WinCraft Agent Guide

## Required Reading

All documents below live under `docs/rules/`.

| Area | Document | Common mistake |
|------|----------|---------------|
| Win32 interop | `win32-interop.md` | Hand-writing P/Invoke without checking CsWin32 |
| Source layout | `source-layout.md` | Placing capability code in `Compatibility/` |
| Framework compatibility | `framework-compatibility.md` | Scattering `#if` instead of using `Compatibility/` helpers |
| Coding conventions | `coding-style.md` | Hardcoding strings instead of `nameof()` |
| Commit conventions | `commit-conventions.md` | Omitting `<type>:` prefix in commit messages |
| Documentation | `documentation.md` | Restating what the code already says |
| Testing | `testing.md` | Testing trivial code, or skipping tests for new non-trivial logic |
| Design | `design-principles.md` | Adding abstraction or architecture without asking |

## Workflow

- Read the relevant rule doc before touching a domain.
- Scan `docs/features/` to know what exists; read on demand when the task
  touches that area.
- Build with `dotnet build -f net45`; net30 validation runs automatically.
