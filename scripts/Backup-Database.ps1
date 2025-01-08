<#
.Synopsis
    Generates a .bak of the database
#>

param (
    [string]$backupFolderPath = "",
    [string]$mappedBackupFolderPath = ""
)

Import-Module (Resolve-Path Utilities) `
    -Function Get-SolutionFolder, `
    Get-ConnectionString, `
    Invoke-ExpressionWithException, `
    Invoke-SQLQuery, `
    Write-Status, `
    Write-Notification, `
    Write-Error `
    -Force

$licenseQuery = "SELECT KeyValue FROM CMS_SettingsKey WHERE KeyName = 'CMSLicenseKey'"
$licenseKeyValue = Invoke-SQLQuery -query $licenseQuery

if (-not $licenseKeyValue) {
    Write-Error "License key not found!"
    exit 1
}

Write-Status "License key found: $licenseKeyValue"

$versionQuery = "SELECT KeyValue FROM CMS_SettingsKey WHERE KeyName = 'CMSDBVersion'"
$cmsDbVersion = Invoke-SQLQuery -query $versionQuery

if (-not $cmsDbVersion) {
    Write-Error "CMSDBVersion not found!"
    exit 1
}

Write-Status "CMSDBVersion found: $cmsDbVersion"

$currentDate = Get-Date -Format "yyyy-MM-dd"
$backupFileName = "Kentico.Community-$cmsDbVersion-$currentDate.bak"
if ([string]::IsNullOrWhiteSpace($backupFolderPath)) {
    $backupFilePath = Join-Path $(Get-SolutionFolder) 'database' $backupFileName
    $outputPath = $backupFilePath
}
else {
    $backupFilePath = Join-Path $backupFolderPath $backupFileName
    $outputPath = Join-Path $mappedBackupFolderPath $backupFileName
}

Write-Status "Backup file will be saved as: $outputPath"

$setEmptyKeyQuery = "UPDATE CMS_SettingsKey SET KeyValue = NULL WHERE KeyName = 'CMSLicenseKey'"
Invoke-SQLQuery -query $setEmptyKeyQuery
Write-Notification "License key cleared."

$backupQuery = @"
BACKUP DATABASE [Kentico.Community] TO DISK = N'$backupFilePath' WITH NOFORMAT, NOINIT, NAME = N'Kentico.Community', SKIP, NOREWIND, NOUNLOAD, STATS = 10;
"@
$backupResult = Invoke-Sqlcmd -ConnectionString $(Get-ConnectionString) -Query $backupQuery -ErrorAction Stop

Write-Notification "Result $backupResult"

while (!(Test-Path $outputPath)) {
    Write-Warning "Waiting for backup to be created at $outputPath"
    Start-Sleep -Seconds 1
}
Write-Notification "Database backup created at $outputPath"

$restoreKeyQuery = "UPDATE CMS_SettingsKey SET KeyValue = '$licenseKeyValue' WHERE KeyName = 'CMSLicenseKey'"
Invoke-SQLQuery -query $restoreKeyQuery
Write-Notification "License key restored."

if (![string]::IsNullOrWhiteSpace($backupFolderPath)) {
    $backupFilePath = (Join-Path $(Get-SolutionFolder) 'database' $backupFileName)
    Copy-Item $outputPath $backupFilePath
}
$zipFilePath = "$backupFilePath.zip"
Compress-Archive -Path $backupFilePath -DestinationPath $zipFilePath -Force
Remove-Item $backupFilePath
Write-Notification "Backup file zipped at $zipFilePath"

Set-Content -Path (Join-Path $(Get-SolutionFolder) 'database' "backups.txt") -Value $backupFileName
Write-Status "backups.txt updated."

Write-Status "Process completed."
