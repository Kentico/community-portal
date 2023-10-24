# Utilities

<#
    .DESCRIPTION
        Returns the web application folder path from the workspace root
#>
function Get-WebProjectPath {
    param(
        [string] $WorkspaceFolder = ".."
    )

    return Join-Path $WorkspaceFolder "src/Kentico.Community.Portal.Web"
}

<#
.DESCRIPTION
   Gets the database connection string from the user secrets or appsettings.json file
#>
function Get-ConnectionString {
    param(
        [string] $WorkspaceFolder = ".."
    )

    $projectPath = Get-WebProjectPath ($WorkspaceFolder)

    # Try to get the connection string from user secrets first
    Write-Host "Checking for a connection string user secrets for project: $projectPath"

    $connectionString = dotnet user-secrets list --project $projectPath `
    | Select-String -Pattern "ConnectionStrings:" `
    | ForEach-Object { $_.Line -replace '^ConnectionStrings:CMSConnectionString \= ', '' }

    if (-not [string]::IsNullOrEmpty($connectionString)) {
        Write-Host 'Using ConnectionString from user-secrets'

        return $connectionString
    }

    $appSettingFileName = $Env:ASPNETCORE_ENVIRONMENT -eq "CI" ? 'appsettings.CI.json' : 'appsettings.json'
    
    $jsonFilePath = Join-Path $projectPath $appSettingFileName

    Write-Host "Using settings from $jsonFilePath"
    
    if (!(Test-Path $jsonFilePath)) {
        throw "Could not find file $jsonFilePath"
    }

    $appSettingsJson = Get-Content $jsonFilePath | Out-String | ConvertFrom-Json
    $connectionString = $appSettingsJson.ConnectionStrings.CMSConnectionString;
    
    if (!$connectionString) {
        throw "Connection string not found in $jsonFilePath"
    }

    return $connectionString;
}

<#
.DESCRIPTION
   Ensures the expression successfully exits and throws an exception
   with the failed expression if it does not.
#>
function Invoke-ExpressionWithException {
    param(
        [string]$expression
    )

    Write-Host "$expression"

    Invoke-Expression -Command $expression

    if ($LASTEXITCODE -ne 0) {
        $errorMessage = "[ $expression ] failed`n`n"

        throw $errorMessage
    }
}