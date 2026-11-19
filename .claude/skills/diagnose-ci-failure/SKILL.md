---
name: diagnose-ci-failure
description: "Diagnoses a failed 'CI: Build and Test' run from its artifacts instead of re-running the pipeline. Use when a PR's CI run is red, a GitHub Actions workflow failed, an E2E or Playwright spec passed locally but failed in CI, or you need the trace, screenshot, or application log from a CI run."
argument-hint: "Optional run id, run URL, or branch name"
---

## Canonical source

This skill's full instructions live in the shared, tool-agnostic copy at
`.agents/skills/diagnose-ci-failure/SKILL.md` (the same file GitHub Copilot uses). This `.claude` copy is a
pointer to keep both agents in sync.

**Read `.agents/skills/diagnose-ci-failure/SKILL.md` now and follow it exactly.** Do not act on this stub
alone — the canonical file holds the source of truth: every step, reference
file, abort condition, and workflow detail.
