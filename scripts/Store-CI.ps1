<#
.Synopsis
    Updates the CI repository will all valid objects from the local database
#>

param (
    [string] $WorkspaceFolder = ".."
)

Import-Module (Join-Path $WorkspaceFolder "scripts/Utilities.psm1")

$projectPath = Get-WebProjectPath $WorkspaceFolder

Write-Host "Storing CI files for Project: $projectPath"

$command = "dotnet run " + `
    "--project $projectPath " + `
    "--kxp-ci-store"

Invoke-ExpressionWithException $command

Write-Host 'CI files stored'