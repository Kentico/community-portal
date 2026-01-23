#!/usr/bin/env pwsh

<#
.SYNOPSIS
Runs unit tests and writes results to GitHub step summary.

.PARAMETER ProjectPath
Path to the test project file.

.PARAMETER Configuration
Build configuration (default: Release).
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectPath,

    [Parameter(Mandatory = $false)]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

Write-Host "Running unit tests..."

try {
    # Capture test output
    $output = dotnet test `
        --project $ProjectPath `
        -c $Configuration `
        --no-build `
        --no-ansi 2>&1 | Out-String
    
    # Write to both console and output
    Write-Host $output
    
    if ($LASTEXITCODE -eq 0) {
        Write-Output $output
        Write-Output "✓ Unit tests passed"
        exit 0
    }
    else {
        Write-Output $output
        Write-Output "❌ Unit tests failed with exit code $LASTEXITCODE"
        exit $LASTEXITCODE
    }
}
catch {
    Write-Host "Unit tests encountered an error: $_" -ForegroundColor Red
    Write-Output "❌ Unit tests encountered an error: $_"
    exit 1
}
