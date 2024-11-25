<#
.Synopsis
    Updates the local database with all the objects in the CI repository.
    You can provide the $LicenseKey or the $LicenseFilePath
#>
[CmdletBinding()]
param (
    [Parameter()]
    [string]$LicenseKey = "",
    [Parameter()]
    [string]$LicenseFilePath = ""
)

Import-Module (Resolve-Path Utilities) `
    -Function Get-SolutionFolder, `
    Invoke-ExpressionWithException, `
    Invoke-SQLQuery, `
    Write-Status, `
    Write-Notification, `
    Write-Error `
    -Force

if (![string]::IsNullOrWhiteSpace($LicenseFilePath)) {
    $LicenseKey = Get-Content -Path $LicenseFilePath -Raw
}

if ([string]::IsNullOrWhiteSpace($LicenseKey)) {
    Write-Error "No license key provide by path or key"
    exit 1
}

$restoreKeyQuery = "UPDATE CMS_SettingsKey SET KeyValue = '$LicenseKey' WHERE KeyName = 'CMSLicenseKey'"
Invoke-SQLQuery -query $restoreKeyQuery

Write-Notification "License key restored."