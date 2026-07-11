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

function Assert-FileContains {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$Pattern,

        [string]$Description = $Pattern
    )

    if (-not (Select-String -LiteralPath $Path -Pattern $Pattern -SimpleMatch -Quiet)) {
        throw "Migration SQL validation failed: missing $Description."
    }
}

function Assert-FileDoesNotContain {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$Pattern,

        [string]$Description = $Pattern
    )

    if (Select-String -LiteralPath $Path -Pattern $Pattern -SimpleMatch -Quiet) {
        throw "Migration SQL validation failed: forbidden $Description found."
    }
}

function Assert-ExistingFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$Description
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Release validation failed: missing $Description at $Path."
    }
}

function New-LinuxCompatibleZipArchive {
    param(
        [Parameter(Mandatory = $true)]
        [string]$SourceDirectory,

        [Parameter(Mandatory = $true)]
        [string]$DestinationPath
    )

    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem

    if (-not (Test-Path -LiteralPath $SourceDirectory -PathType Container)) {
        throw "Release archive validation failed: source directory does not exist: $SourceDirectory"
    }

    $sourceRoot = [System.IO.Path]::GetFullPath($SourceDirectory)
    if (-not $sourceRoot.EndsWith([System.IO.Path]::DirectorySeparatorChar)) {
        $sourceRoot = $sourceRoot + [System.IO.Path]::DirectorySeparatorChar
    }

    $files = @(Get-ChildItem -LiteralPath $SourceDirectory -File -Recurse)
    if ($files.Count -eq 0) {
        throw "Release archive validation failed: source directory contains no files: $SourceDirectory"
    }

    if (Test-Path -LiteralPath $DestinationPath) {
        Remove-Item -LiteralPath $DestinationPath -Force
    }

    $archive = [System.IO.Compression.ZipFile]::Open($DestinationPath, [System.IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($file in $files) {
            $filePath = [System.IO.Path]::GetFullPath($file.FullName)
            $entryName = $filePath.Substring($sourceRoot.Length).
                Replace([System.IO.Path]::DirectorySeparatorChar.ToString(), "/").
                Replace([System.IO.Path]::AltDirectorySeparatorChar.ToString(), "/")
            if ($entryName.Contains("\")) {
                throw "Release archive validation failed: generated ZIP entry still contains a backslash: $entryName"
            }

            [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
                $archive,
                $filePath,
                $entryName,
                [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
    }
    catch {
        if (Test-Path -LiteralPath $DestinationPath) {
            Remove-Item -LiteralPath $DestinationPath -Force
        }

        throw
    }
    finally {
        $archive.Dispose()
    }
}

function Assert-ZipArchiveHasNoBackslashEntries {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem

    Assert-ExistingFile -Path $Path -Description "release archive"

    $archive = [System.IO.Compression.ZipFile]::OpenRead($Path)
    try {
        $entries = @($archive.Entries)
        if ($entries.Count -eq 0) {
            throw "Release archive validation failed: archive contains no entries."
        }

        $badEntries = @($entries | Where-Object { $_.FullName.IndexOf([char]92) -ge 0 } | Select-Object -First 5 -ExpandProperty FullName)
        if ($badEntries.Count -gt 0) {
            throw "Release archive validation failed: archive contains Windows backslash path separators: $($badEntries -join ', ')"
        }
    }
    finally {
        $archive.Dispose()
    }
}

function Invoke-DotnetRestoreWithIsolatedNuGetConfig {
    param(
        [Parameter(Mandatory = $true)]
        [string]$SolutionPath,

        [Parameter(Mandatory = $true)]
        [string]$NuGetConfigPath
    )

    $temporaryAppData = Join-Path ([System.IO.Path]::GetTempPath()) "bespoke-studio-nuget-$([System.Guid]::NewGuid().ToString("N"))"
    $temporaryNuGetDirectory = Join-Path $temporaryAppData "NuGet"
    $previousAppData = $env:APPDATA

    New-Item -ItemType Directory -Path $temporaryNuGetDirectory -Force | Out-Null
    Copy-Item -LiteralPath $NuGetConfigPath -Destination (Join-Path $temporaryNuGetDirectory "NuGet.Config") -Force

    try {
        $env:APPDATA = $temporaryAppData
        Invoke-CheckedCommand -FilePath "dotnet" -Arguments @(
            "restore",
            $SolutionPath,
            "--configfile",
            $NuGetConfigPath)
    }
    finally {
        if ($null -eq $previousAppData) {
            Remove-Item Env:APPDATA -ErrorAction SilentlyContinue
        }
        else {
            $env:APPDATA = $previousAppData
        }

        Remove-Item -LiteralPath $temporaryAppData -Recurse -Force -ErrorAction SilentlyContinue
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
$migrationScriptPath = Join-Path $migrationsPath "bespoke-studio-idempotent.sql"

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
    try {
        Invoke-CheckedCommand -FilePath "dotnet" -Arguments @("restore", "backend/BespokeStudio.sln")
    }
    catch {
        $repoNuGetConfig = Join-Path $repoRoot "backend/NuGet.Config"
        if (-not (Test-Path -LiteralPath $repoNuGetConfig -PathType Leaf)) {
            throw
        }

        Write-Warning "Default dotnet restore failed. Retrying with isolated APPDATA and repo-local NuGet config: $repoNuGetConfig"
        Invoke-DotnetRestoreWithIsolatedNuGetConfig -SolutionPath "backend/BespokeStudio.sln" -NuGetConfigPath $repoNuGetConfig
    }
}

Invoke-CheckedCommand -FilePath "dotnet" -Arguments @("build", "backend/BespokeStudio.sln", "-c", $Configuration, "--no-restore")
Invoke-CheckedCommand -FilePath "dotnet" -Arguments @("publish", $backendProject, "-c", $Configuration, "-o", $appPublishPath, "--no-build")

$wwwrootPath = Join-Path $appPublishPath "wwwroot"
New-Item -ItemType Directory -Path $wwwrootPath -Force | Out-Null
Copy-Item -Path (Join-Path $frontendDistPath "*") -Destination $wwwrootPath -Recurse -Force

Assert-ExistingFile -Path (Join-Path $appPublishPath "BespokeStudio.Api.dll") -Description "published BespokeStudio.Api.dll"
Assert-ExistingFile -Path (Join-Path $wwwrootPath "index.html") -Description "published SPA wwwroot/index.html"

Invoke-CheckedCommand -FilePath "dotnet" -Arguments @(
    "ef",
    "migrations",
    "script",
    "--idempotent",
    "--configuration",
    $Configuration,
    "--no-build",
    "--project",
    $infrastructureProject,
    "--startup-project",
    $backendProject,
    "--output",
    $migrationScriptPath)

Assert-ExistingFile -Path $migrationScriptPath -Description "idempotent migration SQL"
Assert-FileContains -Path $migrationScriptPath -Pattern "20260710120000_AddResendEmailDeliverySettings" -Description "Resend migration id"
Assert-FileContains -Path $migrationScriptPath -Pattern "EmailDeliveryResendApiKeyProtected" -Description "Resend protected API key column"
Assert-FileContains -Path $migrationScriptPath -Pattern "EmailDeliveryResendFromEmail" -Description "Resend From email column"
Assert-FileContains -Path $migrationScriptPath -Pattern "EmailDeliveryReplyToEmail" -Description "Reply-To email column"
Assert-FileDoesNotContain -Path $migrationScriptPath -Pattern "SELECT setval" -Description "unsafe SELECT setval"
Assert-FileContains -Path $migrationScriptPath -Pattern 'PERFORM setval(''"OrderReferenceSequence"''' -Description "OrderReferenceSequence PERFORM setval"
Assert-FileContains -Path $migrationScriptPath -Pattern 'PERFORM setval(''"ContactMessageReferenceSequence"''' -Description "ContactMessageReferenceSequence PERFORM setval"

New-LinuxCompatibleZipArchive -SourceDirectory $appPublishPath -DestinationPath $archivePath
Assert-ZipArchiveHasNoBackslashEntries -Path $archivePath

Write-Host "Release archive: $archivePath"
Write-Host "Published app: $appPublishPath"
Write-Host "Migration script: $migrationScriptPath"
