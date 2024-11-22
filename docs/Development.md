# Development

To ensure schema and data updates to the application are synchronized with the
code, follow the guidance in the Xperience docs for
[Continuous Integration](https://docs.kentico.com/x/YAaiCQ).

> If you are planning to make changes and contribute updates to the application,
> create a branch. See: [Contributing](./Contributing.md) for more details.

## Restore Xperience CI

Run the CI Restore PowerShell script to populate the database with changes from
the CI Repository

- Use the VS Code task `Xperience: CI Restore`
- (**alternative**) Use the script directly

  1. `cd scripts`
  1. `.\Restore-CI.ps1`

## Start npm Dev Servers

1. Install and build the client dependencies

   - Use the VS Code task `npm: dev (Web)`
   - (**alternative**) Run `npm install` and then `npm run dev` at the command
     line in the `.\src\Kentico.Community.Portal.Web` directory
   - (**alternative**) Run `Run-Web-Client.ps1` from the command line in the
     `.\scripts` directory

   - Use the VS Code task `npm: dev (Admin)`
   - (**alternative**) Run `npm install` and then `npm run dev` at the command
     line in the `.\src\Kentico.Community.Portal.Admin\Client` directory
   - (**alternative**) Run `Run-Admin-Client.ps1` from the command line in the
     `.\scripts` directory

## Start Azurite

1. Enable Azurite for local Azure Storage access

   - Install the
     [VS Code extension](https://marketplace.visualstudio.com/items?itemName=Azurite.azurite)
     (installed with recommended extensions)
   - Run Azurite from the VS Code command palette (`ctrl+shift+p`) with
     `Azurite: Start`
   - (**alternative**) Use the
     [Azurite documentation](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite?tabs=visual-studio%2Cblob-storage#install-azurite)
     to install or start it with your IDE of choice
   - (**alternative**) Disable Support Request Processing via your User Secrets
     configuration (see `SupportRequestProcessing` in `appsettings.json`). This
     will disable the code from executing that requires Azurite

## Start ASP.NET Core application

1. Launch server. App should now be running on localhost (see: `README.md` for
   ports)

   - Use the VS Code task `.NET: watch (Web)`
   - (**alternative**) Use the VS Code `.NET Debug and Hot Reload` Launch
     Configuration
   - (**alternative**) Start in VS

## Client Assets

This project uses Webpack for
[Xperience Admin customizations](https://docs.kentico.com/x/zgSiCQ) in the
`Kentico.Community.Portal.Admin` project.

It uses [Vite.js](https://vitejs.dev/) to serve and bundle client assets for the
Xperience application `Kentico.Community.Portal.Web`. The ASP.NET Core
application proxies requests for static assets to the Vite dev server using the
[Vite.AspNetCore](https://github.com/Eptagone/Vite.AspNetCore) integration.

All generated production build client assets are ignored by source control and
in a development environment client assets are served from the Vite dev server
during development, so none of these assets will be visible on the file system
when beginning development.

You will need to start the following servers to fully develop the application

- ASP.NET Core application - `.NET: watch (Web)` VS Code task
- Vite dev server - `npm: dev (Web)` VS Code task
  - (**alternative**) Run `Run-Web-Client.ps1` from the command line in the
    `.\scripts` directory
- Webpack dev server - `npm: dev (Admin)` VS Code task
  - (**alternative**) Run `Run-Admin-Client.ps1` from the command line in the
    `.\scripts` directory

The Vite build process compiles the SCSS and JS in the `~/Client` folder into
the `~/wwwroot/dist` folder during production builds. The generated CSS is
trimmed using PurgeCSS to match what is being used in the `.cshtml` templates.

## Xperience Upgrades

After
[applying a new hotfix or Refresh](https://docs.kentico.com/developers-and-admins/installation/update-xperience-by-kentico-projects#UpdateXperiencebyKenticoprojects-UpdatedevelopmentprojectswithContinuousIntegration),
create a backup of the database - either a `.bacpac` or `.bak` file and add it
to the `.\database` folder.

Then, add its file name to the first line of `backups.txt` to make this new
backup the starting point for any CI tests. This will also be the backup that
developers new to the project will use when setting up their environment.

Finally, create a `.zip` of the backup file, in the `.\database` folder, with
the same name as the backup, appended with `.zip`.

## Login

The local administrator account credentials are:

- Username: administrator
- Password: Pass@12345

You can create a local user account once you login as administrator.
