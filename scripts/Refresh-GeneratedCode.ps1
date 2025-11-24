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

$Types = if ($null -eq $Types) { $scriptConfig.CodeGenerationTypes.PSObject.Properties.Name } else { $Types }

function Get-AppendedCorePath {
    param(
        [string]$relativePath
    )

    return Join-Path $coreProjectPath $relativePath
}

function Get-IncludeExcludeArgs {
    param(
        [string]$typeName
    )
    
    $arguments = ""
    $typeConfig = $scriptConfig.CodeGenerationTypes.$typeName
    
    if ($typeConfig.Include -and $typeConfig.Include.Trim() -ne "") {
        $arguments += "--include `"$($typeConfig.Include)`" "
    }
    
    if ($typeConfig.Exclude -and $typeConfig.Exclude.Trim() -ne "") {
        $arguments += "--exclude `"$($typeConfig.Exclude)`" "
    }
    
    return $arguments.Trim()
}

function GeneratePageTypes {
    $includeExcludeArgs = Get-IncludeExcludeArgs "PageContentTypes"
    
    $command = "dotnet run " + `
        "--project $webProjectPath " + `
        "--no-restore " + `
        "--no-build " + `
        "-- --kxp-codegen " + `
        "--skip-confirmation " + `
        "--type `"PageContentTypes`" " + `
        "--namespace `"Kentico.Community.Portal.Core.Content`" " + `
        "--location `"$(Get-AppendedCorePath "Content/Pages/")`""
    
    if ($includeExcludeArgs) {
        $command += " " + $includeExcludeArgs
    }

    Invoke-ExpressionWithException $command
}

function GenerateResusableTypes {
    $includeExcludeArgs = Get-IncludeExcludeArgs "ReusableContentTypes"
    
    $command = "dotnet run " + `
        "--project $webProjectPath " + `
        "--no-restore " + `
        "--no-build " + `
        "-- --kxp-codegen " + `
        "--skip-confirmation " + `
        "--type `"ReusableContentTypes`" " + `
        "--namespace `"Kentico.Community.Portal.Core.Content`" " + `
        "--location `"$(Get-AppendedCorePath "Content/Hub/")`""
    
    if ($includeExcludeArgs) {
        $command += " " + $includeExcludeArgs
    }

    Invoke-ExpressionWithException $command
}

function GenerateResusableSchemas {
    $includeExcludeArgs = Get-IncludeExcludeArgs "ReusableFieldSchemas"
    
    $command = "dotnet run " + `
        "--project $webProjectPath " + `
        "--no-restore " + `
        "--no-build " + `
        "-- --kxp-codegen " + `
        "--skip-confirmation " + `
        "--type `"ReusableFieldSchemas`" " + `
        "--namespace `"Kentico.Community.Portal.Core.Content`" " + `
        "--location `"$(Get-AppendedCorePath "Content/Hub/")`""
    
    if ($includeExcludeArgs) {
        $command += " " + $includeExcludeArgs
    }

    Invoke-ExpressionWithException $command
}

function GenerateEmailTypes {
    $includeExcludeArgs = Get-IncludeExcludeArgs "EmailContentTypes"
    
    $command = "dotnet run " + `
        "--project $webProjectPath " + `
        "--no-restore " + `
        "--no-build " + `
        "-- --kxp-codegen " + `
        "--skip-confirmation " + `
        "--type `"EmailContentTypes`" " + `
        "--namespace `"Kentico.Community.Portal.Core.Emails`" " + `
        "--location `"$(Get-AppendedCorePath "Emails/")`""
    
    if ($includeExcludeArgs) {
        $command += " " + $includeExcludeArgs
    }

    Invoke-ExpressionWithException $command
}

function GenerateDataTypes {
    $includeExcludeArgs = Get-IncludeExcludeArgs "Classes"
    
    $command = "dotnet run " + `
        "--project $webProjectPath " + `
        "--no-restore " + `
        "--no-build " + `
        "-- --kxp-codegen " + `
        "--skip-confirmation " + `
        "--type `"Classes`" " + `
        "--namespace `"Kentico.Community.Portal.Core.Modules`" " + `
        "--location `"$(Get-AppendedCorePath "Modules/{name}")`" " + `
        "--with-provider-class false"
    
    if ($includeExcludeArgs) {
        $command += " " + $includeExcludeArgs
    }

    Invoke-ExpressionWithException $command
}

function GenerateForms {
    $includeExcludeArgs = Get-IncludeExcludeArgs "Forms"
    
    $command = "dotnet run " + `
        "--project $webProjectPath " + `
        "--no-restore " + `
        "--no-build " + `
        "-- --kxp-codegen " + `
        "--skip-confirmation " + `
        "--type `"Forms`" " + `
        "--namespace `"Kentico.Community.Portal.Core.Forms`" " + `
        "--location `"$(Get-AppendedCorePath "Forms/{name}")`""
    
    if ($includeExcludeArgs) {
        $command += " " + $includeExcludeArgs
    }

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