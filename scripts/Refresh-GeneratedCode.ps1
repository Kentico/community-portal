<#
.Synopsis
    Updates generated code files in the repository for custom Form, Content, and Data Types
#>

param (
    [string[]]$Types = $null
)

Import-Module (Resolve-Path Utilities) `
    -Function `
    Get-WebProjectPath, `
    Get-CoreProjectPath, `
    Invoke-ExpressionWithException, `
    Get-ScriptConfig, `
    Write-Status `
    -Force

$webProjectPath = Get-WebProjectPath
$coreProjectPath = Get-CoreProjectPath

$scriptConfig = Get-ScriptConfig

$Types = if ($null -eq $Types) { $scriptConfig.CodeGenerationTypes } else { $Types }

function Get-AppendedCorePath {
    param(
        [string]$relativePath
    )

    return Join-Path $coreProjectPath $relativePath
}

function GeneratePageTypes {
    $command = "dotnet run " + `
        "--project $webProjectPath " + `
        "--no-restore " + `
        "--no-build " + `
        "-- --kxp-codegen " + `
        "--skip-confirmation " + `
        "--type `"PageContentTypes`" " + `
        "--namespace `"Kentico.Community.Portal.Core.Content`"" + `
        "--location `"$(Get-AppendedCorePath "Content/Pages/")`""

    Invoke-ExpressionWithException $command
}

function GenerateResusableTypes {
    $command = "dotnet run " + `
        "--project $webProjectPath " + `
        "--no-restore " + `
        "--no-build " + `
        "-- --kxp-codegen " + `
        "--skip-confirmation " + `
        "--type `"ReusableContentTypes`" " + `
        "--namespace `"Kentico.Community.Portal.Core.Content`"" + `
        "--location `"$(Get-AppendedCorePath "Content/Hub/")`""

    Invoke-ExpressionWithException $command
}

function GenerateResusableSchemas {
    $command = "dotnet run " + `
        "--project $webProjectPath " + `
        "--no-restore " + `
        "--no-build " + `
        "-- --kxp-codegen " + `
        "--skip-confirmation " + `
        "--type `"ReusableFieldSchemas`" " + `
        "--namespace `"Kentico.Community.Portal.Core.Content`"" + `
        "--location `"$(Get-AppendedCorePath "Content/Hub/")`""

    Invoke-ExpressionWithException $command
}

function GenerateEmailTypes {
    $command = "dotnet run " + `
        "--project $webProjectPath " + `
        "--no-restore " + `
        "--no-build " + `
        "-- --kxp-codegen " + `
        "--skip-confirmation " + `
        "--type `"EmailContentTypes`" " + `
        "--namespace `"Kentico.Community.Portal.Core.Emails`"" + `
        "--location `"$(Get-AppendedCorePath "Emails/")`""

    Invoke-ExpressionWithException $command
}

function GenerateDataTypes {
    $command = "dotnet run " + `
        "--project $webProjectPath " + `
        "--no-restore " + `
        "--no-build " + `
        "-- --kxp-codegen " + `
        "--skip-confirmation " + `
        "--type `"Classes`" " + `
        "--namespace `"Kentico.Community.Portal.Core.Modules`"" + `
        "--location `"$(Get-AppendedCorePath "Modules/{name}")`"" + `
        # "--include `"KenticoCommunity.MemberBadge`"" + ` # helpful to only regenerate a single type
        "--with-provider-class false"

    Invoke-ExpressionWithException $command
}

function GenerateForms {
    $command = "dotnet run " + `
        "--project $webProjectPath " + `
        "--no-restore " + `
        "--no-build " + `
        "-- --kxp-codegen " + `
        "--skip-confirmation " + `
        "--type `"Forms`" " + `
        "--namespace `"Kentico.Community.Portal.Core.Forms`"" + `
        "--location `"$(Get-AppendedCorePath "Forms/{name}")`""

    Invoke-ExpressionWithException $command
}

Write-Status "Re-generating code for Project: $webProjectPath"
Write-Host "`n"

if ($Types -contains 'PageContentTypes') {
    GeneratePageTypes
}

if ($Types -contains 'ReusableContentTypes') {
    GenerateResusableTypes
}

if ($Types -contains 'Classes') {
    GenerateDataTypes
}

if ($Types -contains 'EmailContentTypes') {
    GenerateEmailTypes
}

if ($Types -contains 'ReusableFieldSchemas') {
    GenerateResusableSchemas
}

if ($Types -contains 'Forms') {
    GenerateForms
}

Write-Host "`n"
Write-Status "Code generation complete"

