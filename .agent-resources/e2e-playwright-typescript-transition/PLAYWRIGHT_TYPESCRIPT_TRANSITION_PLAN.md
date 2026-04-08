# Playwright TypeScript E2E Transition Plan

## Goal

Transition the repository's end-to-end test suite from Playwright for .NET +
NUnit to Playwright Test with TypeScript, while using the application's existing
Virtual Inbox MCP server for email-dependent flows.

Move moderation-state setup out of the test process and into the ASP.NET Core
application through a development/CI-only minimal API endpoint that is
explicitly documented as an E2E testing tool.

## Why This Transition Makes Sense

The current E2E suite already uses Playwright, but it is tied to:

- a .NET test project
- NUnit orchestration
- `*.runsettings` Playwright configuration
- direct SQL reads against `CMS_Email` to fetch confirmation and recovery links
- direct SQL cleanup for email records
- custom browser installation via the generated `.NET` `playwright.ps1`
- direct SQL mutation from the test process for moderation-state setup

The article recommends a different model:

- Playwright Test with TypeScript
- Playwright `webServer` orchestration to reuse or start the app
- calling MCP tools from code with `@modelcontextprotocol/sdk`
- using `cheerio` to parse rendered email HTML
- treating email as a real part of the flow, not as a mocked dependency

The same architectural boundary should be applied to moderation setup: if tests
need to change application state, the application should expose a purpose-built,
non-production testing surface instead of letting the test suite reach into SQL
directly.

This repository is already close to that target because the web app already:

- registers Virtual Inbox services in development
- exposes MCP tools in development
- maps the MCP endpoint at `/mcp`
- enables Virtual Inbox in `appsettings.Development.json`

The moderation-state endpoint should follow the same principle as Virtual Inbox
MCP: test support lives in the application boundary, not inside the test runner.

## Current State Summary

### Existing .NET E2E assets

- `test/Kentico.Community.Portal.Web.E2E.Tests/`
- `test/e2e.runsettings`
- `.vscode/tasks.json` entries that invoke `.NET` E2E build, browser install,
  and test execution
- `scripts/Download-PlaywrightBrowsers.ps1`
- `scripts/Run-E2E-Tests.ps1`
- CI steps in `.github/workflows/ci.yml` that publish the app, install
  Playwright browsers through the .NET-generated script, then run `dotnet test`
- `Kentico.Community.Portal.sln` entry for the `.NET` E2E project

### Existing test coverage

- smoke/home-page rendering
- registration + email confirmation + login
- spam moderation login restriction
- account recovery + reset password

### Existing email testing approach

Email-dependent assertions currently query `CMS_Email` directly from the test
process, then parse HTML with AngleSharp.

That should be replaced with Virtual Inbox MCP calls because it is closer to
real behavior, removes brittle DB coupling for email retrieval, and matches the
article's approach.

### Existing moderation-state approach

Moderation-state setup currently runs SQL directly from `MembershipTests.cs`
through `SetModerationStatus()`.

That should be replaced with a development/CI-only ASP.NET Core minimal API
endpoint so the application owns the state transition contract used by E2E
tests.

### Existing orchestration coupling

The current E2E implementation is also wired into three operational surfaces
that must transition with the tests themselves:

- `test/e2e.runsettings` for browser and test-runner behavior
- `.vscode/tasks.json` for local developer commands
- `.github/workflows/ci.yml` for CI installation, startup, execution, and
  artifact publishing

The migration is incomplete until those surfaces target the Node.js and
TypeScript Playwright implementation instead of the `.NET` test project.

There are also a few secondary references that should be cleaned up during
cutover:

- `Kentico.Community.Portal.sln`
- `docs/Run-Tests.md`
- `docs/Architecture.md`
- any ADR or internal documentation that still describes the E2E lane as
  Playwright.NET or assumes `.NET`-specific orchestration

## Target State

Create a dedicated TypeScript Playwright workspace under `test/kentico-community-portal-web-e2e-tests/` and
migrate all E2E tests there.

Recommended structure:

```text
test/
  playwright/
    package.json
    package-lock.json
    tsconfig.json
    playwright.config.ts
    tests/
      smoke/
      membership/
    fixtures/
    support/
      mcp/
      email/
      auth/
      data/
```

This should be a separate Node workspace, not merged into
`src/Kentico.Community.Portal.Web/package.json`.

Reasoning:

- E2E tests are a separate concern from Vite asset compilation.
- CI caching and install steps stay isolated.
- test-only dependencies like `@playwright/test`, `@modelcontextprotocol/sdk`,
  `cheerio`, and possibly SQL helpers do not pollute the web client package.
- future E2E tooling can evolve independently of frontend build tooling.

The application should also expose a small `E2E testing tools` surface for
non-UI state setup that cannot be reached naturally through the browser,
starting with member moderation.

## Core Design Decisions

### 1. Use Playwright Test, not a custom TypeScript runner

Use the standard Playwright TypeScript stack created by
`npm init playwright@latest` and then reshape it to the repository's needs.

### 2. Use Playwright `webServer`

Follow the article's recommendation and let `playwright.config.ts` manage app
startup when practical.

Recommended behavior:

- local: `reuseExistingServer: true` when the app is already running
- local fallback: start the ASP.NET Core app automatically if it is not running
- CI: do not reuse an existing server; let Playwright `webServer` start the app
  and wait for readiness through `url` and `timeout`

Recommended config pattern:

- local command:
  `dotnet run --project ./src/Kentico.Community.Portal.Web/Kentico.Community.Portal.Web.csproj`
- CI command: either the same command with `--no-build`, or
  `dotnet ./publish/Kentico.Community.Portal.Web.dll` if published output
  remains preferable
- CI readiness URL: map the current `StatusCheckUrl` concept to `webServer.url`
- CI startup timeout: map the current `MaxWaitSeconds` concept to
  `webServer.timeout`

Target rule:

- startup waiting should have a single owner
- in the TypeScript target architecture, that owner should be
  `playwright.config.ts`, not a separate CI PowerShell polling loop

### 3. Replace email DB reads with MCP client helpers

Create a reusable TypeScript helper around `@modelcontextprotocol/sdk` to call
the app's MCP endpoint.

Primary tool usage:

- `wait_for_email` for synchronization
- `get_virtual_email_by_guid` when follow-up detail is needed
- `list_virtual_emails` only for diagnostics or troubleshooting

### 4. Parse rendered email HTML with `cheerio`

Use HTML parsing to extract confirmation and password-reset links from actual
rendered email output.

This replaces the current AngleSharp + SQL pattern.

### 5. Add an application-owned minimal API for moderation test setup

Add a minimal API endpoint that is only available in Development and CI-style
environments and is clearly identified as a tool for E2E testing.

Recommended characteristics:

- mounted under a dedicated route group such as `/testing/e2e`
- enabled only when `env.IsDevelopment()` or a dedicated CI environment check
  passes
- excluded from production environments entirely
- strongly typed request model for the target member and moderation status
- explicit endpoint metadata and summary text stating
  `Tool for E2E testing only`
- simple auth/guard strategy appropriate for local and CI use if needed
- implemented in application code, not in Playwright helpers

The endpoint should become the only supported way for Playwright tests to set
moderation state.

### 6. Prefer deterministic, isolated tests over one long scenario

The current `MembershipTests.cs` is effectively a long workflow chain. In
TypeScript, split it into focused tests or at least focused helper-driven
sections so failures are easier to localize.

Suggested grouping:

- `membership/register-and-confirm.spec.ts`
- `membership/login-moderation.spec.ts`
- `membership/password-recovery.spec.ts`
- optionally a single `membership-happy-path.spec.ts` if a full
  business-critical flow is still valuable

### 7. Replace .NET orchestration surfaces, not just test files

The transition must explicitly replace the three places that currently define
how E2E tests run:

- `test/e2e.runsettings`
- `.vscode/tasks.json`
- `.github/workflows/ci.yml`

In the TypeScript implementation, those responsibilities move to:

- `test/kentico-community-portal-web-e2e-tests/playwright.config.ts` for runtime/browser behavior
- `test/kentico-community-portal-web-e2e-tests/package.json` scripts plus VS Code tasks for local execution
- CI steps that run `npm ci` and `npx playwright test`

The final cleanup must also remove solution-level references to the `.NET` E2E
project and update documentation so the repository no longer advertises the
retired execution path.

## Transition Phases

## Phase 0: Freeze Scope And Inventory

1. Confirm the migration scope is only the current E2E coverage, not a broader
   testing redesign.
2. Inventory all current .NET E2E tests and helper behavior.
3. Record which behaviors must remain covered after cutover:
   - home page renders
   - registration requires email confirmation
   - unconfirmed login is blocked
   - confirmed login succeeds
   - spam-moderated accounts are blocked
   - password recovery email works
   - password reset allows sign-in with the new password
4. Confirm which environment assumptions the tests need:
   - SQL Server
   - Azurite
   - development app settings with Virtual Inbox enabled
   - MCP endpoint available at `/mcp`
   - E2E testing endpoint available in Development and CI environments for
     moderation-state setup

## Phase 1: Create The TypeScript Playwright Workspace

1. Create `test/kentico-community-portal-web-e2e-tests/`.
2. Initialize Playwright with TypeScript.
3. Install dependencies:
   - `@playwright/test`
   - `@modelcontextprotocol/sdk`
   - `cheerio`
4. Add repository scripts to `package.json`, for example:
   - `test`
   - `test:headed`
   - `test:ui`
   - `install:browsers`
   - `report`
5. Add `tsconfig.json` with strict settings suitable for test code.
6. Add `.gitignore` entries under the test workspace if needed for:
   - `playwright-report/`
   - `test-results/`
   - `.playwright/`

## Phase 2: Define Runtime Configuration

1. Create `playwright.config.ts`.
2. Configure:
   - `testDir`
   - `use.baseURL`
   - `use.trace = 'retain-on-failure'`
   - screenshots/video policy
   - Chromium-only execution initially
   - sensible retries in CI
3. Add `webServer` config with `reuseExistingServer: !process.env.CI`.
4. Centralize environment variables:
   - app base URL
   - MCP URL, defaulting to `${baseURL}/mcp`
   - E2E tools base URL if it differs from the main app base URL
5. Decide whether local HTTPS stays enabled or whether local E2E should target
   HTTP to reduce certificate friction.
6. Mirror current browser behavior where useful:
   - Chromium
   - viewport around `1080x1920` or a deliberate replacement
   - ignore HTTPS errors if local HTTPS remains in use
7. Create an explicit mapping from `test/e2e.runsettings` into
   `playwright.config.ts`.
8. Create an explicit mapping from the current CI startup-wait logic into
   `playwright.config.ts`.

Recommended mapping:

- `<Playwright><BrowserName>chromium</BrowserName>` ->
  `projects/use.browserName = 'chromium'` or default Chromium project
- `<ExpectTimeout>5000</ExpectTimeout>` -> `expect.timeout = 5000`
- `<LaunchOptions><Headless>true</Headless>` -> `use.headless = true` in
  CI/default scripts
- `<LaunchOptions><Channel>chrome</Channel>` -> `use.channel = 'chrome'` only if
  there is a real need to keep Chrome instead of bundled Chromium
- debug environment variables from `RunConfiguration/EnvironmentVariables` ->
  `use`, `env`, or npm script flags such as `DEBUG=pw:api`
- `Run-E2E-Tests.ps1 -StatusCheckUrl ...` -> `webServer.url`
- `Run-E2E-Tests.ps1 -MaxWaitSeconds 30` -> `webServer.timeout = 30000`
- `Run-E2E-Tests.ps1` HTTP polling loop -> remove in the target architecture and
  rely on Playwright readiness checks

After this mapping is in place, `test/e2e.runsettings` should be treated as
legacy compatibility only and removed after cutover.

The same applies to the CI startup wait wrapper: once `webServer` is
authoritative, the readiness polling should be removed rather than duplicated.

## Phase 3: Build Test Support Layers

1. Add `support/mcp/createMcpClient.ts`.
2. Add `support/email/waitForEmail.ts` that wraps `wait_for_email`.
3. Add `support/email/extractLinks.ts` using `cheerio`.
4. Add shared page helpers or page objects only where they reduce duplication.
5. Add test data factories for unique usernames and emails.
6. Add assertion helpers for common authenticated and anonymous nav checks.
7. Add a small client helper for the E2E testing endpoint, for example
   `support/testingTools/moderationClient.ts`.

Do not over-engineer page objects. Prefer small helpers first.

## Phase 4A: Add Application-Owned E2E Testing Tools

1. Add a dedicated minimal API route group for E2E testing tools, for example
   `/testing/e2e`.
2. Restrict the route group to Development and CI-only environments.
3. Add a moderation endpoint, for example `POST /testing/e2e/member-moderation`.
4. Define a typed request contract containing the member identifier and target
   moderation status.
5. Implement the state change inside the application using the app's own
   service/data layer or a focused infrastructure service.
6. Add endpoint metadata that clearly labels it as an E2E testing tool, for
   example:
   - summary: `Tool for E2E testing only`
   - description:
     `Development/CI-only endpoint used by Playwright to prepare member moderation state`
7. Ensure the route group is omitted entirely from production.
8. Add a short note in docs that this endpoint exists only to support
   deterministic E2E testing.

## Phase 4C: Transition VS Code Tasks

1. Inventory current E2E-related tasks in `.vscode/tasks.json`:
   - `.NET: test (Install Playwright Dependencies)`
   - `.NET: build (Test E2E)`
   - `.NET: test (E2E)`
2. Add Node/TypeScript equivalents, for example:
   - `npm: install (E2E)` targeting `test/kentico-community-portal-web-e2e-tests`
   - `npm: test (Install Playwright Browsers E2E)` or
     `npm: e2e install:browsers`
   - `npm: test (E2E)` running `npx playwright test`
   - `npm: test (E2E headed)` running headed mode
   - `npm: test (E2E report)` running `npx playwright show-report`
3. Decide whether to keep a compatibility task label during migration, for
   example leaving `.NET: test (E2E)` in place temporarily but redirecting it to
   the new Node implementation after cutover.
4. Remove task dependencies on:
   - `.NET: build (Test E2E)`
   - `.NET: test (Install Playwright Dependencies)`
5. Replace those dependencies with Node-oriented prerequisites:
   - `npm ci` or `npm install` in `test/kentico-community-portal-web-e2e-tests`
   - `npx playwright install chromium`
6. Keep app startup separate:
   - either reuse `.NET: run (Web)` / `.NET: watch (Web)`
   - or rely on Playwright `webServer`
7. Update task labels and details so the E2E lane is clearly identified as
   Playwright TypeScript, not Playwright.NET.

## Phase 4D: Transition CI Workflow

1. Replace the current CI dependency on `test/e2e.runsettings` by moving
   browser/runtime config into `test/kentico-community-portal-web-e2e-tests/playwright.config.ts` and npm
   scripts.
2. Add Node setup for the E2E workspace if not already present in the workflow.
3. Replace the current `.NET` browser install step:
   - from
     `./test/Kentico.Community.Portal.Web.E2E.Tests/bin/Release/net10.0/playwright.ps1 install`
   - to `npx playwright install chromium` in `test/kentico-community-portal-web-e2e-tests`
4. Replace the current E2E execution step:
   - from
     `./Run-E2E-Tests.ps1 ... -TestProjectPath ../test/Kentico.Community.Portal.Web.E2E.Tests/... -TestSettings ../test/e2e.runsettings`
   - to a Node-oriented command path, for example `npm ci` then
     `npx playwright test`
5. Move app readiness waiting out of CI PowerShell and into
   `playwright.config.ts`:
   - current `StatusCheckUrl` becomes `webServer.url`
   - current `MaxWaitSeconds` becomes `webServer.timeout`
   - remove the standalone HTTP polling loop from the target CI path
6. Prefer a single startup owner in CI:
   - if Playwright `webServer` starts the app, CI should not also start and poll
     the app
   - if a wrapper script survives temporarily for diagnostics, it should not
     duplicate readiness waiting
7. Update cache keys to reflect the Node E2E workspace, preferably based on
   `test/kentico-community-portal-web-e2e-tests/package-lock.json`.
8. Publish Playwright-native artifacts:
   - HTML report
   - traces
   - screenshots
   - videos if enabled
9. Keep app diagnostics and startup logs only if they can be captured without
   reintroducing a second readiness wait loop.

## Phase 4B: Replace Email Retrieval With Virtual Inbox MCP

1. Translate the current confirmation email retrieval logic into:
   - wait for email by recipient
   - assert subject
   - parse structured content or HTML body
   - extract confirmation URL
2. Translate the current recovery email retrieval logic the same way.
3. Remove the need for `ResetMemberEmails()` by using unique inbox values per
   test run and, when necessary, `sinceId` filtering or equivalent MCP query
   narrowing.
4. Add good failure output when email does not arrive:
   - recipient
   - expected subject substring
   - timeout
   - optional recent-email diagnostics

## Phase 5: Migrate Tests One By One

### 5A. Smoke tests

1. Port `HomePageTests.cs` first.
2. Verify the app starts correctly under Playwright TypeScript.
3. Verify traces and reports are captured correctly in local and CI runs.

### 5B. Membership flow tests

1. Port registration + confirmation.
2. Port login-after-confirmation assertions.
3. Port password recovery + reset flow.
4. Replace direct moderation SQL calls with the development/CI-only E2E testing
   endpoint.

For moderation-status mutation, the target approach is fixed:

1. add an application-owned minimal API endpoint for E2E testing
2. call that endpoint from Playwright helper code
3. remove direct SQL mutation from the test suite

Rationale:

- Email retrieval is exactly what Virtual Inbox solves.
- Member moderation state is not part of Virtual Inbox.
- The application, not the test process, should own the contract for non-UI test
  setup.
- This keeps the test harness closer to real behavior while still allowing
  deterministic setup for cases that cannot be reached purely through the
  browser.

## Phase 6: Run Both Suites In Parallel Temporarily

1. Keep the current .NET E2E project running while the new TypeScript suite is
   incomplete.
2. Add a parity checklist mapping each old test behavior to a new spec.
3. Gate removal of the .NET E2E suite on:
   - parity achieved
   - local workflow documented
   - CI green with Playwright TypeScript
   - moderation setup handled through the application-owned E2E testing endpoint
   - VS Code E2E tasks switched to the Node/TypeScript implementation
   - `e2e.runsettings` no longer required for E2E execution

This avoids a risky big-bang cutover.

## Phase 7: Replace Local Developer Workflow

1. Add VS Code tasks for:
   - `npm install` in `test/kentico-community-portal-web-e2e-tests`
   - browser install for Playwright TypeScript
   - E2E test run
   - headed test run
   - report viewer
2. Update `docs/Run-Tests.md` to describe:
   - how to install browsers
   - whether the app auto-starts or should already be running
   - how MCP-backed email tests work
   - how the development/CI-only E2E testing endpoint is used for moderation
     setup
   - that `e2e.runsettings` has been replaced by
     `test/kentico-community-portal-web-e2e-tests/playwright.config.ts`
3. Update `docs/Architecture.md` to rename the E2E project description from
   Playwright.NET to Playwright Test + TypeScript.
4. Update `.vscode/tasks.json` so local execution no longer points at the `.NET`
   E2E project.
5. Review any internal docs or ADRs that mention E2E tooling and update only the
   ones that are now inaccurate.

## Phase 8: Replace CI Pipeline Steps

1. Keep the existing .NET restore/build/publish steps that prepare the
   application.
2. Remove the E2E workflow's dependency on `test/e2e.runsettings` and move those
   settings into `test/kentico-community-portal-web-e2e-tests/playwright.config.ts`.
3. Add Node setup for the new Playwright workspace.
4. Cache:
   - `test/kentico-community-portal-web-e2e-tests/node_modules`
   - Playwright browser cache
5. Replace `.NET` Playwright browser installation:
   - remove dependency on `test/.../playwright.ps1 install`
   - use `npx playwright install chromium`
6. Replace `dotnet test` E2E execution with:
   - `npm ci`
   - `npx playwright test`
7. Keep existing environment bootstrapping that the app still needs:
   - SQL Server
   - database initialization
   - Azurite
   - license restore/configuration
8. Upload artifacts on failure and preferably on success too:
   - `playwright-report/`
   - `test-results/`
   - traces/screenshots/videos
   - app logs if the app is still started out of process

## Phase 9: Remove .NET-Specific E2E Infrastructure

After parity and CI cutover:

1. Remove `test/Kentico.Community.Portal.Web.E2E.Tests/`.
2. Remove `test/e2e.runsettings` after its settings have been fully represented
   in `test/kentico-community-portal-web-e2e-tests/playwright.config.ts`.
3. Remove or replace `.vscode/tasks.json` entries tied to `.NET` E2E execution.
4. Remove or simplify `scripts/Download-PlaywrightBrowsers.ps1`.
5. Remove or simplify `scripts/Run-E2E-Tests.ps1`.
6. Remove old CI steps tied to `.NET` Playwright execution.
7. Remove the `.NET` E2E project from `Kentico.Community.Portal.sln`.
8. Update solution-adjacent docs, onboarding instructions, and any stale E2E
   architecture references.

## Migration Details By Current Test Helper

### `CommunityPageTests.cs`

Replace with Playwright config defaults:

- `baseURL`
- viewport
- color scheme if still relevant
- HTTPS behavior

### `GetConfirmationEmailURL()`

Replace with `wait_for_email` + `cheerio` parsing.

### `GetRecoveryEmailURL()`

Replace with `wait_for_email` + `cheerio` parsing.

### `ResetMemberEmails()`

Delete after migration unless a real cleanup need remains. Unique emails per
test should make this unnecessary.

### `SetModerationStatus()`

Replace with calls to the application-owned minimal API endpoint for E2E
testing.

Recommended endpoint shape:

- route group: `/testing/e2e`
- route: `POST /testing/e2e/member-moderation`
- enabled only in Development and CI-style environments
- endpoint summary/description explicitly stating `Tool for E2E testing only`
- implementation owned by the ASP.NET Core application

## Recommended File-Level Implementation Plan

1. Add `test/kentico-community-portal-web-e2e-tests/package.json`.
2. Add `test/kentico-community-portal-web-e2e-tests/tsconfig.json`.
3. Add `test/kentico-community-portal-web-e2e-tests/playwright.config.ts`.
4. Add `test/kentico-community-portal-web-e2e-tests/support/mcp/createMcpClient.ts`.
5. Add `test/kentico-community-portal-web-e2e-tests/support/email/waitForEmail.ts`.
6. Add `test/kentico-community-portal-web-e2e-tests/support/email/extractLinks.ts`.
7. Add `test/kentico-community-portal-web-e2e-tests/support/data/testIdentity.ts`.
8. Add `test/kentico-community-portal-web-e2e-tests/support/testingTools/moderationClient.ts`.
9. Add ASP.NET Core E2E testing endpoint implementation for moderation state.
10. Add `test/kentico-community-portal-web-e2e-tests/tests/smoke/home.spec.ts`.
11. Add `test/kentico-community-portal-web-e2e-tests/tests/membership/register-and-confirm.spec.ts`.
12. Add `test/kentico-community-portal-web-e2e-tests/tests/membership/password-recovery.spec.ts`.
13. Add moderation test support using the new endpoint.
14. Update `.vscode/tasks.json`.
15. Update `.github/workflows/ci.yml`.
16. Update `docs/Run-Tests.md`.
17. Update `docs/Architecture.md`.
18. Remove the `.NET` E2E project from `Kentico.Community.Portal.sln`.
19. Review ADR/internal docs for stale Playwright.NET references.
20. Remove `test/e2e.runsettings` after parallel validation.
21. Remove old .NET E2E assets after parallel validation.

## Transition Mapping For Current Operational Files

### `test/e2e.runsettings`

Current responsibility:

- NUnit filtering for E2E lane selection
- Playwright browser defaults
- Playwright expect timeout
- launch options

Transition target:

- E2E lane selection moves out of `.NET` test filtering entirely
- browser defaults move to `test/kentico-community-portal-web-e2e-tests/playwright.config.ts`
- timeout moves to `expect.timeout`
- launch options move to `use` config and npm scripts

Cutover rule:

- keep `test/e2e.runsettings` only while the `.NET` E2E suite still runs
- delete it once the TypeScript suite is the only E2E lane

### `.vscode/tasks.json`

Current responsibility:

- builds the `.NET` E2E project
- installs Playwright browsers through the generated `.NET` script
- runs E2E via `dotnet test --settings ./test/e2e.runsettings`

Transition target:

- task-based install uses the new `test/kentico-community-portal-web-e2e-tests` package scripts
- browser installation uses `npx playwright install chromium`
- E2E execution uses `npx playwright test`
- optional report viewer uses `npx playwright show-report`

Cutover rule:

- during migration, keep both old and new task lanes
- after parity, remove or rename the old `.NET` E2E tasks so there is a single
  obvious E2E path

### `.github/workflows/ci.yml`

Current responsibility:

- publishes the app
- installs browsers through the `.NET` Playwright script
- runs E2E through `Run-E2E-Tests.ps1` and `dotnet test`
- uploads app logs on failure
- waits for app readiness indirectly through `Run-E2E-Tests.ps1`

Transition target:

- keep app build/publish/environment bootstrap as needed
- install Node dependencies in `test/kentico-community-portal-web-e2e-tests`
- install Playwright browsers with `npx playwright install chromium`
- run E2E with `npx playwright test`
- upload Playwright reports and traces alongside app diagnostics
- move readiness waiting into `test/kentico-community-portal-web-e2e-tests/playwright.config.ts` via
  `webServer.url` and `webServer.timeout`
- remove CI-side startup polling from the steady-state design

Cutover rule:

- once CI is green on the TypeScript suite, remove the old `.NET` E2E block
  entirely instead of maintaining two permanent execution paths

### `scripts/Run-E2E-Tests.ps1`

Current responsibility:

- starts the published app
- waits for the app to become ready by polling `StatusCheckUrl`
- applies a startup timeout through `MaxWaitSeconds`
- runs `.NET` E2E tests
- captures app logs

Transition target:

- do not use this script as the primary E2E runner once Playwright TypeScript is
  in place
- move readiness waiting into `test/kentico-community-portal-web-e2e-tests/playwright.config.ts`
- keep only any residual diagnostic or log-capture behavior if it is still
  needed and cannot be handled more simply elsewhere

Cutover rule:

- once Playwright `webServer` is the startup owner in CI, remove the readiness
  polling behavior from this script or retire the script entirely

### `Kentico.Community.Portal.sln`

Current responsibility:

- includes the `.NET` E2E project in the solution

Transition target:

- remove the retired `.NET` E2E project once the TypeScript suite is the
  supported E2E implementation

Cutover rule:

- keep the solution entry only while the `.NET` E2E project still exists and is
  used for parity checks
- remove it during final cleanup so solution metadata matches the supported test
  architecture

## Risks And Decisions To Resolve Early

### 1. Moderation-state setup seam

This is the only major behavior in the current suite that does not naturally map
to browser + Virtual Inbox testing.

Decision needed:

- exact route shape and request contract for the E2E testing endpoint
- whether Development and CI should be detected by environment name, config
  flag, or both
- whether the endpoint needs a lightweight secret/header guard in CI

### 2. Local startup model

Decide whether developers should:

- run the app themselves and let Playwright reuse it
- or rely on `webServer` to start it automatically

Recommended default:

- support both, with `reuseExistingServer` locally

### 3. HTTPS and certificates

If local E2E keeps HTTPS, configure Playwright to ignore HTTPS errors in dev. If
certificate issues keep causing friction, consider a dedicated local HTTP E2E
base URL.

### 4. CI startup strategy

Target approach:

- let Playwright start the app directly through `webServer`
- move readiness waiting into `webServer.url` and `webServer.timeout`
- do not keep a second external polling loop in CI

## Suggested Rollout Order

1. Create the TS Playwright workspace.
2. Port the home page smoke test.
3. Implement MCP email client helpers.
4. Port registration + email confirmation.
5. Port password recovery.
6. Resolve moderation-state setup. Implement the application-owned E2E testing
   endpoint and switch the moderation spec to it.
7. Run old and new suites in parallel.
8. Switch CI to TypeScript Playwright.
9. Remove the .NET E2E project and scripts.

## Definition Of Done

The transition is complete when:

1. all current E2E behaviors are covered by Playwright TypeScript specs
2. email-dependent tests use Virtual Inbox MCP instead of SQL reads from
   `CMS_Email`
3. moderation-state setup uses the application's development/CI-only E2E testing
   endpoint instead of direct SQL in the test runner
4. local developer setup is documented and repeatable
5. CI runs `npx playwright test` successfully and publishes Playwright artifacts
6. the old `.NET` E2E project and its support scripts are removed
7. `.vscode/tasks.json`, `test/e2e.runsettings`, and `.github/workflows/ci.yml`
   no longer depend on the `.NET` E2E implementation
8. `Kentico.Community.Portal.sln` and the relevant docs no longer reference the
   retired `.NET` E2E lane
9. CI startup waiting is owned by `test/kentico-community-portal-web-e2e-tests/playwright.config.ts` rather
   than duplicated in PowerShell and workflow logic

## Practical First Implementation Slice

If the transition is done incrementally, the first meaningful slice should be:

1. scaffold `test/kentico-community-portal-web-e2e-tests/`
2. add `playwright.config.ts` with local app startup/reuse
3. add MCP email client helper
4. port the home page smoke test
5. port registration + confirmation using Virtual Inbox MCP
6. add the moderation E2E testing endpoint before porting the moderation spec

That slice proves the hardest new pieces early:

- TypeScript Playwright wiring
- app orchestration
- MCP integration
- rendered email parsing
- business-critical membership coverage

The next slice after that should implement the application-owned moderation
endpoint so the TypeScript suite never inherits the current direct-SQL mutation
pattern.
