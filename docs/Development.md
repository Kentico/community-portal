# Development

> If you are planning to make changes and contribute updates to the application,
> create a branch. See: [Contributing](./Contributing.md) for more details.

This project is typically developed with 3 web servers running simultaneously
and separately.

1. Kestrel server for the Xperience by Kentico ASP.NET Core application
1. Webpack dev server for Xperience administration client customization code
1. Vite dev server for website channel client assets

Details on how to start these 3 servers can be found below.

## Start everything with .NET Aspire (optional)

Instead of starting each server separately, you can run the
[Aspire AppHost](./Aspire.md) to orchestrate the database, Azure Storage
emulator, both client dev servers, and the web application together, with a
dashboard for logs, traces, and metrics:

```powershell
aspire run
```

See [Local Development with .NET Aspire](./Aspire.md) for prerequisites and the
one-time database restore. The manual workflow below remains fully supported.

### Outer loop vs. inner loop

Aspire is **additive**, not a replacement for the per-server tasks below. Think
of it as two complementary loops:

- **Outer loop** — `aspire run` brings up the _whole system_ (SQL, Azurite, both
  client dev servers, and the web app) with one command and a telemetry
  dashboard. Best for onboarding, running the full stack, end-to-end checks, and
  giving agents a one-command environment.
- **Inner loop** — the individual VS Code tasks (`.NET: watch (Web)`,
  `pnpm: watch (Web)`, `pnpm: watch (Admin)`) iterate on _one piece_ fast,
  without spinning up everything. Best when you are focused on a single project.

By default `aspire run` runs the web app without file watching; rebuild it from
the dashboard's **Rebuild** command, or enable
[watch mode](./Aspire.md#hot-reload-watch-mode) for automatic rebuilds. Building,
formatting, testing, CI Store/Restore, database backups, and deployment packaging
stay outside Aspire — they remain plain VS Code tasks / scripts.

### Which task when

| I want to…                                       | Use                                                          |
| ------------------------------------------------ | ----------------------------------------------------------- |
| Run the full stack with one command + dashboard  | `aspire run` (or the `Aspire: Run` task)                    |
| Iterate quickly on the web app only              | `.NET: watch (Web)`                                         |
| Iterate on website channel client assets only    | `pnpm: watch (Web)` (Vite)                                  |
| Iterate on admin React customizations only       | `pnpm: watch (Admin)` (webpack)                             |
| Install all client dependencies                  | `pnpm: install (workspace)`                                 |
| Build / rebuild the solution                     | `.NET: build (Solution)` / `.NET: rebuild (Solution)`       |
| Run unit / E2E tests                             | `.NET: test (Solution)` / `pnpm: test (Playwright E2E)`     |
| Sync the database to/from the CI repository      | `Xperience: CI Store` / `Xperience: CI Restore`             |
| Back up the database to a `.bak`                 | `Xperience: Database Backup`                                |

## Start pnpm dev servers

1. Install and build the client dependencies for the website channel experience

   This uses Vite as a dev server.
   - Use the VS Code task `pnpm: watch (Web)`
   - (**alternative**) Run `pnpm install` and then `pnpm run dev` at the command
     line in the `.\src\Kentico.Community.Portal.Web` directory
   - (**alternative**) Run `Run-Web-Client.ps1` from the command line in the
     `.\scripts` directory

1. Install and build the client dependencies for the custom Xperience
   administration React components

   This uses webpack as a dev server.
   - Use the VS Code task `pnpm: watch (Admin)`
   - (**alternative**) Run `pnpm install` and then `pnpm start` at the command
     line in the `.\src\Kentico.Community.Portal.Admin\Client` directory
   - (**alternative**) Run `Run-Admin-Client.ps1` from the command line in the
     `.\scripts` directory

## Start Azurite

Enable Azurite for local Azure Storage access.

- Install the
  [VS Code extension](https://marketplace.visualstudio.com/items?itemName=Azurite.azurite)
  (installed with recommended extensions)
- Run Azurite from the VS Code command palette (`ctrl+shift+p`) with
  `Azurite: Start`
- (**alternative**) Use the
  [Azurite documentation](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite?tabs=visual-studio%2Cblob-storage#install-azurite)
  to install or start it with your IDE of choice
- (**alternative**) Use
  [the Azurite Docker image](https://github.com/Azure/Azurite?tab=readme-ov-file#dockerhub)
- (**alternative**) Disable Support Request Processing via your User Secrets
  configuration (see `SupportRequestProcessing` in `appsettings.json`). This
  will disable the code from executing that requires Azurite.

## Start ASP.NET Core application

Launch the ASP.NET Core application on localhost.

See: `README.md` or
`Kentico.Community.Portal.Web\Properties\launchSettings.json` for ports.

- Use the VS Code task `.NET: watch (Web)`
- (**alternative**) Use the VS Code `.NET Debug and Hot Reload` Launch
  Configuration
- (**alternative**) Start in VS using
  [Start without debugging](https://dailydotnettips.com/back-to-basic-difference-between-start-debugging-start-without-debugging-in-visual-studio/)
- (**alternative**) Navigate to the `Kentico.Community.Portal.Web` directory in
  a terminal and run `dotnet watch`

## Client Assets

### Administration

The Webpack dev server is used for
[Xperience Admin customizations](https://docs.kentico.com/x/zgSiCQ) in the
`Kentico.Community.Portal.Admin` project.

### Website channels

[Vite.js](https://vitejs.dev/) is used to serve and bundle client assets for the
Xperience website channel experiences generated by
`Kentico.Community.Portal.Web`.

The ASP.NET Core application proxies requests for static assets to the Vite dev
server using the [Vite.AspNetCore](https://github.com/Eptagone/Vite.AspNetCore)
integration.

Because client assets are served from the Vite dev server during development and
all generated production build client assets are ignored by source control none
of these assets will be visible on the file system when beginning development.

You will need to start the following servers to fully develop the application

For production builds, the Vite build process compiles the SCSS and JS in the
`~/Client` folder into the `~/wwwroot/dist` folder and generated CSS is trimmed
using PurgeCSS to match what is being used in the `.cshtml` templates.

## Xperience Upgrades

After
[applying a new hotfix or Refresh](https://docs.kentico.com/developers-and-admins/installation/update-xperience-by-kentico-projects#UpdateXperiencebyKenticoprojects-UpdatedevelopmentprojectswithContinuousIntegration),
create a backup of the database as a `.bak` file and add it to the `.\database`
folder.

The PowerShell settings you configured are used by the
`.\scripts\Backup-Database.ps1` script to perform the following steps.

1. Remove the license key from the database
1. Generate a `.bak` file
1. Restore the license key to the database
1. Find the generated `.bak` file on the file system
1. Copy it to the `.\database` folder
1. Create a `.zip` file of the backup

## Login

The local administrator account credentials are:

- Username: administrator
- Password: Pass@12345

You can create a local user account once you login as administrator.
