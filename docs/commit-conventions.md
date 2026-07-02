# Commit Conventions

## Message Format

- Use `<type>: <short English summary>`.  Prefer including a brief body
  describing what changed and why; title-only commits are acceptable for
  trivial or self-explanatory changes.
- When running `git commit` in PowerShell, use a single-quoted here-string
  (`@' ... '@`) for multi-line messages so that `@` is not accidentally
  embedded in the commit message.

## Git Operations

- Prefer small, verifiable PowerShell and Git commands instead of long chained
  commands.
- Run Git write operations serially. Do not overlap `git add`, `git commit`,
  `git merge`, `git rebase`, or branch-changing commands.
