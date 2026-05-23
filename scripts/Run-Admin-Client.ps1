<#
.Synopsis
    Runs the Admin client for local development
#>

Import-Module (Resolve-Path Utilities) `
    -Function Get-AdminClientProjectPath, Invoke-ExpressionWithException, Write-Status `
    -Force

Write-Status "Running admin client"
Write-Host "`n"

Set-Location (Get-AdminClientProjectPath)

$command = "pnpm install"
Invoke-ExpressionWithException $command

$command = "pnpm start"
Invoke-ExpressionWithException $command
