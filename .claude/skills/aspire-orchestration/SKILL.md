---
name: aspire-orchestration
description: >-
  **WORKFLOW SKILL** — Manage Aspire AppHost lifecycle and recover from file locks,
  port conflicts, and orphaned processes. WHEN: "start my Aspire app", "aspire start",
  "aspire stop", "aspire wait", "restart the API service", "file lock error",
  "MSB3491", "CS2012", "port already in use", "upgrade Aspire CLI", "aspire update --self",
  "proxies missing in aspire ps", "--include-hidden", "aspire integration list",
  "aspire integration search", "default watch", "hot reload". INVOKES: aspire CLI
  (start, stop, wait, ps, resource, integration, add, init, doctor, update, restore).
  FOR SINGLE OPERATIONS: Run the aspire CLI command directly.
license: MIT
metadata:
  author: Microsoft
  version: "0.0.1"
---

## Canonical source

This skill's full instructions live in the shared, tool-agnostic copy at
`.agents/skills/aspire-orchestration/SKILL.md` (the same file GitHub Copilot uses). This `.claude` copy is a
pointer to keep both agents in sync.

**Read `.agents/skills/aspire-orchestration/SKILL.md` now and follow it exactly.** Do not act on this stub
alone — the canonical file holds the source of truth: every step, reference
file, abort condition, and workflow detail.
