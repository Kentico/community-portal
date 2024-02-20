# Required Software

## .NET Runtime

.NET 8.0 SDK or newer

- <https://dotnet.microsoft.com/en-us/download/dotnet/8.0>
- See `global.json` file for specific SDK requirements

## Node.js Runtime

- [Node.js](https://nodejs.org/en/download) 18.16.1 or newer
- [NVM for Windows](https://github.com/coreybutler/nvm-windows) to manage multiple installed versions of Node.js
- See `engines` in the solution's `package.json` files for specific version requirements

## PowerShell

- [PowerShell](https://learn.microsoft.com/en-us/powershell/scripting/overview?view=powershell-7.3) v7.3 or newer

## C# Editor

- VS Code
- Visual Studio
- Rider

## Database

- SQL Server 2019 or newer compatible database

  - <https://learn.microsoft.com/en-us/sql/linux/sql-server-linux-setup?view=sql-server-ver15>
  - <https://learn.microsoft.com/en-us/azure/azure-sql-edge/disconnected-deployment>

## SQL Editor

- MS SQL Server Management Studio
- Azure Data Studio

## Azure Storage

Azure storage is used locally for data storage and background job processing

- [Azurite](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite?tabs=visual-studio%2Cblob-storage)
- [Azure Storage Explorer](https://azure.microsoft.com/en-us/products/storage/storage-explorer/)

## Email SMTP Server

- [MailHog](https://github.com/mailhog/MailHog), [Mailpit](https://github.com/axllent/mailpit) or a similar local SMTP server for local email testing
