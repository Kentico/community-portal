<#
.Synopsis
    Updates local database data and schema to the version of the project's referenced Xperience NuGet package.
    Optional -AgentMode switch skips interactive confirmations by appending --skip-confirmation.

.DESCRIPTION
    Executes the Xperience update via 'dotnet run' passing '-- --kxp-update'. When -AgentMode is supplied,
    the script adds '--skip-confirmation' (per Xperience docs) to bypass the database backup prompt for automation.
    CI disable/enable and CI Store operations are orchestrated externally.

.PARAMETER AgentMode
    When specified, disables confirmation prompts during the Xperience update (adds --skip-confirmation).

.EXAMPLE
    ./Update-Xperience.ps1
    Performs standard update with confirmation prompt.

.EXAMPLE
    ./Update-Xperience.ps1 -AgentMode
    Runs update without confirmation prompt (for CI/CD or scripted automation).
#>

param(
    [switch] $AgentMode
)

Import-Module (Resolve-Path Utilities) `
    -Function Get-WebProjectPath, `
    Invoke-ExpressionWithException, `
    Write-Status `
    -Force

$projectPath = Get-WebProjectPath
$launchProfile = $Env:ASPNETCORE_ENVIRONMENT -eq "CI" ? "Portal.WebCI" : "Portal.Web"
$configuration = $Env:ASPNETCORE_ENVIRONMENT -eq "CI" ? "Release" : "Debug"

Write-Status "Begin Xperience Update"
Write-Host "`n"

$command = "dotnet run " + `
    "--project $projectPath " + `
    "--launch-profile $launchProfile " + `
    "-c $configuration " + `
    "-- " + `
    "--kxp-update" + `
($AgentMode.IsPresent ? " --skip-confirmation" : "")

if ($AgentMode) {
    Write-Status "AgentMode enabled: skipping confirmations"
}

Invoke-ExpressionWithException $command

Write-Host "`n"
Write-Status "Update Complete"
