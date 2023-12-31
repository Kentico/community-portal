<#
.Synopsis
    Updates generated code files in the repository for custom Form, Content, and Data Types
#>

param (
    [string]$WorkspaceFolder = ".."
)

Import-Module (Join-Path $WorkspaceFolder "scripts/Utilities.psm1")

$projectPath = Get-WebProjectPath $WorkspaceFolder

Write-Host "Re-generating code for Project: $projectPath"

$command = "dotnet run " + `
    "--project $projectPath " + `
    "--no-restore " + `
    "--no-build " + `
    "-- --kxp-codegen " + `
    "--skip-confirmation " + `
    "--type `"PageContentTypes`" " + `
    "--namespace `"Kentico.Community.Portal.Core.Content`"" + `
    "--location `"../Kentico.Community.Portal.Core/Content/Pages/`""

Invoke-ExpressionWithException $command

$command = "dotnet run " + `
    "--project $projectPath " + `
    "--no-restore " + `
    "--no-build " + `
    "-- --kxp-codegen " + `
    "--skip-confirmation " + `
    "--type `"ReusableContentTypes`" " + `
    "--namespace `"Kentico.Community.Portal.Core.Content`"" + `
    "--location `"../Kentico.Community.Portal.Core/Content/Hub/`""

Invoke-ExpressionWithException $command

$command = "dotnet run " + `
    "--project $projectPath " + `
    "--no-restore " + `
    "--no-build " + `
    "-- --kxp-codegen " + `
    "--skip-confirmation " + `
    "--type `"Classes`" " + `
    "--namespace `"Kentico.Community.Portal.Core.Modules`"" + `
    "--location `"../Kentico.Community.Portal.Core/Modules/{name}`""

Invoke-ExpressionWithException $command

Write-Host "Code generation complete"

