---
name: aspire
description: >-
  **WORKFLOW SKILL** - Top-level router for Aspire 13.4 distributed apps. Detects the
  AppHost, enforces safety guardrails, and routes to the right sub-skill.
  USE FOR: Aspire AppHost detected, aspire CLI, distributed app, cloud-native .NET,
  aspire start, aspire stop, aspire resource, aspire deploy, aspire destroy, aspire publish,
  aspire init, aspire new, aspire add, aspire integration list/search, aspire wait,
  aspire describe, aspire ps, aspire dashboard run, aspire doctor, aspire update,
  aspire logs, aspire otel, --include-hidden, aspireify, WithBrowserLogs, custom
  dashboard/resource commands, .aspire/modules recovery, Playwright URL discovery.
  DO NOT USE FOR: non-Aspire .NET projects (use dotnet directly), Azure provisioning
  without Aspire (use azure-prepare), container-only repos with no AppHost, ordinary
  build/test tasks.
  INVOKES: aspire-init, aspireify, aspire-orchestration, aspire-deployment, aspire-monitoring.
  FOR SINGLE OPERATIONS: Route directly to the matching sub-skill.
license: MIT
metadata:
  author: Microsoft
  version: "0.0.1"
---

## Canonical source

This skill's full instructions live in the shared, tool-agnostic copy at
`.agents/skills/aspire/SKILL.md` (the same file GitHub Copilot uses). This `.claude` copy is a
pointer to keep both agents in sync.

**Read `.agents/skills/aspire/SKILL.md` now and follow it exactly.** Do not act on this stub
alone — the canonical file holds the source of truth: every step, reference
file, abort condition, and workflow detail.
