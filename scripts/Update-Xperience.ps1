<#
.Synopsis
    Updates local database data and schema to the version of the project's referenced Xperience NuGet package
#>

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
    "--no-build " + `
    "--no-restore " + `
    "-c $configuration " + `
    "--kxp-update"

Invoke-ExpressionWithException $command

Write-CMSEnableCI $connection 'True'

$connection.Close()

Write-Host "`n"
Write-Status "Update Complete"

& (Resolve-Path "./Store-CI.ps1")