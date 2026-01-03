---
allowed-tools: Bash(git add:*), Bash(git reset:*), Bash(git status:*), Bash(git diff:*), Bash(git -c user.name=* -c user.email=* commit:*)
argument-hint: [optional guidance]
description: Commit as Claude with logical separation
---

## Context
- Current status: !`git status --short`
- Full diff: !`git diff`
- Staged diff: !`git diff --cached`

## Additional guidance
$ARGUMENTS

## Task
Analyze all pending changes and create atomic commits, each representing a single logical change.

### Process
1. Review the diff and identify distinct logical units (e.g., refactor vs feature vs bugfix vs formatting)
2. If changes are already well-staged, proceed with that commit
3. If unstaged changes span multiple concerns, stage and commit them separately:
   - `git reset` if needed to unstage
   - `git add <specific files or hunks>` for each logical group
   - Commit each group before moving to the next
4. Use conventional commit format: `type(scope): description`

### Commit command
```
git -c user.name="Claude" -c user.email="claude@anthropic.com" commit -m "<message>"
```

### Guidelines
- One concern per commit
- Order commits logically (dependencies first)
- If unsure whether to split, ask
