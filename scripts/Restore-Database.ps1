<#
.EXAMPLE
    cd community-portal
    
    .\scripts\Restore-Database.ps1 `
        -ServerName "localhost\sqlexpress" `
        -DatabaseName "KenticoCommunity.Test.01" `
        -BacpacFile "KenticoCommunity-26.3.1-2023-06-26.bacpac" `
        -WorkspaceFolder $PWD
#>

param (
    [string] $WorkspaceFolder = "..",
    [string] $ServerName = "localhost",
    [string] $DatabaseName = "KenticoCommunity",
    [string] $BacpacFile = "KenticoCommunity.bacpac"
)

$bacpacPath = Join-Path $WorkspaceFolder "database" $BacpacFile
$connectionString = "Data Source=$ServerName;Initial Catalog=master;Integrated Security=True;Persist Security Info=False;Connect Timeout=10;Encrypt=False;Current Language=English;"

Write-Host "Installing packages"

Install-Module -Name dbatools
Import-Module dbatools
Set-DbatoolsConfig -Name Import.EncryptionMessageCheck -Value $false -PassThru | Register-DbatoolsConfig

Write-Host "Importing .bacpac"

Publish-DbaDacPackage `
    -ConnectionString $connectionString `
    -Type Bacpac `
    -Database $DatabaseName `
    -Path $bacpacPath

Write-Host "Database restore complete."