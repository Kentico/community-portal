# Required Software

## .NET Runtime

.NET 10 SDK required; projects target .NET 10

- <https://dotnet.microsoft.com/en-us/download/dotnet/10.0>
  - Target framework: .NET 10 (`net10.0`)
  - See `global.json` for the exact SDK version requirement

## Node.js Runtime

- [Node.js](https://nodejs.org/en/download/package-manager) 24.x (24.22.1+; LTS, security fixes)
- [pnpm](https://pnpm.io/installation), standalone install (not Corepack). Any
  recent version works: pnpm switches to the version pinned in `packageManager`
  automatically.
- [NVM for Windows](https://github.com/coreybutler/nvm-windows) to manage
  multiple installed versions of Node.js (recommended)
  - See `engines` in the root `package.json` for the exact version range

## PowerShell

- [PowerShell](https://learn.microsoft.com/en-us/powershell/scripting/overview?view=powershell-7.3)
  v7.3 (cross-platform)
  - Note: You **cannot** use
    [Windows PowerShell](https://learn.microsoft.com/en-us/powershell/scripting/what-is-windows-powershell)
    with this project. Windows PowerShell is an older, Windows-only, version of
    PowerShell. This project requires the modern, cross-platform version of
    PowerShell which is v7.0+.

## .NET Aspire (optional)

[.NET Aspire](https://aspire.dev) can orchestrate the full local development
stack (database, client dev servers, and the web app) with a single
`aspire run` command. See
[Local Development with .NET Aspire](./Aspire.md).

- [Aspire CLI](https://aspire.dev/get-started/install-cli/) 17
  - Requires [Docker](https://www.docker.com/) running for the SQL Server
    container

## C# Editor

- [VS Code](https://code.visualstudio.com) (cross-platform, preferred)
- [Cursor](https://cursor.com/) (cross-platform)
- (**alternative**) Visual Studio (Windows only)
- (**alternative**) Rider (cross-platform)

## Database

- SQL Server 2022 or newer compatible database
  - [SQL Server 2022 on Docker](https://learn.microsoft.com/en-us/sql/linux/sql-server-linux-setup?view=sql-server-ver15)
    (cross-platform, preferred)
    - <https://community.kentico.com/blog/developing-with-xperience-by-kentico-on-macos>
    - <https://www.milanlund.com/knowledge-base/setting-up-a-solution-for-integrating-custom-code-in-xperience-by-kentico-on-macos>
    - <https://konabos.com/blog/set-up-xperience-by-kentico-on-docker-in-minutes>
  - [SQL Server Express](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)
    (Windows only)

## SQL Editor

- VS Code with
  [SQL Server extension](https://marketplace.visualstudio.com/items/?itemName=ms-mssql.mssql)
  (preferred, cross-platform)
- (**alternative**)
  [SQL Server Management Studio](https://learn.microsoft.com/en-us/ssms/download-sql-server-management-studio-ssms)
  (Windows only)

## Email SMTP Server

- [MailHog](https://github.com/mailhog/MailHog),
  [Mailpit](https://github.com/axllent/mailpit) or a similar local SMTP server
  for local email testing
