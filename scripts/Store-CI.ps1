<#
.Synopsis
    Updates the CI repository will all valid objects from the local database
#>

Import-Module (Resolve-Path Utilities) `
    -Function Get-WebProjectPath, Invoke-ExpressionWithException, Write-Status `
    -Force

Write-Status "Storing CI files"
Write-Host "`n"

$command = "dotnet run " + `
    "--project $(Get-WebProjectPath) " + `
    "--kxp-ci-store"

Invoke-ExpressionWithException $command

Write-Host "`n"
Write-Status 'CI files stored'