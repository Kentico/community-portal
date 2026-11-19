---
name: aspire-monitoring
description: >-
  **ANALYSIS SKILL** - Observe Aspire apps: logs, traces, metrics, resource state,
  telemetry export, browser telemetry, and the standalone dashboard. Routes between local
  Aspire CLI, AKS workload diagnostics, and deployed Azure resource health.
  USE FOR: aspire logs, aspire otel logs, aspire otel traces, aspire otel spans, aspire
  describe, aspire ps, aspire export, aspire dashboard run, --include-hidden, browser logs
  in dashboard, WithBrowserLogs, App Insights query, AKS pod logs, container app logs.
  DO NOT USE FOR: start/stop/wait (use aspire-orchestration), deploy/publish/destroy (use
  aspire-deployment), AppHost code edits like WithBrowserLogs() (use aspireify), Azure
  provisioning (use azure-prepare).
  INVOKES: aspire CLI, azure-diagnostics (deployed Azure), kubectl + Container Insights.
  FOR SINGLE OPERATIONS: Run the aspire CLI command directly for quick log or describe lookups.
license: MIT
metadata:
  author: Microsoft
  version: "0.0.1"
---

## Canonical source

This skill's full instructions live in the shared, tool-agnostic copy at
`.agents/skills/aspire-monitoring/SKILL.md` (the same file GitHub Copilot uses). This `.claude` copy is a
pointer to keep both agents in sync.

**Read `.agents/skills/aspire-monitoring/SKILL.md` now and follow it exactly.** Do not act on this stub
alone — the canonical file holds the source of truth: every step, reference
file, abort condition, and workflow detail.
