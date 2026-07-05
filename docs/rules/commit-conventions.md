# Commit Conventions

## Message Format

- Use `<type>: <short English summary>`.  Body optional for trivial changes.
- Do not use PowerShell here-string syntax (`@'...'@`) in Bash — it
  embeds a literal `@` in the commit message.
- After committing, verify with `git log -1 --format='%s'` that the
  subject line starts with the expected `<type>:` prefix.

## Git Operations

- Prefer small, verifiable commands instead of long chains.
- Run Git write operations serially. Do not overlap `add`, `commit`,
  `merge`, `rebase`, or branch-changing commands.
