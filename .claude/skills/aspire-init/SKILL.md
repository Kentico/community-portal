---
name: aspire-init
description: >-
  **WORKFLOW SKILL** - First-run flow for adding Aspire to a repo. Picks `aspire new`
  (greenfield) or `aspire init` (existing repo), drops the AppHost skeleton, then hands
  off to `aspireify` for resource wiring.
  USE FOR: aspire init, aspire new, aspire-starter, aspire-ts-starter, aspire-py-starter,
  add Aspire to existing repo, scaffold Aspire app, bootstrap Aspire, no AppHost detected,
  install aspireify, generated .aspire/modules.
  DO NOT USE FOR: AppHost wiring on an existing AppHost (use aspireify), start/stop/wait
  (use aspire-orchestration), deploy/publish (use aspire-deployment), logs/traces (use
  aspire-monitoring), repo that already has an AppHost.
  INVOKES: aspire CLI (init, new, doctor), aspireify (handoff after skeleton drop).
  FOR SINGLE OPERATIONS: Run `aspire init` or `aspire new TEMPLATE` directly.
license: MIT
metadata:
  author: Microsoft
  version: "0.0.1"
---

## Canonical source

This skill's full instructions live in the shared, tool-agnostic copy at
`.agents/skills/aspire-init/SKILL.md` (the same file GitHub Copilot uses). This `.claude` copy is a
pointer to keep both agents in sync.

**Read `.agents/skills/aspire-init/SKILL.md` now and follow it exactly.** Do not act on this stub
alone — the canonical file holds the source of truth: every step, reference
file, abort condition, and workflow detail.
