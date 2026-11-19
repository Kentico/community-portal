# Global rules

IMPORTANT: In all interactions and commit messages, be _extremely_ concise and
sacrifice grammar for the sake of concision.

IMPORTANT: There is usually a "Watch" dev server running. Check its output to
see if code changes compiled. Only run a separate build command when instructed
to.

When committing changes, follow
[Git commit conventions](.agent-resources/instructions/Git.instructions.md) for
type, scope, and message format.

When creating agent generated research or plan documents, always store them in
the `.agent-resources/plans` folder.

## Skills

`.agents/skills/` holds only skills unique to this repo. It is the canonical,
tool-agnostic copy — GitHub Copilot reads it directly.

Each skill also has a stub at `.claude/skills/<name>/SKILL.md` carrying just the
frontmatter and a pointer to the canonical file.

## Instructions

Load these instructions as-needed when working on relevant technology in the
project.

- C# - `.github\instructions\CSharp.instructions.md`
- CSS - `.agent-resources\instructions\CSS.instructions.md`
- Xperience code - `.github\instructions\Xperience.instructions.md`
- Git - `.agent-resources\instructions\Git.instructions.md`
- Tests (unit, integration, E2E commands) - `docs\Run-Tests.md`

## Agent skills

### Issue tracker

Issues live as GitHub issues on this repository's `origin` remote. See `docs/agents/issue-tracker.md`.

### Domain docs

Single-context — `CONTEXT.md` + `docs/adr/` at the repo root. See `docs/agents/domain.md`.
