<#
.Synopsis
    Creates a deployment package for uploading to the Xperience Cloud environment.
#>
[CmdletBinding()]
param (
    # Output path for exported deployment package. Default is repository root.
    [Parameter(Mandatory = $false)]
    [string]$OutputPackagePath = "../DeploymentPackage.zip",

    # If present, the custom build number won't be used as a "Product version" suffix in the format yyyyMMddHHmm.
    [switch]$KeepProductVersion,

    # Mode in which the storage assets are deployed, if present.
    [ValidateSet("Create", "CreateUpdate")]
    [String]$StorageAssetsDeploymentMode = "CreateUpdate",

    [bool]$CompressPackage = $true,

    [bool]$DeployStorageAssets = $false
)
$ErrorActionPreference = "Stop"

Import-Module (Resolve-Path Utilities) `
    -Function `
    Get-SolutionPath, `
    Invoke-ExpressionWithException, `
    Get-WebProjectPath, `
    Get-ScriptConfig, `
    Write-Status, `
    Write-Notification `
    -Force

$projectPath = Get-WebProjectPath
$configuration = $Env:ASPNETCORE_ENVIRONMENT -eq "CI" ? "Release" : "Debug"
$launchProfile = $Env:ASPNETCORE_ENVIRONMENT -eq "CI" ? "Portal.WebCI" : "Portal.Web"
$AssemblyName = $(Get-ScriptConfig).AssemblyName

$OutputFolderPath = "../bin/CloudDeployment/"
$MetadataFilePath = Join-Path $OutputFolderPath "cloud-metadata.json"

$CDRepositoryFolderFolder = "`$CDRepository"
$CDRepositoryFolderPath = Join-Path $projectPath $CDRepositoryFolderFolder
$StorageAssetsFolderName = "`$StorageAssets"

$CDRepositoryConfigurationFolder = "App_Data/CDRepository"

# Get CD repositories paths
$ProjectCDRepositoryPath = Join-Path $projectPath $CDRepositoryFolderFolder
$OutputCDRepositoryPath = Join-Path $OutputFolderPath $CDRepositoryFolderFolder

$BuildNumber = (Get-Date).ToUniversalTime().ToString("yyyyMMddHHmm")

Write-Status "Generating `$CDRepository files for project: $projectPath"

if (Test-Path -Path $CDRepositoryFolderPath -PathType Container) {
    Remove-Item -Path $CDRepositoryFolderPath -Recurse -Force
    Write-Output "Previous CDRepository deleted."
}

function Copy-Migrations {
    param(
        [string] $ProjectPath,
        [string] $ConfigurationFolder,
        [string] $RepositoryFolder
    )

    $sourceConfigurationPath = Join-Path $ProjectPath $ConfigurationFolder
    $repositoryPath = Join-Path $ProjectPath $RepositoryFolder

    Copy-Item -Path $sourceConfigurationPath -Destination $repositoryPath -Recurse
}

Copy-Migrations `
    -ProjectPath $projectPath `
    -ConfigurationFolder $CDRepositoryConfigurationFolder `
    -RepositoryFolder $CDRepositoryFolderFolder

$cdStoreCommand = "dotnet run " + `
    "--project $projectPath " + `
    "--launch-profile $launchProfile " + `
    "--no-build " + `
    "--no-restore " + `
    "-c $configuration " + `
    "--kxp-cd-store " + `
    "--repository-path './$CDRepositoryFolderFolder'" `

Invoke-ExpressionWithException $cdStoreCommand

Write-Status "`$CDRepository generated"

# Remove previously published website
Remove-Item -Recurse -Force $OutputFolderPath -ErrorAction SilentlyContinue

# Publish the application in the 'Release' mode
$publishCommand = "dotnet publish " + `
    "$projectPath " + `
    "--nologo " + `
    "-c $configuration " + `
    "-o $OutputFolderPath " + `
    "--no-build"

if (!$KeepProductVersion) {
    $publishCommand += " --version-suffix $BuildNumber"
}

Invoke-ExpressionWithException $publishCommand

# Check for non-existing or empty CD repository which could corrupt the database
if (-not (Test-Path $ProjectCDRepositoryPath) -or (@(Get-ChildItem -Path $ProjectCDRepositoryPath -Directory).Count -le 0)) {
    throw "Cannot detect CD repository on path '$ProjectCDRepositoryPath'. Make sure to run 'dotnet run --kxp-cd-store --repository-path ""```$CDRepository""' before 'Export-DeploymentPackage.ps1'."
}

# Copy content of the CD repository to the output folder
Copy-Item -Force -Recurse "$ProjectCDRepositoryPath/*" -Destination $OutputCDRepositoryPath

# Get storage assets paths
$LocalStorageAssetsPath = Join-Path $projectPath $StorageAssetsFolderName

if ($DeployStorageAssets -and (Test-Path $LocalStorageAssetsPath)) {
    $OutputStorageAssetsPath = Join-Path $OutputFolderPath $StorageAssetsFolderName
    # Check if storage asset top-level directories have valid names
    Get-ChildItem -Path $LocalStorageAssetsPath | ForEach-Object {
        if ($_.Name -cnotmatch "^[a-z0-9](?!.*--)[a-z0-9-]{1,61}[a-z0-9]$") {
            throw "Storage asset directory '$($_.FullName)' does not have a valid name. Top level storage asset directories must have names that are 3-63 characters long and contain only lowercase letters, numbers or dashes (-). Every dash symbol must be surrounded by letters or numbers."
        }
    }

    # If storage assets folder exists, copy it to the output folder, but only copy child items of the source folder
    Copy-Item -Force -Recurse (Join-Path $LocalStorageAssetsPath "*") -Destination $OutputStorageAssetsPath

    # Deployed assets need to have lowercase names
    Get-ChildItem -Path $OutputStorageAssetsPath -Recurse | ForEach-Object {
        $lowercasedAssetName = $_.Name.ToLowerInvariant()

        if ($_.Name -cne $lowercasedAssetName) {
            Rename-Item -Force $_.FullName "$($_.Name).tmp"
            Rename-Item -Force "$($_.FullName).tmp" $lowercasedAssetName
        }
    }

    # Add necessary metadata if storage assets folder has been exported as well
    if (Test-Path $OutputStorageAssetsPath) {
        $PackageMetadata.Add("StorageAssetsDirectory", $StorageAssetsFolderName)
        $PackageMetadata.Add("StorageAssetsDeploymentMode", $StorageAssetsDeploymentMode)
    }
}

$AssemblyPath = Join-Path $OutputFolderPath "$AssemblyName.dll" -Resolve
$PackageMetadata = @{
    AssemblyName = $AssemblyName
    Version      = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($AssemblyPath).ProductVersion
}

# Create all necessary metadata for cloud-based package deployment
$PackageMetadata | ConvertTo-Json -Depth 2 | Set-Content $MetadataFilePath -Encoding utf8

# Create a deployment package
if (Test-Path -Path $OutputPackagePath -PathType Container) {
    $OutputPackagePath = Join-Path -Path $OutputPackagePath -ChildPath "./DeploymentPackage.zip"
}

Write-Status "$OutputFolderPath folder fully populated"

if ($CompressPackage) {
    Compress-Archive -Force -Path "$OutputFolderPath/*" -DestinationPath $OutputPackagePath

    Write-Status "Deployment package created: $OutputPackagePath"
}
else {
    Write-Notification "Deployment package generation skipped"
}
