<#
.Synopsis
    Updates local database data and schema to the version of the project's referenced Xperience NuGet package.
    Optional -AgentMode switch skips interactive confirmations by appending --skip-confirmation.

.DESCRIPTION
    Executes the Xperience update via 'dotnet run' passing '-- --kxp-update'. When -AgentMode is supplied,
    the script adds '--skip-confirmation' (per Xperience docs) to bypass the database backup prompt for automation.

.PARAMETER AgentMode
    When specified, disables confirmation prompts during the Xperience update (adds --skip-confirmation).

.EXAMPLE
    ./Update-Xperience.ps1
    Performs standard update with confirmation prompt.

.EXAMPLE
    ./Update-Xperience.ps1 -AgentMode
    Runs update without confirmation prompt (for CI/CD or scripted automation).
#>

param(
    [switch] $AgentMode
)

Import-Module (Resolve-Path Utilities) `
    -Function Get-WebProjectPath, `
    Invoke-ExpressionWithException, `
    Get-ConnectionString, `
    Write-Status, `
    Write-Notification, `
    Write-Error `
    -Force

<#
.DESCRIPTION
   Sets the CMSEnableCI settings key to the given value
   Should be 'True' or 'False'
#>
function Write-CMSEnableCI {
    param(
        [System.Data.SqlClient.SqlConnection] $Connection,
        [string] $Value
    )

    $updateQuery = "UPDATE CMS_SettingsKey SET KeyValue = N'$Value' WHERE KeyName = N'CMSEnableCI'"
    $updateCommand = New-Object System.Data.SqlClient.SqlCommand($updateQuery, $Connection)

    try {
        $result = $updateCommand.ExecuteNonQuery()
        if ($result -eq 0) {
            throw "CMS_SettingsKey update did not affect any rows."
        }
        elseif ($result -eq 1) {
            Write-Notification "CMSEnableCI set to $Value"
        }
    }
    catch {
        Write-Error "Can't update Settings Key CMSEnableCI: $_.Exception.Message"
    }
}

$projectPath = Get-WebProjectPath
$launchProfile = $Env:ASPNETCORE_ENVIRONMENT -eq "CI" ? "Portal.WebCI" : "Portal.Web"
$configuration = $Env:ASPNETCORE_ENVIRONMENT -eq "CI" ? "Release" : "Debug"

Write-Status "Begin Xperience Update"
Write-Host "`n"

$connection = New-Object system.data.SqlClient.SQLConnection(Get-ConnectionString)
$connection.Open()

Write-CMSEnableCI $connection 'False'

$command = "dotnet run " + `
    "--project $projectPath " + `
    "--launch-profile $launchProfile " + `
    "-c $configuration " + `
    "-- " + `
    "--kxp-update" + `
($AgentMode.IsPresent ? " --skip-confirmation" : "")

if ($AgentMode) {
    Write-Status "AgentMode enabled: skipping confirmations"
}

Invoke-ExpressionWithException $command

Write-CMSEnableCI $connection 'True'

$connection.Close()

Write-Host "`n"
Write-Status "Update Complete"

& (Resolve-Path "./Store-CI.ps1")