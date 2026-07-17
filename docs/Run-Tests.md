# Run Tests

There are multiple test lanes in this repository.

Unit and integration tests still run through `dotnet test` with the
`*.runsettings` files in the `test` folder:

```powershell
dotnet test -s .\test\basic.runsettings
```

| Settings File       | Purpose                                             |
| ------------------- | --------------------------------------------------- |
| `basic.runsettings` | Runs only unit and integration tests, not E2E tests |

## Integration

To run tests for your environment you can create `Tests.Local.config` files in
the test projects that are copies of the existing files, but with your own
settings.

These files are ignored by source control, so any new setting keys/entries
should be added to the tracked `Tests.Local.config` files.

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

Run the suite with:

```powershell
cd .\test\kentico-community-portal-web-e2e-tests
pnpm test
```

Run only the existing live-site/member lane:

```powershell
cd .\test\kentico-community-portal-web-e2e-tests
pnpm test:portal
```

Run only the administration lane:

```powershell
cd .\test\kentico-community-portal-web-e2e-tests
pnpm test:admin
```

Local execution supports both of these modes:

1. Reuse an already running `Kentico.Community.Portal.Web` instance.
2. Let `test/kentico-community-portal-web-e2e-tests/playwright.config.ts` start
   the app automatically.

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

Moderation-state setup uses the development/CI-only endpoint
`POST /testing/e2e/member-moderation`. This endpoint exists only to support
deterministic Playwright E2E setup and is not mapped in production.
