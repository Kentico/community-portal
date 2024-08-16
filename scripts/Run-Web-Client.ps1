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

$command = "npm i"
Invoke-ExpressionWithException $command

$command = "npm start"
Invoke-ExpressionWithException $command
