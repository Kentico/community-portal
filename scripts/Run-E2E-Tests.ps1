#!/usr/bin/env pwsh

<#
.SYNOPSIS
Runs the ASP.NET Core application and executes E2E tests against it.

.DESCRIPTION
Starts the published ASP.NET Core application as a background process,
waits for it to be ready, runs E2E tests, and captures all output for diagnostics.

.PARAMETER PublishPath
Path to the published application directory.

.PARAMETER StatusCheckUrl
URL to check for application health/readiness.

.PARAMETER TestProjectPath
Path to the E2E test project file (.csproj).

.PARAMETER TestSettings
Path to the test settings file.

.PARAMETER MaxWaitSeconds
Maximum time to wait for application startup (default: 30).
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$PublishPath,

    [Parameter(Mandatory = $true)]
    [string]$StatusCheckUrl,

    [Parameter(Mandatory = $true)]
    [string]$TestProjectPath,

    [Parameter(Mandatory = $true)]
    [string]$TestSettings,

    [Parameter(Mandatory = $true)]
    [string]$AppDllName,

    [Parameter(Mandatory = $false)]
    [int]$MaxWaitSeconds = 30
)

$ErrorActionPreference = "Stop"

# Output files for application logs
$stdoutLog = Join-Path $PublishPath "app-stdout.log"
$stderrLog = Join-Path $PublishPath "app-stderr.log"

# Clean up any existing log files
Remove-Item $stdoutLog -ErrorAction SilentlyContinue
Remove-Item $stderrLog -ErrorAction SilentlyContinue

Write-Host "Starting ASP.NET Core application..."
Write-Host "  Path: $PublishPath"
Write-Host "  DLL: $AppDllName"
Write-Host "  Status URL: $StatusCheckUrl"
Write-Host "  Logs: $stdoutLog, $stderrLog"

# Verify the DLL exists
$dllPath = Join-Path $PublishPath $AppDllName
if (-not (Test-Path $dllPath)) {
    Write-Host "Application DLL not found: $dllPath" -ForegroundColor Red
    Write-Output "❌ Application DLL not found"
    exit 1
}

# Start the application as a background process with output redirection
$appProcess = Start-Process `
    -FilePath "dotnet" `
    -ArgumentList $AppDllName `
    -WorkingDirectory $PublishPath `
    -RedirectStandardOutput $stdoutLog `
    -RedirectStandardError $stderrLog `
    -NoNewWindow `
    -PassThru

Write-Host "Application started with PID: $($appProcess.Id)"

$testsPassed = $false
$appStarted = $false

try {
    # Wait for application to be ready
    Write-Host ""
    Write-Host "Waiting for application to be ready (max ${MaxWaitSeconds}s)..."
    
    $attempts = 0
    $maxAttempts = $MaxWaitSeconds * 2  # Check every 0.5s
    
    while ($attempts -lt $maxAttempts) {
        # Check if process is still running
        if ($appProcess.HasExited) {
            Write-Host "Application process exited unexpectedly with code: $($appProcess.ExitCode)" -ForegroundColor Red
            
            Write-Host ""
            Write-Host "=== Application STDOUT ==="
            if (Test-Path $stdoutLog) {
                Get-Content $stdoutLog
            }
            else {
                Write-Host "(no output)"
            }
            
            Write-Host ""
            Write-Host "=== Application STDERR ==="
            if (Test-Path $stderrLog) {
                Get-Content $stderrLog
            }
            else {
                Write-Host "(no output)"
            }
            
            Write-Output "❌ Application failed to start"
            exit 1
        }

        try {
            $response = Invoke-WebRequest -Uri $StatusCheckUrl -Method Get -SkipCertificateCheck -TimeoutSec 2
            if ($response.StatusCode -eq 200) {
                Write-Host "✓ Application is ready (after $([math]::Round($attempts * 0.5, 1))s)"
                $appStarted = $true
                break
            }
        }
        catch {
            if ($attempts % 4 -eq 0) {
                # Log every 2 seconds
                Write-Host "  Waiting... ($([math]::Round($attempts * 0.5, 1))s elapsed)"
                Write-Output "  Waiting... ($([math]::Round($attempts * 0.5, 1))s elapsed)"
            }
        }
        
        Start-Sleep -Milliseconds 500
        $attempts++
    }

    if (-not $appStarted) {
        Write-Host "Application did not respond within ${MaxWaitSeconds}s" -ForegroundColor Red
        
        Write-Host ""
        Write-Host "=== Last 50 lines of STDOUT ==="
        if (Test-Path $stdoutLog) {
            Get-Content $stdoutLog -Tail 50
        }
        else {
            Write-Host "(no output)"
        }
        
        Write-Host ""
        Write-Host "=== Last 50 lines of STDERR ==="
        if (Test-Path $stderrLog) {
            Get-Content $stderrLog -Tail 50
        }
        else {
            Write-Host "(no output)"
        }
        
        Write-Output "❌ Application failed to start within timeout"
        exit 1
    }

    # Run E2E tests
    Write-Host ""
    Write-Host "Running E2E tests..."
    Write-Host "  Test Project: $TestProjectPath"
    Write-Host "  Settings: $TestSettings"
    Write-Host ""
    
    dotnet test --project $TestProjectPath --settings $TestSettings -c Release --no-build --no-ansi
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "E2E tests failed with exit code: $LASTEXITCODE" -ForegroundColor Red
        Write-Output "❌ E2E tests failed with exit code: $LASTEXITCODE"
        exit $LASTEXITCODE
    }
    
    Write-Host ""
    Write-Host "✓ E2E tests passed"
    Write-Output "✓ E2E tests passed"
    $testsPassed = $true
}
finally {
    # Always stop and clean up the application
    Write-Host ""
    Write-Host "Stopping application..."
    
    if (-not $appProcess.HasExited) {
        Stop-Process -Id $appProcess.Id -Force -ErrorAction SilentlyContinue
        
        # Wait a moment for graceful shutdown
        Start-Sleep -Seconds 2
        
        if (-not $appProcess.HasExited) {
            Write-Host "  Force killing process..."
            Stop-Process -Id $appProcess.Id -Force -ErrorAction SilentlyContinue
        }
    }
    
    Write-Host "✓ Application stopped"
    
    # Display application logs for diagnostics (even on success, but abbreviated)
    if ($testsPassed) {
        Write-Host ""
        Write-Host "=== Application Startup Logs (first 20 lines) ==="
        if (Test-Path $stdoutLog) {
            Get-Content $stdoutLog -Head 20
        }
    }
    else {
        Write-Host ""
        Write-Host "=== Full Application STDOUT ==="
        if (Test-Path $stdoutLog) {
            Get-Content $stdoutLog
        }
        else {
            Write-Host "(no output)"
        }
        
        Write-Host ""
        Write-Host "=== Full Application STDERR ==="
        if (Test-Path $stderrLog) {
            Get-Content $stderrLog
        }
        else {
            Write-Host "(no output)"
        }
    }
}

if (-not $testsPassed) {
    Write-Output "❌ E2E tests failed"
    exit 1
}

exit 0
