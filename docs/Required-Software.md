# Required Software

## .NET Runtime

.NET 10.0 SDK (version `10.0.100`) required; projects target .NET 9.0

- <https://dotnet.microsoft.com/en-us/download/dotnet/10.0>
  - Target framework: .NET 9.0 (`net9.0`)
  - See `global.json` for the exact SDK version requirement

## Node.js Runtime

- [Node.js](https://nodejs.org/en/download/package-manager) 24.11.1 or newer
- [NVM for Windows](https://github.com/coreybutler/nvm-windows) to manage
  multiple installed versions of Node.js (recommended)
  - See `engines` in the solution's `package.json` files for specific version
    requirements

## PowerShell

- [PowerShell](https://learn.microsoft.com/en-us/powershell/scripting/overview?view=powershell-7.3)
  v7.3 (cross-platform)
  - Note: You **cannot** use
    [Windows PowerShell](https://learn.microsoft.com/en-us/powershell/scripting/what-is-windows-powershell)
    with this project. Windows PowerShell is an older, Windows-only, version of
    PowerShell. This project requires the modern, cross-platform version of
    PowerShell which is v7.0+.

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

## Azure Storage

Azure storage is used locally for data storage and background job processing.

- [Azurite](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite?tabs=visual-studio%2Cblob-storage)
  local install
- (**alternative**)
  [Azurite on Docker](https://github.com/Azure/Azurite?tab=readme-ov-file#dockerhub)
- [Azure Storage Explorer](https://azure.microsoft.com/en-us/products/storage/storage-explorer/)

## Email SMTP Server

- [MailHog](https://github.com/mailhog/MailHog),
  [Mailpit](https://github.com/axllent/mailpit) or a similar local SMTP server
  for local email testing
