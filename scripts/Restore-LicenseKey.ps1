<#
.Synopsis
    Updates the local database with all the objects in the CI repository
#>
[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)]
    [string]$LicenseKey = ""
)

Import-Module (Resolve-Path Utilities) `
    -Function Get-SolutionFolder, `
    Invoke-ExpressionWithException, `
    Invoke-SQLQuery, `
    Write-Status, `
    Write-Notification, `
    Write-Error `
    -Force

$restoreKeyQuery = "UPDATE CMS_SettingsKey SET KeyValue = '$LicenseKey' WHERE KeyName = 'CMSLicenseKey'"
Invoke-SQLQuery -query $restoreKeyQuery

Write-Notification "License key restored."