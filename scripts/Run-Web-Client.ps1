<#
.Synopsis
    Runs the Web client for local development
#>


Import-Module (Resolve-Path Utilities) `
    -Function Get-WebProjectPath, Invoke-ExpressionWithException, Write-Status `
    -Force

Write-Status "Running web client"
Write-Host "`n"

Set-Location (Get-WebProjectPath)

$command = "pnpm install"
Invoke-ExpressionWithException $command

$command = "pnpm start"
Invoke-ExpressionWithException $command
