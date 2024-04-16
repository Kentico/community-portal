<#
.Synopsis
    Updates generated code files in the repository for custom Form, Content, and Data Types
#>

param (
    [string]$WorkspaceFolder = "..",
    [string[]]$Types = ''
)

Import-Module (Join-Path $WorkspaceFolder "scripts/Utilities.psm1")

$projectPath = Get-WebProjectPath $WorkspaceFolder

function GeneratePageTypes {
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
}

function GenerateResusableTypes {
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
}

function GenerateResusableSchemas {
    $command = "dotnet run " + `
        "--project $projectPath " + `
        "--no-restore " + `
        "--no-build " + `
        "-- --kxp-codegen " + `
        "--skip-confirmation " + `
        "--type `"ReusableFieldSchemas`" " + `
        "--namespace `"Kentico.Community.Portal.Core.Content`"" + `
        "--location `"../Kentico.Community.Portal.Core/Content/Hub/`""

    Invoke-ExpressionWithException $command
}

function GenerateDataTypes {
    $command = "dotnet run " + `
        "--project $projectPath " + `
        "--no-restore " + `
        "--no-build " + `
        "-- --kxp-codegen " + `
        "--skip-confirmation " + `
        "--type `"Classes`" " + `
        "--namespace `"Kentico.Community.Portal.Core.Modules`"" + `
        "--location `"../Kentico.Community.Portal.Core/Modules/{name}`"" + `
        "--with-provider-class false"

    Invoke-ExpressionWithException $command
}

function GenerateForms {
    $command = "dotnet run " + `
        "--project $projectPath " + `
        "--no-restore " + `
        "--no-build " + `
        "-- --kxp-codegen " + `
        "--skip-confirmation " + `
        "--type `"Forms`" " + `
        "--namespace `"Kentico.Community.Portal.Core.Forms`"" + `
        "--location `"../Kentico.Community.Portal.Core/Forms/{name}`""

    Invoke-ExpressionWithException $command
}

Write-Host "Re-generating code for Project: $projectPath"

if ($Types -contains 'Pages') {
    GeneratePageTypes
}

if ($Types -contains 'Reusable') {
    GenerateResusableTypes
    GenerateResusableSchemas
}

if ($Types -contains 'DataTypes') {
    GenerateDataTypes
}

if ($Types -contains 'Forms') {
    GenerateForms
}

Write-Host "Code generation complete"

