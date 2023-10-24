<#
.Synopsis
    Downloads the Playwright browsers
    See: https://playwright.dev/dotnet/docs/browsers#installing-google-chrome--microsoft-edge
#>

param (
    [string] $WorkspaceFolder = ".."
)

Import-Module (Join-Path $WorkspaceFolder "scripts/Utilities.psm1")

$scriptPath = Join-Path $WorkspaceFolder "test/Kentico.Community.Portal.Web.E2E.Tests/bin/Debug/net6.0/playwright.ps1"

if (-not (Test-Path $scriptPath)) {
    $errorMessage = "The file '$scriptPath' does not exist. Please run a Debug build of the E2E project before running this script."
    throw $errorMessage
}

Write-Host "Downloading Playwright browsers"

pwsh -File $scriptPath install

Write-Host 'Browsers installed'