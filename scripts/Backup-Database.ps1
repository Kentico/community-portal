<#
.Synopsis
    Generates a .bak of the database
#>

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
$backupFilePath = Join-Path $(Get-SolutionFolder) 'database' $backupFileName

Write-Status "Backup file will be saved as: $backupFilePath"

$setEmptyKeyQuery = "UPDATE CMS_SettingsKey SET KeyValue = NULL WHERE KeyName = 'CMSLicenseKey'"
Invoke-SQLQuery -query $setEmptyKeyQuery
Write-Notification "License key cleared."

$backupQuery = @"
BACKUP DATABASE [Kentico.Community] TO DISK = N'$backupFilePath' WITH NOFORMAT, NOINIT, NAME = N'Kentico.Community', SKIP, NOREWIND, NOUNLOAD, STATS = 10;
"@
Invoke-Sqlcmd -ConnectionString $(Get-ConnectionString) -Query $backupQuery
while (!(Test-Path $backupFilePath)) {
    Write-Warning "Waiting for backup to be created at $backupFilePath"
    Start-Sleep -Seconds 1
}
Write-Notification "Database backup created at $backupFilePath"

$restoreKeyQuery = "UPDATE CMS_SettingsKey SET KeyValue = '$licenseKeyValue' WHERE KeyName = 'CMSLicenseKey'"
Invoke-SQLQuery -query $restoreKeyQuery
Write-Notification "License key restored."

$zipFilePath = "$backupFilePath.zip"
Compress-Archive -Path $backupFilePath -DestinationPath $zipFilePath
Remove-Item $backupFilePath
Write-Notification "Backup file zipped at $zipFilePath"

Set-Content -Path (Join-Path $(Get-SolutionFolder) 'database' "backups.txt") -Value $backupFileName
Write-Status "backups.txt updated."

Write-Status "Process completed."
