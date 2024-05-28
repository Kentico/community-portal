<#
.Synopsis
    Downloads the Playwright browsers
    See: https://playwright.dev/dotnet/docs/browsers#installing-google-chrome--microsoft-edge
#>

Import-Module (Resolve-Path Utilities) `
    -Function Get-ScriptConfig, Invoke-ExpressionWithException, Write-Status `
    -Force

$scriptPath = Join-Path $(Get-ScriptConfig).WorkspaceFolder "test/Kentico.Community.Portal.Web.E2E.Tests/bin/Debug/net8.0/playwright.ps1"

if (-not (Test-Path $scriptPath)) {
    $errorMessage = "The file '$scriptPath' does not exist. Please run a Debug build of the E2E project before running this script."
    throw $errorMessage
}

Write-Status "Downloading Playwright browsers"

pwsh -File $scriptPath install

Write-Status 'Browsers installed'