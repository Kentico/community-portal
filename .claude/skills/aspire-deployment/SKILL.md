---
name: aspire-deployment
description: "**WORKFLOW SKILL** — Deploy Aspire apps from AppHost models to Docker Compose, Kubernetes, Azure, or AWS. WHEN: \"deploy Aspire app\", \"publish Aspire artifacts\", \"deploy to Azure Container Apps\", \"generate Kubernetes artifacts\", \"tear down Aspire deployment\". INVOKES: aspire CLI, Aspire docs, target cloud/container CLIs. FOR SINGLE OPERATIONS: use generic Azure, Kubernetes, Docker, or AWS tools only when no Aspire AppHost exists."
license: MIT
metadata:
  author: Microsoft
  version: "0.0.1"
---

## Canonical source

This skill's full instructions live in the shared, tool-agnostic copy at
`.agents/skills/aspire-deployment/SKILL.md` (the same file GitHub Copilot uses). This `.claude` copy is a
pointer to keep both agents in sync.

**Read `.agents/skills/aspire-deployment/SKILL.md` now and follow it exactly.** Do not act on this stub
alone — the canonical file holds the source of truth: every step, reference
file, abort condition, and workflow detail.
