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
    # Marks the deployment package as zero-downtime ready (enables read-only mode support during SaaS deployment)
    [switch]$ZeroDowntimeSupportEnabled,
    # If specified, overrides the CompressDeploymentPackage setting from config
    [switch]$CompressPackage
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


$scriptConfig = Get-ScriptConfig
$projectPath = Get-WebProjectPath
$configuration = $Env:ASPNETCORE_ENVIRONMENT -eq "CI" ? "Release" : "Debug"
$launchProfile = $Env:ASPNETCORE_ENVIRONMENT -eq "CI" ? "Portal.WebCI" : "Portal.Web"
$AssemblyName = $scriptConfig.WebProjectAssemblyName

$OutputFolderPath = "../bin/CloudDeployment/"
$MetadataFilePath = Join-Path $OutputFolderPath "cloud-metadata.json"

$CDRepositoryFolderFolder = "`$CDRepository"
$CDRepositoryFolderPath = Join-Path $projectPath $CDRepositoryFolderFolder

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

$AssemblyPath = Join-Path $OutputFolderPath "$AssemblyName.dll" -Resolve
$PackageMetadata = @{
    AssemblyName                   = $AssemblyName
    Version                        = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($AssemblyPath).ProductVersion
    SupportsZeroDowntimeDeployment = $ZeroDowntimeSupportEnabled.IsPresent
}

if ($ZeroDowntimeSupportEnabled) {
    Write-Status "Zero-downtime support enabled"
}

if (-not $PackageMetadata.ContainsKey('AssemblyName') -or [string]::IsNullOrWhiteSpace($PackageMetadata.AssemblyName)) {
    throw "Package metadata AssemblyName missing or empty"
}
if (-not $PackageMetadata.ContainsKey('Version') -or [string]::IsNullOrWhiteSpace($PackageMetadata.Version)) {
    throw "Package metadata Version missing or empty"
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
