[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$PublishRoot = "publish/netcup",
    [string]$FrontendPublicSiteUrl = "https://oksanalogosha.com",
    [string]$FrontendApiBaseUrl = "/api",
    [switch]$SkipNpmInstall,
    [switch]$SkipDotnetRestore
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Invoke-CheckedCommand {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,

        [string[]]$Arguments = @()
    )

    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code ${LASTEXITCODE}: $FilePath $($Arguments -join ' ')"
    }
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "../..")
Set-Location $repoRoot

$publishRootPath = Join-Path $repoRoot $PublishRoot
$appPublishPath = Join-Path $publishRootPath "app"
$migrationsPath = Join-Path $publishRootPath "migrations"
$archivePath = Join-Path $publishRootPath "bespoke-studio-release.zip"
$frontendDistPath = Join-Path $repoRoot "dist"
$backendProject = Join-Path $repoRoot "backend/src/BespokeStudio.Api/BespokeStudio.Api.csproj"
$infrastructureProject = Join-Path $repoRoot "backend/src/BespokeStudio.Infrastructure/BespokeStudio.Infrastructure.csproj"

if (Test-Path $publishRootPath) {
    Remove-Item -LiteralPath $publishRootPath -Recurse -Force
}

New-Item -ItemType Directory -Path $appPublishPath -Force | Out-Null
New-Item -ItemType Directory -Path $migrationsPath -Force | Out-Null

if (-not $SkipNpmInstall) {
    Invoke-CheckedCommand -FilePath "npm.cmd" -Arguments @("install")
}

$previousApiBaseUrl = $env:VITE_API_BASE_URL
$previousPublicSiteUrl = $env:VITE_PUBLIC_SITE_URL
try {
    $env:VITE_API_BASE_URL = $FrontendApiBaseUrl
    $env:VITE_PUBLIC_SITE_URL = $FrontendPublicSiteUrl
    Invoke-CheckedCommand -FilePath "npm.cmd" -Arguments @("run", "build")
}
finally {
    if ($null -eq $previousApiBaseUrl) {
        Remove-Item Env:VITE_API_BASE_URL -ErrorAction SilentlyContinue
    }
    else {
        $env:VITE_API_BASE_URL = $previousApiBaseUrl
    }

    if ($null -eq $previousPublicSiteUrl) {
        Remove-Item Env:VITE_PUBLIC_SITE_URL -ErrorAction SilentlyContinue
    }
    else {
        $env:VITE_PUBLIC_SITE_URL = $previousPublicSiteUrl
    }
}

if (-not $SkipDotnetRestore) {
    Invoke-CheckedCommand -FilePath "dotnet" -Arguments @("restore", "backend/BespokeStudio.sln")
}

Invoke-CheckedCommand -FilePath "dotnet" -Arguments @("build", "backend/BespokeStudio.sln", "-c", $Configuration, "--no-restore")
Invoke-CheckedCommand -FilePath "dotnet" -Arguments @("publish", $backendProject, "-c", $Configuration, "-o", $appPublishPath, "--no-build")

$wwwrootPath = Join-Path $appPublishPath "wwwroot"
New-Item -ItemType Directory -Path $wwwrootPath -Force | Out-Null
Copy-Item -Path (Join-Path $frontendDistPath "*") -Destination $wwwrootPath -Recurse -Force

Invoke-CheckedCommand -FilePath "dotnet" -Arguments @(
    "ef",
    "migrations",
    "script",
    "--idempotent",
    "--no-build",
    "--project",
    $infrastructureProject,
    "--startup-project",
    $backendProject,
    "--output",
    (Join-Path $migrationsPath "bespoke-studio-idempotent.sql"))

Compress-Archive -Path (Join-Path $appPublishPath "*") -DestinationPath $archivePath -Force

Write-Host "Release archive: $archivePath"
Write-Host "Published app: $appPublishPath"
Write-Host "Migration script: $(Join-Path $migrationsPath 'bespoke-studio-idempotent.sql')"
