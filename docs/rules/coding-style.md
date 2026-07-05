# Coding Style

## File Encoding

- Prefer UTF-8 when reading or writing text files.  Do not change BOM,
  line endings, or file encoding unless the task explicitly requires it.
- `.nsi` scripts must be UTF-8 with BOM and CRLF line endings (NSIS Unicode
  requirement).
- `.ps1` scripts must set console output encoding to UTF-8.

## Naming

- Prefer capability names (`RegistryAccess`, `PrivilegeBroker`) over platform
  nouns (`Registry`, `Process`).
- Avoid namespace or type names that collide with .NET, WPF, or Win32 types
  (e.g. `Registry`, `Task`, `Process`, `Application`, `Path`, `File`).
- Use `nameof(...)` instead of hardcoded symbol-name strings.
- Registry key and value names live in a `{Feature}Registry` class with
  `Keys` and `Values` subclasses.  Both Catalog and Editor reference these
  constants — never hardcode a registry string or put schema names on Item
  classes.

## Null

- Never return null for a collection — return an empty one.

## Event Subscription

- Lambda: ≤5 lines, single subscription, not part of the class contract.
- Named method: longer, reused, required by inheritance/interfaces, or an
  extensibility point.
