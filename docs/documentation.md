# Documentation

## Format

- Prefer tables over bullet lists and Mermaid diagrams over numbered lists.

## Language

- All comments, documentation, and developer-facing text must be written in English.

## Code Documentation

- Default to no comment.  Add one only when the code is surprising, works
  around an external constraint, or reflects a non-obvious design choice.
  Never restate what the line of code already says.
- Remove noise comments during any edit that touches the same file.  Existing
  comments that are still correct and non-obvious should stay.
- Public and internal types/methods get a one-line `<summary>` describing
  purpose.  Omit `<param>`, `<returns>`, and `<remarks>` unless the behaviour
  is genuinely unexpected.

## Document Maintenance

- Keep `docs/source-layout.md` focused on durable structure.  Do not update it
  for routine class additions, one-off helper moves, or implementation-level
  refactors that still fit the existing rules.
