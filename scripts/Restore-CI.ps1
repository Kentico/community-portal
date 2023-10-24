<#
.Synopsis
    Updates the local database with all the objects in the CI repository
#>

param (
    [string] $WorkspaceFolder = ".."
)

Import-Module (Join-Path $WorkspaceFolder "scripts/Utilities.psm1")

$projectPath = Get-WebProjectPath $WorkspaceFolder
$repositoryPath = Join-Path $projectPath "App_Data/CIRepository"
$launchProfile = $Env:ASPNETCORE_ENVIRONMENT -eq "CI" ? "Portal.WebCI" : "Portal.Web"
$configuration = $Env:ASPNETCORE_ENVIRONMENT -eq "CI" ? "Release" : "Debug"

<#
.DESCRIPTION
   Runs a database migration with the given name
#>
function Invoke-Migration {
    param(
        [System.Data.SqlClient.SqlConnection] $Connection,
        [System.Data.SqlClient.SqlTransaction] $Transaction,
        [string] $MigrationName,
        [string] $RepositoryPath
    )

    $migrationFolder = "@migrations"

    $migrationPath = Join-Path $RepositoryPath "$migrationFolder/$MigrationName.sql"
    if (!(Test-Path $migrationPath)) {
        Write-Error "The file $migrationPath does not exist."
        return
    }

    $sourceScript = Get-Content $migrationPath

    $sqlCommand = ""
    $sqlList = @()

    foreach ($line in $sourceScript) {
        if ($line -imatch "^\s*GO\s*$") {
            $sqlList += $sqlCommand
            $sqlCommand = ""
        }
        else {
            $sqlCommand += $line + "`r`n"
        }
    }

    $sqlList += $sqlCommand

    $rowsAffected = 0
    foreach ($sql in $sqlList) {
        if ([bool]$sql.Trim()) {
            $command = New-Object System.Data.SqlClient.SqlCommand($sql, $Connection)
            $command.Transaction = $Transaction

            try {
                $rowsAffectedInBatch = $command.ExecuteNonQuery()

                if ($rowsAffectedInBatch -gt 0) {
                    $rowsAffected += $rowsAffectedInBatch
                }
            }
            catch {
                Write-Error $_.Exception.Message
                return $FALSE
            }
        }
    }

    Write-RowsAffected -Connection $Connection -Transaction $Transaction -MigrationName $MigrationName -RowsAffected $rowsAffected

    return $TRUE
}


<#
.DESCRIPTION
   Logs rows affected by the migration.
#>
function Write-RowsAffected {
    param(
        [System.Data.SqlClient.SqlConnection] $Connection,
        [System.Data.SqlClient.SqlTransaction] $Transaction,
        [string] $MigrationName,
        [int] $RowsAffected
    )

    $logRowsAffectedQuery = "UPDATE CI_Migration SET RowsAffected = $RowsAffected WHERE MigrationName = '$MigrationName'"
    $logRowsAffectedCommand = New-Object System.Data.SqlClient.SqlCommand($logRowsAffectedQuery, $Connection)
    $logRowsAffectedCommand.Transaction = $Transaction

    try {
        $logRowsAffectedCommand.ExecuteNonQuery()
    }
    catch {
        Write-Host "Can't log rows affected: $_.Exception.Message"
    }
}

<#
.DESCRIPTION
   Checks if a migration with the given name was already applied. If not, the method returns false and the migration is marked as applied.
#>
function Confirm-Migration {
    param(
        [System.data.SqlClient.SQLConnection] $Connection,
        [System.Data.SqlClient.SqlTransaction] $Transaction,
        [string] $MigrationName
    )

    $sql = "DECLARE @migrate INT
			EXEC @migrate = Proc_CI_CheckMigration '$MigrationName'
			SELECT @migrate"

    $command = New-Object system.data.sqlclient.sqlcommand($sql, $Connection)
    $command.Transaction = $Transaction

    return $command.ExecuteScalar()
}


<#
.DESCRIPTION
   Runs all migrations in the migration list
#>
function Invoke-MigrationList {
    param(
        [string] $ConnectionString,
        [string] $MigrationList,
        [string] $RepositoryPath
    )

    $migrations = Get-Content (Join-Path $RepositoryPath $MigrationList)

    $connection = New-Object system.data.SqlClient.SQLConnection($ConnectionString)
    $connection.Open()
    
    foreach ($migrationName in $migrations) {
        $transaction = $connection.BeginTransaction("MigrationTransaction")

        if (Confirm-Migration -Connection $connection -Transaction $transaction -MigrationName $migrationName) {
            Write-Host "Applying migration '$migrationName'."
            if (!(Invoke-Migration -Connection $Connection -Transaction $transaction -MigrationName $migrationName -RepositoryPath $RepositoryPath)) {
                $transaction.Rollback()
                $connection.Close()
                return $FALSE
            }
        }

        $transaction.Commit()
    }

    $connection.Close()

    return $TRUE
}


<#
.DESCRIPTION
   Runs migrations contained in the provided $ListFileName
#>
function Invoke-Migrations {
    param(
        [string] $WorkspaceFolder,
        [string] $RepositoryPath,
        [string] $ListFileName
    )

    $connectionString = Get-ConnectionString $WorkspaceFolder

    # Executes migration scripts before the restore
    if (!(Invoke-MigrationList $connectionString $ListFileName $RepositoryPath)) {
        Write-Error "$ListFileName Database migrations failed."
        exit 1
    }

    Write-Host "$ListFileName migrations complete for Project: $projectPath"
}

Write-Host "Processing migrations for Project: $projectPath"

Invoke-Migrations `
    -WorkspaceFolder $WorkspaceFolder `
    -RepositoryPath $repositoryPath `
    -ListFileName "Before.txt"

Write-Host "Processing CI files for Project: $projectPath"

$command = "dotnet run " + `
    "--launch-profile $launchProfile " + `
    "-c $configuration " + `
    "--no-build " + `
    "--no-restore " + `
    "--project $projectPath " + `
    "--kxp-ci-restore"

Invoke-ExpressionWithException $command

Write-Host 'CI files processed'

Invoke-Migrations `
    -WorkspaceFolder $WorkspaceFolder `
    -RepositoryPath $repositoryPath `
    -ListFileName "After.txt"

Write-Host 'Migrations processed'