<#
.Synopsis
    Updates local database data and schema to the version of the project's referenced Xperience NuGet package
#>

param (
    [string] $WorkspaceFolder = ".."
)

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
        } elseif ($result -eq 1) {
            Write-Host "CMSEnableCI set to $Value"
        }
    }
    catch {
        Write-Host "Can't update Settings Key CMSEnableCI: $_.Exception.Message"
    }
}

Import-Module (Join-Path $WorkspaceFolder "scripts/Utilities.psm1")

$projectPath = Get-WebProjectPath $WorkspaceFolder
$launchProfile = $Env:ASPNETCORE_ENVIRONMENT -eq "CI" ? "Portal.WebCI" : "Portal.Web"
$configuration = $Env:ASPNETCORE_ENVIRONMENT -eq "CI" ? "Release" : "Debug"

Write-Host "Begin Project Update: $projectPath"

$connectionString = Get-ConnectionString $WorkspaceFolder
$connection = New-Object system.data.SqlClient.SQLConnection($ConnectionString)
$connection.Open()

Write-CMSEnableCI $connection 'False'

$command = "dotnet run " + `
    "--project $projectPath " + `
    "--launch-profile $launchProfile " + `
    "--no-build " + `
    "--no-restore " + `
    "-c $configuration " + `
    "--kxp-update"

Invoke-ExpressionWithException $command

Write-CMSEnableCI $connection 'True'

$connection.Close()

Write-Host "Update Complete"

& (Join-Path $WorkspaceFolder "scripts/Store-CI.ps1") -WorkspaceFolder $WorkspaceFolder