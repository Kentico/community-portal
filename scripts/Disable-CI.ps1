<#
.Synopsis
    Disables Xperience Continuous Integration (CI) via CLI
#>

Import-Module (Resolve-Path Utilities) `
    -Function Get-WebProjectPath, Invoke-ExpressionWithException, Write-Status `
    -Force

$projectPath = Get-WebProjectPath
$launchProfile = $Env:ASPNETCORE_ENVIRONMENT -eq "CI" ? "Portal.WebCI" : "Portal.Web"
$configuration = $Env:ASPNETCORE_ENVIRONMENT -eq "CI" ? "Release" : "Debug"

Write-Status "Disabling CI"
Write-Host "`n"

$command = "dotnet run " + `
    "--launch-profile $launchProfile " + `
    "-c $configuration " + `
    "--no-build " + `
    "--no-restore " + `
    "--project $projectPath " + `
    "-- " + `
    "--kxp-ci-disable " + `
    "--format json"

Invoke-ExpressionWithException $command

Write-Host "`n"
Write-Status "CI disabled"