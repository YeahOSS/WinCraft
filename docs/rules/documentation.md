# Documentation

All documents under `docs/` are written primarily for AI — no prose,
no redundancy, no restating what code already says.

| Directory | Purpose |
|-----------|---------|
| `rules/` | AI behavioural rules — naming, layout, interop, testing, commits |
| `features/` | Technical references — architecture, protocols, domain models |

## Common Rules

- Prefer tables over bullet lists and Mermaid diagrams over numbered lists.
- All comments and developer-facing text must be written in English.

## Code Comment

- Default to no comment.  Add one only when the code is surprising, works
  around an external constraint, or reflects a non-obvious design choice.
- Remove noise comments during any edit that touches the same file.

## Rules Documents (`docs/rules/`)

- One rule per line where possible.  Bullet lists over prose paragraphs.
- State the rule, then the exception (if any).  Don't explain the rationale
  unless it's genuinely surprising.
- Reference other rule docs by filename when a boundary is crossed
  (e.g. "see `source-layout.md` for placement rules").

## Feature Documents (`docs/features/`)

- Name after the domain (`context-menu.md`, `ipc-protocol.md`).
- Describe structure and relationships, not implementation details.
