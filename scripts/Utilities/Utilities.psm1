# Utilities for other scripts

$scriptConfig = @{}
$scriptConfig.WorkspaceFolder = ".."
$scriptConfig.SolutionFileName = "Kentico.Community.Portal.slnx"
$scriptConfig.WebProjectAssemblyName = "Kentico.Community.Portal.Web"
$scriptConfig.BackupSourceFolderPath = "/var/backups"
$scriptConfig.BackupSourcePathSeparator = "/"
$scriptConfig.BackupHostSourceFolderPath = "~/backups"
$scriptConfig.BackupDestinationFolderPath = Join-Path $scriptConfig.WorkspaceFolder "database"
$scriptConfig.CodeGenerationTypes = @{
    "ReusableContentTypes" = @{ "Include" = ""; "Exclude" = "" }
    "PageContentTypes"     = @{ "Include" = ""; "Exclude" = "" }
    "Forms"                = @{ "Include" = ""; "Exclude" = "" }
    "ReusableFieldSchemas" = @{ "Include" = ""; "Exclude" = "" }
    "EmailContentTypes"    = @{ "Include" = ""; "Exclude" = "" }
    "Classes"              = @{ "Include" = ""; "Exclude" = "" }
}
$scriptConfig.CompressDeploymentPackage = $True

# Local settings can be defined by copying settings.template.json to settings.local.json.
# Resolved from the module's own location rather than $scriptConfig.WorkspaceFolder, so the
# file is found regardless of which directory the calling script was invoked from.
$settingsFileName = "settings.local.json"
$settingsFilePath = Join-Path $PSScriptRoot $settingsFileName

if (Test-Path $settingsFilePath) {
    Write-Information "Using settings from $settingsFileName"

    $settings = Get-Content -Path $settingsFilePath -Raw | ConvertFrom-Json

    # Only keys actually present in the file override the defaults above. Assigning
    # unconditionally set any absent key to $null, so a settings file that omitted one
    # silently discarded its default instead of falling back to it.
    foreach ($key in @(
            "BackupSourceFolderPath",
            "BackupSourcePathSeparator",
            "BackupHostSourceFolderPath",
            "CodeGenerationTypes",
            "CompressDeploymentPackage")) {

        if ($null -ne $settings.$key) {
            $scriptConfig.$key = $settings.$key
        }
    }
}
else {
    Write-Information "Settings file '$settingsFileName' not found. Using default settings."
}

<#
    .DESCRIPTION
        Returns shared configuration for PowerShell scripts
#>
function Get-ScriptConfig {
    return $scriptConfig
}

<#
    .DESCRIPTION
        Returns the solutions's root folder
#>
function Get-SolutionFolder {
    return Resolve-Path($($scriptConfig.WorkspaceFolder))
}

<#
    .DESCRIPTION
        Returns the main solution file path
#>
function Get-SolutionPath {
    return Resolve-Path(Join-Path $($scriptConfig.WorkspaceFolder) $($scriptConfig.SolutionFileName))
}

<#
    .DESCRIPTION
        Returns the web application folder path from the workspace root
#>
function Get-WebProjectPath {
    return Resolve-Path(Join-Path $($scriptConfig.WorkspaceFolder) "src/Kentico.Community.Portal.Web")
}

<#
    .DESCRIPTION
        Returns the admin application folder path from the workspace root
#>
function Get-AdminProjectPath {
    return Resolve-Path(Join-Path $($scriptConfig.WorkspaceFolder) "src/Kentico.Community.Portal.Admin")
}

<#
    .DESCRIPTION
        Returns the admin client application folder path from the workspace root
#>
function Get-AdminClientProjectPath {
    return Resolve-Path(Join-Path $($scriptConfig.WorkspaceFolder) "src/Kentico.Community.Portal.Admin/Client")
}

<#
    .DESCRIPTION
        Returns the Core project folder path from the workspace root
#>
function Get-CoreProjectPath {
    return Resolve-Path(Join-Path $($scriptConfig.WorkspaceFolder) "src/Kentico.Community.Portal.Core")
}


<#
    .DESCRIPTION
        Gets the database connection string from the user secrets or appsettings.json file
#>
function Get-ConnectionString {
    # Prefer a connection string injected by Aspire (populated when a script runs as a resource in the
    # Aspire dashboard via WithReference). Falls back to user secrets / appsettings for standalone runs.
    if (-not [string]::IsNullOrWhiteSpace($Env:ConnectionStrings__CMSConnectionString)) {
        return $Env:ConnectionStrings__CMSConnectionString
    }

    $projectPath = Get-WebProjectPath

    $connectionString = dotnet user-secrets list --project $projectPath `
    | Select-String -Pattern "ConnectionStrings:" `
    | ForEach-Object { $_.Line -replace '^ConnectionStrings:CMSConnectionString \= ', '' }

    if (-not [string]::IsNullOrEmpty($connectionString)) {
        return $connectionString
    }

    $appSettingFileName = $Env:ASPNETCORE_ENVIRONMENT -eq "CI" ? 'appsettings.CI.json' : 'appsettings.json'
    $jsonFilePath = Join-Path $projectPath $appSettingFileName
    
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

function Invoke-SQLQuery {
    param (
        [string]$query,
        [int]$maxRetries = 5,
        [int]$retryDelaySeconds = 1
    )

    $connectionString = Get-ConnectionString
    $attempt = 0
    
    while ($attempt -lt $maxRetries) {
        try {
            $sqlCommand = New-Object System.Data.SqlClient.SqlCommand
            $sqlConnection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
            $sqlConnection.Open()
            
            $sqlCommand.Connection = $sqlConnection
            $sqlCommand.CommandText = $query
            $result = $sqlCommand.ExecuteScalar()
            
            $sqlConnection.Close()
            return $result
        }
        catch {
            $attempt++
            if ($attempt -lt $maxRetries) {
                Write-Host "Database connection failed (attempt $attempt/$maxRetries), retrying in ${retryDelaySeconds}s: $_"
                Start-Sleep -Seconds $retryDelaySeconds
            }
            else {
                throw "Failed to execute SQL query after $maxRetries attempts: $_"
            }
        }
    }
}

function Write-Status {
    param(
        [string]$message
    )

    Write-Host $message -ForegroundColor Blue
}

function Write-Notification {
    param(
        [string]$message
    )

    Write-Host $message -ForegroundColor Magenta
}

function Write-Error {
    param(
        [string]$message
    )

    Write-Host $message -ForegroundColor Red
}