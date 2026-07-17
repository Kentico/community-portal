---
name: local-e2e-setup
description: "Starts the local Development infrastructure needed for Playwright E2E tests and validates it with one minimal authenticated admin smoke test. Use when setting up local E2E testing, Playwright development prerequisites, admin test infrastructure, Vite or webpack watchers, or verifying an E2E environment without running a full suite."
argument-hint: "Optional base URL or reason for setup"
---

# Local E2E Smoke Setup

Prepare this repository for local Playwright E2E testing, then prove the
infrastructure works with the smallest meaningful admin test.

## Scope

Use this for the Development app/runtime. It starts the client watchers and runs
only the authenticated admin-shell smoke test, not `pnpm test:admin` or the full
E2E suite.

Do not stop or replace user-owned app, watcher, or browser processes.

## Procedure

1. Read [docs/Run-Tests.md](../../../docs/Run-Tests.md) and
   `test/kentico-community-portal-web-e2e-tests/playwright.config.ts`.
2. Confirm the E2E package dependencies and Chromium are available. Only run
   `pnpm install` or `pnpm exec playwright install chromium` when they are
   missing; do not update packages.
3. Check the VS Code task output for these local Development asset watchers:
   - `pnpm: watch (Web)` for Vite
   - `pnpm: watch (Admin)` for webpack
4. Start only any watcher that is not already running. Wait for each watcher to
   report it is ready before testing.
5. Reuse an already-running `Kentico.Community.Portal.Web` instance when one is
   available. Otherwise let `playwright.config.ts` start it automatically. Do
   not run a separate build.
6. From `test/kentico-community-portal-web-e2e-tests`, run exactly:

   ```powershell
   pnpm exec playwright test --project=admin-tests tests/admin/admin-signin.smoke.spec.ts
   ```

   This also runs the `admin-setup` dependency, which authenticates the default
   administrator and writes the reusable browser state.

7. Report the command result and whether the local infrastructure is ready.

## Failure Routing

- If the app cannot start or an asset fails to load, check the watcher task
  output and the configured local URLs before changing tests.
- If sign-in, navigation, or an assertion fails, open the same flow with the
  Chrome DevTools MCP server. Inspect the current accessibility snapshot and
  perform the login/navigation manually to distinguish an infrastructure issue
  from an outdated test assumption.
- Keep the validation scope to this one spec. Do not run a broader suite just to
  diagnose setup.
- Preserve user configuration and unrelated working-tree changes.

## Success Criteria

- Both client asset watchers are ready or intentionally unnecessary for a
  published-app run.
- The Development app is reachable at the configured base URL.
- The setup dependency and `admin-signin.smoke.spec.ts` pass.
- No packages, app code, or tests were changed merely to establish the local
  environment.
