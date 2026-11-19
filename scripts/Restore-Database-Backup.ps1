<#
.SYNOPSIS
    Restores the Xperience database into a SQL Server container from a .bak file.

.DESCRIPTION
    Shared by ci.yml and deploy.yml, which previously each carried their own copy of
    this logic inline and had already drifted apart (only CI verified the backup file
    before restoring it).

    Every stage fails loudly with the command that broke and what it was trying to do,
    so a red CI run names its own cause instead of requiring a log archaeology session.

    After the restore the database is switched to SIMPLE recovery and its log shrunk.
    The CI restore that follows generates enough transactions to grow the log past
    2.5 GB under FULL recovery, on a runner that is already tight on disk by the time
    Playwright downloads browsers.

.PARAMETER BackupFileName
    File name (not path) of the extracted .bak inside the ./database folder.

.PARAMETER SaPassword
    Password for the SQL Server `sa` account.

.PARAMETER ContainerName
    Name of the running SQL Server container. Matches the container created by the
    potatoqualitee/mssqlsuite action.

.PARAMETER ReadyTimeoutSeconds
    How long to wait for SQL Server to start accepting connections before giving up.
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$BackupFileName,

    [Parameter(Mandatory = $true)]
    [string]$SaPassword,

    [Parameter(Mandatory = $false)]
    [string]$ContainerName = "sql",

    [Parameter(Mandatory = $false)]
    [int]$ReadyTimeoutSeconds = 120
)

$ErrorActionPreference = "Stop"

Import-Module (Resolve-Path Utilities) `
    -Function `
    Write-Status, `
    Write-Notification `
    -Force

# Not a parameter: database/backup-restore.sql hardcodes both [Kentico.Community] and the
# '/var/opt/mssql/backups/Kentico.Community.bak' source path. A caller-supplied name would
# change where this script copies the backup while the .sql still restored from the old
# path, which reports success having restored nothing.
$databaseName = "Kentico.Community"

$databaseFolderPath = Join-Path ".." "database"
$hostBackupFilePath = Join-Path $databaseFolderPath $BackupFileName
$containerBackupFolderPath = "/var/opt/mssql/backups"
$containerBackupFilePath = "$containerBackupFolderPath/$databaseName.bak"

<#
    .DESCRIPTION
        Runs a sqlcmd query against the container, throwing a message naming the
        stage that failed rather than leaving a bare non-zero exit code in the log.

        -b is what makes that possible: without it sqlcmd exits 0 even when the T-SQL
        raises an error, so a corrupt backup or a failed RESTORE would be reported as
        success here and surface several steps later as an unrelated failure.
#>
function Invoke-SqlCmdQuery {
    param(
        [string]$Query,
        [string]$Stage
    )

    $output = sqlcmd -S localhost -d master -U "sa" -P $SaPassword -N o -C -b -Q $Query 2>&1 | Out-String

    if ($LASTEXITCODE -ne 0) {
        Write-Host $output
        throw "$Stage failed (sqlcmd exit code $LASTEXITCODE). Query: $Query"
    }

    return $output
}

<#
    .DESCRIPTION
        Runs a sqlcmd script file against the container. See Invoke-SqlCmdQuery for -b.
#>
function Invoke-SqlCmdFile {
    param(
        [string]$Path,
        [string]$Stage
    )

    $output = sqlcmd -S localhost -d master -U "sa" -P $SaPassword -N o -C -b -i $Path 2>&1 | Out-String

    if ($LASTEXITCODE -ne 0) {
        Write-Host $output
        throw "$Stage failed (sqlcmd exit code $LASTEXITCODE). Script: $Path"
    }

    return $output
}

<#
    .DESCRIPTION
        Runs a docker command, throwing a message naming the stage that failed.
#>
function Invoke-DockerCommand {
    param(
        [string[]]$DockerArguments,
        [string]$Stage
    )

    $output = & docker @DockerArguments 2>&1 | Out-String

    if ($LASTEXITCODE -ne 0) {
        Write-Host $output
        throw "$Stage failed (docker exit code $LASTEXITCODE). Command: docker $($DockerArguments -join ' ')"
    }

    return $output
}

# === Preconditions ===
# Checked up front so a missing file or a dead container reports itself here rather
# than as a confusing sqlcmd error several stages later.

if (-not (Test-Path $hostBackupFilePath)) {
    $available = (Get-ChildItem -Path $databaseFolderPath -File -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Name) -join ", "

    if ([string]::IsNullOrWhiteSpace($available)) {
        $available = "<none - the folder is empty or does not exist>"
    }

    throw "Backup file not found at '$hostBackupFilePath'. Files present in '$databaseFolderPath': $available"
}

$runningContainers = docker ps --filter "name=$ContainerName" --format "{{.Names}}"

if ($runningContainers -notcontains $ContainerName) {
    throw "SQL Server container '$ContainerName' is not running. Running containers: $((docker ps --format '{{.Names}}') -join ', ')"
}

# === Readiness gate ===
# The SQL install step can return before the engine accepts connections. Gate on a real
# query rather than assuming readiness, so a slow engine start is a clear wait here
# instead of an unexplained failure in the restore below.

Write-Status "Waiting up to ${ReadyTimeoutSeconds}s for SQL Server to accept connections..."

$deadline = (Get-Date).AddSeconds($ReadyTimeoutSeconds)
$isReady = $false

while (-not $isReady -and (Get-Date) -lt $deadline) {
    sqlcmd -S localhost -d master -U "sa" -P $SaPassword -N o -C -b -Q "SELECT 1" *> $null

    if ($LASTEXITCODE -eq 0) {
        $isReady = $true
    }
    else {
        Start-Sleep -Seconds 2
    }
}

if (-not $isReady) {
    docker logs $ContainerName

    throw "SQL Server did not accept connections within ${ReadyTimeoutSeconds}s. Container logs are above."
}

Write-Notification "SQL Server is accepting connections."

# === Copy the backup into the container ===

Write-Status "Copying backup into container '$ContainerName'..."

Invoke-DockerCommand `
    -DockerArguments @("exec", "-u", "root", $ContainerName, "bash", "-c", "mkdir -p $containerBackupFolderPath") `
    -Stage "Creating the backup folder in the container" | Out-Null

Invoke-DockerCommand `
    -DockerArguments @("cp", $hostBackupFilePath, "${ContainerName}:$containerBackupFilePath") `
    -Stage "Copying the backup into the container" | Out-Null

Invoke-DockerCommand `
    -DockerArguments @("exec", "-u", "root", $ContainerName, "bash", "-c", "chown mssql:mssql $containerBackupFilePath") `
    -Stage "Setting backup file ownership" | Out-Null

Invoke-DockerCommand `
    -DockerArguments @("exec", "-u", "root", $ContainerName, "bash", "-c", "chmod 644 $containerBackupFilePath") `
    -Stage "Setting backup file permissions" | Out-Null

Write-Notification "Backup copied to $containerBackupFilePath"

# === Verify then restore ===

Write-Status "Verifying backup file contents..."

Invoke-SqlCmdQuery `
    -Query "RESTORE FILELISTONLY FROM DISK = '$containerBackupFilePath'" `
    -Stage "Verifying the backup file" | Write-Host

Write-Status "Restoring database '$databaseName'..."

Invoke-SqlCmdFile `
    -Path (Join-Path $databaseFolderPath "backup-restore.sql") `
    -Stage "Restoring the database from backup" | Write-Host

Write-Notification "Database '$databaseName' restored."

# === Reclaim log space ===
# The restored log arrives sized for the transaction volume of the source database and
# grows further during the CI restore. SIMPLE recovery lets SQL Server reuse log space
# instead of retaining it, which is what keeps the file small for the rest of the run.

Write-Status "Switching '$databaseName' to SIMPLE recovery and shrinking its log..."

$shrinkLogQuery = @"
ALTER DATABASE [$databaseName] SET RECOVERY SIMPLE;

USE [$databaseName];

DECLARE @logFileName SYSNAME = (SELECT TOP 1 name FROM sys.database_files WHERE type_desc = 'LOG');
DECLARE @shrinkCommand NVARCHAR(MAX) = N'DBCC SHRINKFILE (N''' + @logFileName + N''', 64)';

EXEC sp_executesql @shrinkCommand;
"@

Invoke-SqlCmdQuery -Query $shrinkLogQuery -Stage "Reclaiming transaction log space" | Write-Host

Write-Status "Database restore complete."
