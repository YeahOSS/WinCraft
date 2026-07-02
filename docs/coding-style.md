# Coding Style

## File Encoding

- Prefer UTF-8 when reading or writing text files. Do not change BOM, line
  endings, or file encoding unless the task explicitly requires it.

## Naming

- Prefer capability names (`RegistryAccess`, `PrivilegeBroker`) over platform
  nouns (`Registry`, `Process`).  Applies to types and namespaces alike.
- Avoid namespace or type names that collide with common .NET, WPF, or Win32
  framework types.  Do not introduce project namespaces named exactly like
  framework surface areas such as `Registry`, `Task`, `Process`,
  `Application`, `Path`, `File`, `Directory`, or `Window`.
- Avoid hardcoded symbol-name strings — use `nameof(...)`.

## Null

- Never return null for a collection — return an empty one.

## Event Subscription

- Lambda: ≤5 lines, single subscription, not part of the class contract.
- Named method: longer, reused, required by inheritance/interfaces, or an extensibility point.
- Don't extract a trivial lambda or inline a long handler.
