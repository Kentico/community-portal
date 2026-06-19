# Local Development with .NET Aspire

[.NET Aspire](https://aspire.dev) can orchestrate the entire local development
stack with a single command. Instead of starting the database, Azure Storage
emulator, client dev servers, and the ASP.NET Core application separately (see
[Development](./Development.md)), the Aspire AppHost starts and supervises all of
them together and provides a dashboard with logs, traces, and metrics.

> Aspire is an **optional** convenience. The manual, server-by-server workflow
> documented in [Development](./Development.md) continues to work unchanged.

## What the AppHost orchestrates

The AppHost project is `src/Kentico.Community.Portal.AppHost`. It models these
resources:

| Resource        | Type                         | Notes                                                                                  |
| --------------- | ---------------------------- | -------------------------------------------------------------------------------------- |
| `sql`           | SQL Server 2022 container    | Persistent container + `kentico-community-sql-data` volume. Host port is Aspire-managed |
| `kentico-community` | Database                 | The `Kentico.Community` database on `sql`                                               |
| `storage`       | Azurite container            | Blob/queue/table on the standard ports `10000`/`10001`/`10002`                          |
| `web-client`    | Vite dev server (pnpm)       | Website channel client assets, HTTPS `5174`                                             |
| `admin-client`  | Webpack dev server (pnpm)    | Admin React customizations, HTTPS `3099`                                                |
| `web`           | ASP.NET Core project         | The Xperience app on `45039` (site), `45040` (admin), `45041` (http)                    |
| `generate-code` | Dev action (explicit-start)  | Regenerates Xperience content/object type code files. See [Developer actions](#on-demand-developer-actions) |
| `deployment-package` | Dev action (explicit-start) | Builds the Xperience SaaS deployment package (for testing CD `repository.config` changes) |
| `db-backup`     | Dev action (explicit-start)  | Backs up the database to a zipped `.bak` in `./database`                                |
| `data-cleaner`  | Dev action (explicit-start)  | Scrubs member data from a restored production backup before CI restore (**destructive**) |

> `sql` is the **managed default**. To use your own local SQL Server instead, see
> [Use your own SQL Server](#use-your-own-sql-server-instead-of-the-managed-container) —
> then `sql` is replaced by a lightweight `CMSConnectionString` reference.

The `web` project is wired with
[`Aspire.ServiceDefaults`](../src/Kentico.Community.Portal.ServiceDefaults), which
adds OpenTelemetry (traces/metrics/logs), health checks, and service discovery so
telemetry appears in the dashboard.

## Prerequisites

In addition to the [Required Software](./Required-Software.md):

- **Aspire CLI** — `dotnet tool install -g Aspire.Cli` (or see
  <https://aspire.dev>). Confirm with `aspire --version`.
- **Docker** running (for the `sql` and `storage` containers).
- **Trusted .NET dev certificate** — `dotnet dev-certs https --trust`.
- **pnpm workspace installed once** — run `pnpm install` at the repository root.
  The client dev servers reuse the shared workspace `node_modules`; the AppHost
  does **not** run a per-project `pnpm install`.

## Run

From the repository root:

```powershell
aspire run
```

This builds the AppHost, starts every resource, and prints the dashboard URL
(for example `https://localhost:17143`). Open it to watch resource health, logs,
traces, and metrics. Press `Ctrl+C` to stop.

The website is available at <https://localhost:45039> and the admin UI at
<https://localhost:45040>, exactly as with the manual workflow.

## Hot reload (watch mode)

By default `aspire run` launches the `web` project as a plain `dotnet run` — it
does **not** watch for file changes. After editing C#/Razor, apply the change by
running the **Rebuild** command on the `web` resource in the dashboard (it stops,
rebuilds, and restarts the project). This predictable, no-watch default is what
automated agents and CI rely on, so the committed project config leaves watch
**off**.

To make your own `aspire run` rebuild automatically on file changes (the
`dotnet watch` experience), enable Aspire's watch feature **for your machine**:

```powershell
aspire config set features.defaultWatchEnabled true --global
```

This is stored in your per-user Aspire settings (`--global`), so it is **not**
committed and only affects you. Turn it back off with:

```powershell
aspire config set features.defaultWatchEnabled false --global
```

For tight inner-loop work on just the web app, the `.NET: watch (Web)` VS Code
task still runs the project directly under `dotnet watch` without the AppHost.

## First run: restore the database

The `sql` resource starts with an **empty** `Kentico.Community` database. Populate
it once from the backup committed in `./database`:

1. In the dashboard, open the `sql` resource and run the **Restore database**
   command (the database icon). It extracts the latest `database/*.zip` backup and
   restores it as `Kentico.Community`. Progress is written to the `sql` resource
   console.
2. Start (or restart) the `web` resource from the dashboard once the restore
   completes.

The database lives on the persistent `kentico-community-sql-data` volume, so this
is a one-time step — it survives across `aspire run` sessions. To start over,
stop the AppHost and remove the volume:

```powershell
docker volume rm kentico-community-sql-data
```

> The committed backup has the **license key removed** (see
> [Development](./Development.md#xperience-upgrades)). After restoring, the site
> returns a license error until you add a valid license key to the database, the
> same as with a manual restore.

## On-demand developer actions

Several common dev operations are surfaced in the dashboard as **explicit-start**
resources, so they are discoverable instead of buried in `scripts/` and the VS Code
tasks. They never run on their own — start them on demand from the dashboard (each
also streams its output to its own resource console):

| Resource             | What it does                                                      | Backed by |
| -------------------- | ----------------------------------------------------------------- | --------- |
| `generate-code`      | Regenerates content/object type code files for the web app        | `scripts/Refresh-GeneratedCode.ps1` |
| `deployment-package` | Builds the Xperience SaaS deployment package                      | `scripts/Export-DeploymentPackage.ps1` |
| `db-backup`          | Backs up the database to a zipped `.bak` in `./database`          | `scripts/Backup-Database.ps1` |
| `data-cleaner`       | Replaces member data in a restored prod backup before CI restore  | `Kentico.Community.DataCleaner.App` |

Each action receives the resolved database connection string from the AppHost
(`ConnectionStrings__CMSConnectionString`), so it targets the **same database** as
the `web` app — the managed `sql` container, or your own server in
[bring-your-own mode](#use-your-own-sql-server-instead-of-the-managed-container).
When managed, the action also waits for `sql` to be healthy before it runs.

Notes:

- **PowerShell 7 (`pwsh`)** must be on your `PATH` for the three script-based
  actions.
- `generate-code` writes `.cs` files into `Kentico.Community.Portal.Core`, and
  `deployment-package` performs a publish. Both touch source/output, so running them
  while `web` is in [watch mode](#hot-reload-watch-mode) triggers a rebuild — prefer
  running them when `web` is stopped.
- `data-cleaner` is **destructive** (it deletes `CMS_Member` and rewrites CI
  repository content). Starting it from the dashboard runs it non-interactively
  (`--skip-confirmation`); only start it against a database you intend to scrub.
- `db-backup` writes the `.bak` using the paths in `scripts/Utilities` (see
  `settings.local.json`). Those defaults target your own SQL Server, so backup is
  most useful in bring-your-own mode; against the managed container you would need
  the backup path to map to the container's bind mount.

## Configuration & secrets

- When the **managed container** is used, the AppHost injects its connection
  string into the `web` project as `ConnectionStrings__CMSConnectionString`,
  **overriding** the `CMSConnectionString` in your
  [User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)
  for that run. When your **own server** is used (see below), the AppHost passes
  through that same connection string instead.
- Your User Secrets are **not** modified. Non-Aspire scripts (for example
  `scripts/Restore-CI.ps1`) and the manual workflow continue to use them.
- Azurite runs on the standard emulator ports, so the existing `CMSAzure*`
  development settings keep working unchanged.

## Use your own SQL Server instead of the managed container

The AppHost chooses the database mode **automatically**:

- **A `CMSConnectionString` already exists** — in the Web project's user secrets
  (the connection string the manual workflow already uses) or the AppHost's own
  configuration — Aspire references **that** existing SQL Server and creates **no**
  container. So if you already develop against your own local SQL Server,
  `aspire run` reuses it with no extra setup.
- **No connection string is configured** (CI/CD, fresh onboarding) — Aspire
  provisions the managed `sql` container + data volume + one-time restore:
  "Aspire manages everything".

When your own server is used:

- The dashboard shows a single `CMSConnectionString` connection resource instead
  of the `sql` container + `kentico-community` database.
- The `web` app connects straight to your server — there is no container, data
  volume, or "Restore database" step.

The AppHost reads the Web project's secrets through its `UserSecretsId`
(`3df470e6-54c4-41d8-b8f0-36955d9433d2`, kept in sync with
`Kentico.Community.Portal.Web.csproj`). To point the AppHost at a **different**
server without touching the Web app secrets, set it in the AppHost's own secrets:

```powershell
cd src/Kentico.Community.Portal.AppHost
dotnet user-secrets set "ConnectionStrings:CMSConnectionString" "Data Source=localhost,1433;Initial Catalog=Kentico.Community;User ID=sa;Password=<your password>;Encrypt=False;TrustServerCertificate=true"
```

> To exercise the managed container path on a machine that already has a
> `CMSConnectionString` (for example to reproduce onboarding), temporarily remove
> it from the Web project's user secrets.

## SQL Server port

The `sql` container's host port is assigned dynamically by Aspire so it never
collides with a standalone SQL Server container you may already run on `1433`.
Find the mapped port (and a copyable connection string) on the `sql` resource in
the dashboard if you want to connect with an external SQL editor.

## Scope: local development only (not CI/CD)

The AppHost is intentionally a **local inner-loop** tool. The GitHub Actions
workflows (`.github/workflows/ci.yml` and `deploy.yml`) deliberately **do not**
use Aspire, and there is no plan to change that. The reasoning:

- **`deploy.yml` has nothing for Aspire to orchestrate.** It builds the solution,
  bootstraps a database, generates an **Xperience SaaS deployment package**, and
  uploads it to the Xperience portal. No application server runs. Aspire's own
  deployment story (`aspire deploy` / `azd`) targets Azure Container Apps and
  Kubernetes and has no concept of the Xperience SaaS package API, so it cannot
  replace any step.
- **`ci.yml` is a fixed-topology, headless batch job.** Its SQL Server (the
  `mssqlsuite` action), Azurite, and app startup look superficially like what the
  AppHost does locally, but swapping in Aspire would be a **lateral move that adds
  complexity, not a simplification**:
  - The Xperience-specific database bootstrap — restore the committed `.bak`,
    inject the license key, and run `Restore-CI.ps1` — lives in scripts, not in
    the AppHost model. Aspire would not remove any of those steps.
  - The end-to-end tests use **Playwright (TypeScript)**, which is kept in
    TypeScript because the TS Playwright runner is better maintained than the
    .NET one. Playwright's own `webServer` already launches the published app and
    gates on a health URL, and the suite relies on **fixed ports** (`45039`).
    Routing this through an Aspire test host (`Aspire.Hosting.Testing`) with
    dynamic ports would mean *more* moving parts for an already-green pipeline.

The value Aspire adds — interactive orchestration, the dashboard, service
discovery, and fast onboarding — applies to the **inner loop**, which is exactly
what this AppHost provides. CI/CD stays on its purpose-built, ephemeral-runner
steps.

> If the team ever wants integration or E2E tests **authored in C#** that boot the
> whole system through the AppHost, `Aspire.Hosting.Testing` becomes attractive —
> but that is a test-architecture decision, not a CI simplification, and it does
> not change the TypeScript Playwright suite described above.

## Troubleshooting

- **`web` exits immediately on first run** — the database has not been restored
  yet. Run **Restore database** on `sql`, then start `web`.
- **`sql` is unhealthy with `Login failed for user 'sa'`** — a different SQL
  container is answering on the expected port, or a stale
  `kentico-community-sql-data` volume holds an old password. Stop other SQL
  containers, or `docker volume rm kentico-community-sql-data` and rerun.
- **Client assets 404** — ensure `pnpm install` was run once at the repository
  root so the shared workspace `node_modules` exists.
- **Duplicate `sql-*` containers / `web` can't connect after switching folders** —
  the managed `sql` container is persistent, and Aspire derives its identity
  partly from the absolute path of the `database` backup folder. Running
  `aspire run` from two different checkouts (for example a git worktree **and**
  the main clone) therefore creates two SQL containers that fight over the same
  `kentico-community-sql-data` volume; the second one exits with code 255 and the
  dashboard shows `sql` unhealthy. Run managed mode from a **single** working
  directory, or switch to [your own SQL Server](#use-your-own-sql-server-instead-of-the-managed-container).
  Remove a stale container with `docker rm <name>` (the data lives on the volume,
  not the container, so this is safe).
