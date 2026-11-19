# Run Tests

There are three test lanes in this repository.

| Lane        | Project                                               | Runner                       |
| ----------- | ----------------------------------------------------- | ---------------------------- |
| Unit        | `test/Kentico.Community.Portal.Web.Tests`             | `dotnet test` (MTP)          |
| Integration | `test/Kentico.Community.Portal.Web.Integration.Tests` | `dotnet test` (MTP)          |
| E2E         | `test/kentico-community-portal-web-e2e-tests`         | Playwright Test (TypeScript) |

## Unit and integration

`global.json` sets the test runner to Microsoft.Testing.Platform (MTP), so
`dotnet test` takes `--project` (or `--solution`) instead of a positional path,
and `.runsettings` files are not used. Run from the repository root:

```powershell
# Unit
dotnet test --project .\test\Kentico.Community.Portal.Web.Tests

# Integration (needs the local database)
dotnet test --project .\test\Kentico.Community.Portal.Web.Integration.Tests

# Both
dotnet test
```

A running `Kentico.Community.Portal.Web` (e.g. the Aspire `web` resource) locks
its Debug output, so the build fails with `MSB3027`. Stop the app first, or add
`--no-build` to run already-built tests.

### Integration configuration

Integration tests connect to the database in `Tests.config`
(`CMSTestConnectionString`, the local development database by default). To use
a different database, create a `Tests.Local.config` next to it with the same
shape. It overrides `Tests.config` and is ignored by source control, so add any
new keys to the tracked `Tests.config`.

## E2E

This project uses [Playwright Test](https://playwright.dev/) with TypeScript for
end-to-end (E2E) tests from `test/kentico-community-portal-web-e2e-tests`.

Install the E2E dependencies and Playwright browser from
`test/kentico-community-portal-web-e2e-tests`:

```powershell
cd .\test\kentico-community-portal-web-e2e-tests
pnpm install
pnpm exec playwright install chromium
```

`playwright.config.ts` defines these Playwright projects:

| Project          | Specs                                       | Script             |
| ---------------- | ------------------------------------------- | ------------------ |
| `portal-tests`   | Live site / member specs                    | `pnpm test:portal` |
| `admin-setup`    | `tests/admin/auth.setup.ts` (admin sign-in) | (dependency)       |
| `admin-tests`    | `tests/admin/*.spec.ts`                     | `pnpm test:admin`  |
| `isolated-tests` | `*.isolated.spec.ts` (process-wide state)   | (none)             |

`isolated-tests` depends on `portal-tests` and `admin-tests`, so it runs after
them. Run from `test/kentico-community-portal-web-e2e-tests`:

```powershell
cd .\test\kentico-community-portal-web-e2e-tests

# Everything, including isolated-tests last
pnpm test

# One project
pnpm test:portal
pnpm test:admin

# isolated-tests alone, without running its dependencies first
pnpm exec playwright test --project=isolated-tests --no-deps

# One spec
pnpm exec playwright test --project=portal-tests tests/privacy/email-tracking.spec.ts
```

CI runs `portal-tests`, then `admin-tests`, then `isolated-tests` (with
`--no-deps`, since the other two already ran).

Local execution supports both of these modes:

1. Reuse an already running `Kentico.Community.Portal.Web` instance.
2. Let `test/kentico-community-portal-web-e2e-tests/playwright.config.ts` start
   the app automatically.

### Local timeouts

Local runs are slower than CI, and specs that wait for emails (registration,
password recovery) can hit the 30s test timeout. These usually aren't real failures:

- **Warm-up**: Playwright only waits for the home page to respond. Right after the
  app starts or restarts, the first specs hit cold pages. Load a few pages
  (`/login`, `/register`) or run one spec before a full run.
- **Debug build**: the local app is a Debug build behind the Vite dev server. CI
  runs a Release publish.
- **Parallel workers**: local runs are capped at 4 workers in `playwright.config.ts`;
  CI uses Playwright's default (half the CPU cores). Use fewer when timeouts appear:
  `pnpm exec playwright test --project=portal-tests --workers=2`.

If a spec passes when run alone, these are the likely cause.

### Local prerequisites and toolchains

For local E2E runs against the Development app/runtime, start the client dev
servers so proxied assets are available using these VS Code tasks:

- `pnpm: watch (Web)` (Vite)
- `pnpm: watch (Admin)` (webpack)

Or run `pnpm start` in each application directory with a `package.json`.

If you run tests against a published app (CI-style), these watch servers are not
required. In that flow, production client assets are built as part of the
build/publish process and the app runs from published output.

The default base URL is `https://localhost:45039`. Override it with
`PORTAL_BASE_URL` when needed. The Virtual Inbox MCP endpoint defaults to
`${PORTAL_BASE_URL}/mcp` and can be overridden with `PORTAL_MCP_URL`.

Administration E2E defaults and overrides:

- `ADMIN_BASE_URL` defaults to `${PORTAL_BASE_URL}/admin`.
- `ADMIN_DEFAULT_USERNAME` defaults to `administrator`.
- `ADMIN_DEFAULT_PASSWORD` defaults to `Pass@12345`.

Administration tests use a setup project that signs in once and stores browser
state in `playwright/.auth/admin.json`, then reuses that state in `admin-tests`.

For administration selector reliability, prefer role/name and label selectors
first. Use `data-testid` only as a fallback for built-in admin UI and keep those
selectors centralized in page-object helpers.

Email-dependent flows use the development Virtual Inbox MCP tools rather than
direct SQL reads from `CMS_Email`.

Automation emails (e.g. the `MemberWelcome` welcome email) arrive a few seconds
after the trigger (2-4s locally). Development and CI shorten
`CMSProcessContactActionsInterval`, `SchedulerOptions.SecondsInterval` and
`EmailQueueOptions.LoadInterval` for this (see `appsettings.Development.json` /
`appsettings.CI.json`). With production values it can take up to ~2 minutes.

Moderation-state setup uses the development/CI-only endpoint
`POST /testing/e2e/member-moderation`. This endpoint exists only to support
deterministic Playwright E2E setup and is not mapped in production.

Member Role setup (for native page security scenarios) uses the
development/CI-only endpoint `POST /testing/e2e/member-role`, which grants or
revokes one of the `SystemMemberRoles` for a member. Like the moderation
endpoint, it exists only to support deterministic Playwright E2E setup and is
not mapped in production.
