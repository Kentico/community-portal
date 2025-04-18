# Development

> If you are planning to make changes and contribute updates to the application,
> create a branch. See: [Contributing](./Contributing.md) for more details.

This project is typically developed with 3 web servers running simultaneously
and separately.

1. Kestrel server for the Xperience by Kentico ASP.NET Core application
1. Webpack dev server for Xperience administration client customization code
1. Vite dev server for website channel client assets

Details on how to start these 3 servers can be found below.

## Start npm dev servers

1. Install and build the client dependencies for the website channel experience

   This uses Vite as a dev server.

   - Use the VS Code task `npm: dev (Web)`
   - (**alternative**) Run `npm install` and then `npm run dev` at the command
     line in the `.\src\Kentico.Community.Portal.Web` directory
   - (**alternative**) Run `Run-Web-Client.ps1` from the command line in the
     `.\scripts` directory

1. Install and build the client dependencies for the custom Xperience
   administration React components

   This uses webpack as a dev server.

   - Use the VS Code task `npm: dev (Admin)`
   - (**alternative**) Run `npm install` and then `npm run dev` at the command
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
