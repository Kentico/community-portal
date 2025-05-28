# Performs a deep clean of all .NET build artifacts
# Useful to resolve compilation errors related to outdated \bin or \obj data
# Authored by GitHub Copilot

#!/usr/bin/env pwsh

$foldersToRemove = @(
    "bin",
    "obj",
    "TestResults", # Common test output directory
    "node_modules", # NPM packages
    ".vs", # Visual Studio temporary files
    "BenchmarkDotNet.Artifacts" # Benchmark results
)

# Get the script's directory
$scriptPath = $PSScriptRoot
# Get the solution root (parent of scripts folder)
$solutionRoot = Split-Path $scriptPath -Parent

Write-Host "Cleaning build artifacts in: $solutionRoot"

foreach ($folder in $foldersToRemove) {
    Write-Host "Searching for $folder directories..."
    $items = Get-ChildItem -Path $solutionRoot -Filter $folder -Directory -Recurse
    
    foreach ($item in $items) {
        Write-Host "Removing $($item.FullName)"
        try {
            if (-Not (Test-Path -Path $item.FullName)) {
                continue
            }
            Remove-Item -Path $item.FullName -Recurse -Force
            Write-Host "Successfully removed $($item.FullName)" -ForegroundColor Green
        }
        catch {
            Write-Host "Failed to remove $($item.FullName): $($_.Exception.Message)" -ForegroundColor Red
        }
    }
}

Write-Host "`nBuild artifact cleanup complete!" -ForegroundColor Green
