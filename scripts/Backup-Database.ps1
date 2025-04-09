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
    Get-ScriptConfig, `
    Write-Error `
    -Force

$scriptConfig = Get-ScriptConfig

$licenseQuery = "SELECT KeyValue FROM CMS_SettingsKey WHERE KeyName = 'CMSLicenseKey'"
$licenseKeyValue = Invoke-SQLQuery -query $licenseQuery

if (-not $licenseKeyValue) {
    Write-Warning "No license key found in the database"
}
else {
    Write-Status "License key found: $licenseKeyValue"
}

$versionQuery = "SELECT KeyValue FROM CMS_SettingsKey WHERE KeyName = 'CMSDBVersion'"
$cmsDbVersion = Invoke-SQLQuery -query $versionQuery

if (-not $cmsDbVersion) {
    Write-Error "CMSDBVersion not found!"
    exit 1
}

Write-Status "CMSDBVersion found: $cmsDbVersion"

$currentDate = Get-Date -Format "yyyy-MM-dd"
$backupFileName = "Kentico.Community-$cmsDbVersion-$currentDate.bak"

$backupSourceFilePath = "$($scriptConfig.BackupSourceFolderPath)$($scriptConfig.BackupSourcePathSeparator)$backupFileName"
# $backupSourceFilePath = $backupSourceFilePath -replace [regex]::Escape([IO.Path]::DirectorySeparatorChar), $pathSeparator
$backupHostSourceFilePath = Join-Path $scriptConfig.BackupHostSourceFolderPath $backupFileName
$backupDestinationFilePath = Join-Path $scriptConfig.BackupDestinationFolderPath $backupFileName

$setEmptyKeyQuery = "UPDATE CMS_SettingsKey SET KeyValue = NULL WHERE KeyName = 'CMSLicenseKey'"
Invoke-SQLQuery -query $setEmptyKeyQuery
Write-Notification "License key cleared."

$backupQuery = @"
BACKUP DATABASE [Kentico.Community] 
TO DISK = N'$backupSourceFilePath'
WITH NOFORMAT, 
    NOINIT, 
    NAME = N'Kentico.Community', 
    SKIP, 
    NOREWIND, 
    NOUNLOAD, 
    STATS = 10;
"@
$backupResult = Invoke-Sqlcmd -ConnectionString $(Get-ConnectionString) -Query $backupQuery -ErrorAction Stop

if (-not [string]::IsNullOrWhiteSpace($backupResult)) {
    Write-Notification "Backup query result: $backupResult"
}

while (!(Test-Path $backupHostSourceFilePath)) {
    Write-Warning "Waiting for backup to be created: $backupHostSourceFilePath"
    Start-Sleep -Seconds 1
}
Write-Notification "Database backup file created: $backupHostSourceFilePath"

$restoreKeyQuery = "UPDATE CMS_SettingsKey SET KeyValue = '$licenseKeyValue' WHERE KeyName = 'CMSLicenseKey'"
Invoke-SQLQuery -query $restoreKeyQuery
Write-Notification "License key restored."

Copy-Item $backupHostSourceFilePath $backupDestinationFilePath
Write-Notification "Backup file copied: $($backupHostSourceFilePath) => $backupDestinationFilePath"

$zipFilePath = Join-Path $scriptConfig.BackupDestinationFolderPath "$backupFileName.zip"
Compress-Archive -Path $backupDestinationFilePath -DestinationPath $zipFilePath -Force
Remove-Item $backupDestinationFilePath
Write-Notification "Backup file zipped to: $zipFilePath"

Write-Status "Process completed."
