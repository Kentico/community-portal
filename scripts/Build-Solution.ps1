<#
.Synopsis
    Builds the entire .NET solution
#>

Import-Module (Resolve-Path Utilities) `
    -Function Get-SolutionPath, Invoke-ExpressionWithException, Write-Status `
    -Force

Write-Status "Building solution"
Write-Host "`n"

$command = "dotnet build $(Get-SolutionPath)"

Invoke-ExpressionWithException $command

Write-Host "`n"
Write-Status "Build complete"