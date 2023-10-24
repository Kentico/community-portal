<#
.Synopsis
    Updates local database data and schema to the version of the project's referenced Xperience NuGet package
#>

param (
    [string] $WorkspaceFolder = ".."
)

Import-Module (Join-Path $WorkspaceFolder "scripts/Utilities.psm1")

$projectPath = Get-WebProjectPath $WorkspaceFolder
$launchProfile = $Env:ASPNETCORE_ENVIRONMENT -eq "CI" ? "Portal.WebCI" : "Portal.Web"
$configuration = $Env:ASPNETCORE_ENVIRONMENT -eq "CI" ? "Release" : "Debug"

Write-Host "Begin Project Update: $projectPath"

$command = "dotnet run " + `
    "--project $projectPath " + `
    "--launch-profile $launchProfile " + `
    "--no-build " + `
    "--no-restore " + `
    "-c $configuration " + `
    "--kxp-update"

Invoke-ExpressionWithException $command

Write-Host "Update Complete"