<#
.Synopsis
    Displays Xperience database status via CLI
    (Ready, MissingConnectionString, NotAvailable, UpdateRequired)
#>

Import-Module (Resolve-Path Utilities) `
    -Function Get-WebProjectPath, Invoke-ExpressionWithException, Write-Status `
    -Force

$projectPath = Get-WebProjectPath
$launchProfile = $Env:ASPNETCORE_ENVIRONMENT -eq "CI" ? "Portal.WebCI" : "Portal.Web"
$configuration = $Env:ASPNETCORE_ENVIRONMENT -eq "CI" ? "Release" : "Debug"

Write-Status "Getting database status"
Write-Host "`n"

$command = "dotnet run " + `
    "--launch-profile $launchProfile " + `
    "-c $configuration " + `
    "--no-build " + `
    "--no-restore " + `
    "--project $projectPath " + `
    "-- " + `
    "--kxp-db-status " + `
    "--format json"

Invoke-ExpressionWithException $command

Write-Host "`n"
Write-Status "Database status retrieved"
