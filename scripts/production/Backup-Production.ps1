#requires -Version 5.1
<#
.SYNOPSIS
    Draft/reference production backup script for Bespoke Sewing Studio.

.DESCRIPTION
    Creates a timestamped backup folder outside the Git repository with:
    - PostgreSQL custom-format dump (postgresql.dump)
    - Optional uploads archive (uploads.zip)
    - Optional Data Protection keys archive (data-protection-keys.zip)
    - pg_restore --list verification (postgresql.dump.list.txt)
    - backup-metadata.json

    SECURITY:
    - Do NOT pass PostgreSQL passwords on the command line.
    - Provide credentials via PGPASSWORD, .pgpass, Windows Credential Manager,
      or another secret store outside Git.
    - Backups may contain personal data; store them encrypted and restrict access.
    - Never commit backup artifacts or Data Protection key archives to Git.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$BackupRoot,

    [Parameter(Mandatory = $true)]
    [string]$DatabaseName,

    [Parameter(Mandatory = $true)]
    [string]$DatabaseUser,

    [string]$DatabaseHost = "127.0.0.1",
    [int]$DatabasePort = 5432,
    [string]$UploadsPath,
    [string]$DataProtectionKeysPath,
    [int]$RetentionDays = 0,
    [switch]$DryRun,
    [switch]$SkipUploads,
    [switch]$SkipDataProtectionKeys,
    [switch]$SkipPgRestoreList,
    [switch]$ApplyRetention
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$ScriptName = 'Backup-Production.ps1'
$ScriptVersion = '1.0.0-draft'
$TimestampFolderPattern = '^\d{8}-\d{6}$'

function Write-Step {
    param([string]$Message)
    Write-Host "[backup] $Message"
}

function Write-Warn {
    param([string]$Message)
    Write-Warning "[backup] $Message"
}

function Get-RepositoryRoot {
    try {
        $root = git rev-parse --show-toplevel 2>$null
        if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($root)) {
            return (Resolve-Path -LiteralPath $root.Trim()).Path
        }
    }
    catch {
        # git may be unavailable; repo check is best-effort only.
    }

    return $null
}

function Assert-BackupRootOutsideRepository {
    param(
        [string]$RootPath,
        [string]$RepositoryRootPath
    )

    if ([string]::IsNullOrWhiteSpace($RepositoryRootPath)) {
        Write-Warn 'Git repository root could not be resolved; BackupRoot inside-repo check was skipped.'
        return
    }

    $normalizedBackupRoot = (Resolve-Path -LiteralPath $RootPath).Path.TrimEnd('\')
    $normalizedRepositoryRoot = $RepositoryRootPath.TrimEnd('\')

    if ($normalizedBackupRoot.StartsWith($normalizedRepositoryRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "BackupRoot must be outside the Git repository. Repository root: $normalizedRepositoryRoot"
    }
}

function Get-TimestampFolderName {
    return (Get-Date).ToUniversalTime().ToString('yyyyMMdd-HHmmss')
}

function Test-TimestampBackupFolderName {
    param([string]$Name)
    return $Name -match $TimestampFolderPattern
}

function Resolve-CommandPath {
    param([string]$CommandName)

    $command = Get-Command $CommandName -ErrorAction SilentlyContinue
    if (-not $command) {
        return $null
    }

    return $command.Source
}

function Invoke-PgDumpBackup {
    param(
        [string]$OutputPath,
        [switch]$WhatIfOnly
    )

    if ($WhatIfOnly) {
        Write-Step "Dry-run: would run pg_dump -> $OutputPath"
        return
    }

    $pgDumpPath = Resolve-CommandPath -CommandName 'pg_dump'
    if (-not $pgDumpPath) {
        throw 'pg_dump was not found in PATH. Install PostgreSQL client tools and retry.'
    }

    $arguments = @(
        '--host', $DatabaseHost,
        '--port', $DatabasePort.ToString(),
        '--username', $DatabaseUser,
        '--format=custom',
        '--file', $OutputPath,
        $DatabaseName
    )

    Write-Step "Running pg_dump to $OutputPath"
    & $pgDumpPath @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "pg_dump failed with exit code $LASTEXITCODE."
    }

    if (-not (Test-Path -LiteralPath $OutputPath)) {
        throw "PostgreSQL dump file was not created: $OutputPath"
    }

    if ((Get-Item -LiteralPath $OutputPath).Length -le 0) {
        throw "PostgreSQL dump file is empty: $OutputPath"
    }
}

function Invoke-PgRestoreListVerification {
    param(
        [string]$DumpPath,
        [string]$ListPath,
        [switch]$WhatIfOnly
    )

    if ($WhatIfOnly) {
        Write-Step "Dry-run: would run pg_restore --list -> $ListPath"
        return
    }

    $pgRestorePath = Resolve-CommandPath -CommandName 'pg_restore'
    if (-not $pgRestorePath) {
        throw 'pg_restore was not found in PATH. Install PostgreSQL client tools or use -SkipPgRestoreList.'
    }

    Write-Step "Verifying dump with pg_restore --list"
    & $pgRestorePath '--list' $DumpPath | Set-Content -LiteralPath $ListPath -Encoding UTF8
    if ($LASTEXITCODE -ne 0) {
        throw "pg_restore --list failed with exit code $LASTEXITCODE."
    }

    if (-not (Test-Path -LiteralPath $ListPath)) {
        throw "pg_restore list file was not created: $ListPath"
    }

    if ((Get-Item -LiteralPath $ListPath).Length -le 0) {
        throw "pg_restore list file is empty: $ListPath"
    }
}

function Invoke-DirectoryArchive {
    param(
        [string]$SourcePath,
        [string]$ArchivePath,
        [string]$Label,
        [switch]$WhatIfOnly
    )

    if ($WhatIfOnly) {
        Write-Step "Dry-run: would archive $Label from $SourcePath -> $ArchivePath"
        return
    }

    if (-not (Test-Path -LiteralPath $SourcePath)) {
        throw "$Label path does not exist: $SourcePath"
    }

    Write-Step "Creating $Label archive -> $ArchivePath"
    if (Test-Path -LiteralPath $ArchivePath) {
        Remove-Item -LiteralPath $ArchivePath -Force
    }

    Compress-Archive -Path (Join-Path $SourcePath '*') -DestinationPath $ArchivePath -Force

    if (-not (Test-Path -LiteralPath $ArchivePath)) {
        throw "$Label archive was not created: $ArchivePath"
    }

    if ((Get-Item -LiteralPath $ArchivePath).Length -le 0) {
        throw "$Label archive is empty: $ArchivePath"
    }
}

function Get-GitMetadataValue {
    param([string[]]$Arguments)

    try {
        $value = git @Arguments 2>$null
        if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($value)) {
            return $value.Trim()
        }
    }
    catch {
        # ignore
    }

    return $null
}

function New-BackupMetadataObject {
    param(
        [bool]$IncludedPostgresDump,
        [bool]$IncludedUploads,
        [bool]$IncludedDataProtectionKeys,
        [bool]$PgRestoreListCreated
    )

    return [ordered]@{
        scriptName = $ScriptName
        scriptVersion = $ScriptVersion
        createdAtUtc = (Get-Date).ToUniversalTime().ToString('o')
        gitCommit = (Get-GitMetadataValue -Arguments @('rev-parse', 'HEAD'))
        gitBranch = (Get-GitMetadataValue -Arguments @('rev-parse', '--abbrev-ref', 'HEAD'))
        databaseName = $DatabaseName
        databaseHost = $DatabaseHost
        databasePort = $DatabasePort
        includedPostgresDump = $IncludedPostgresDump
        includedUploads = $IncludedUploads
        includedDataProtectionKeys = $IncludedDataProtectionKeys
        uploadsPathProvided = -not [string]::IsNullOrWhiteSpace($UploadsPath)
        dataProtectionKeysPathProvided = -not [string]::IsNullOrWhiteSpace($DataProtectionKeysPath)
        pgRestoreListCreated = $PgRestoreListCreated
        retentionDays = $RetentionDays
        notes = @(
            'Draft/reference backup script. Passwords and secrets are intentionally omitted.',
            'Backups may contain personal data and must be stored securely.',
            'Data Protection keys archives are sensitive and must never be committed to Git.'
        )
    }
}

function Write-BackupMetadata {
    param(
        [string]$MetadataPath,
        [hashtable]$Metadata,
        [switch]$WhatIfOnly
    )

    if ($WhatIfOnly) {
        Write-Step "Dry-run: would write metadata -> $MetadataPath"
        return
    }

    $Metadata | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $MetadataPath -Encoding UTF8
}

function Invoke-RetentionPrune {
    param(
        [string]$RootPath,
        [string]$CurrentFolderName,
        [switch]$WhatIfOnly
    )

    if (-not $ApplyRetention -or $RetentionDays -le 0) {
        Write-Step 'Retention prune skipped (ApplyRetention not set or RetentionDays <= 0).'
        return @{
            Candidates = 0
            Deleted = 0
        }
    }

    if (-not (Test-Path -LiteralPath $RootPath)) {
        Write-Warn "Retention prune skipped because BackupRoot does not exist yet: $RootPath"
        return @{
            Candidates = 0
            Deleted = 0
        }
    }

    $cutoff = (Get-Date).ToUniversalTime().AddDays(-$RetentionDays)
    $candidateFolders = @()

    Get-ChildItem -LiteralPath $RootPath -Directory | ForEach-Object {
        if ($_.Name -eq $CurrentFolderName) {
            return
        }

        if (-not (Test-TimestampBackupFolderName -Name $_.Name)) {
            return
        }

        if ($_.LastWriteTimeUtc -lt $cutoff) {
            $candidateFolders += $_
        }
    }

    if ($candidateFolders.Count -eq 0) {
        Write-Step "Retention prune: no candidate folders older than $RetentionDays day(s)."
        return @{
            Candidates = 0
            Deleted = 0
        }
    }

    if ($WhatIfOnly) {
        Write-Step "Dry-run: would delete $($candidateFolders.Count) retention candidate folder(s)."
        foreach ($folder in $candidateFolders) {
            Write-Step "Dry-run retention candidate: $($folder.FullName)"
        }

        return @{
            Candidates = $candidateFolders.Count
            Deleted = 0
        }
    }

    $deleted = 0
    foreach ($folder in $candidateFolders) {
        Remove-Item -LiteralPath $folder.FullName -Recurse -Force
        $deleted++
    }

    Write-Step "Retention prune deleted $deleted folder(s)."
    return @{
        Candidates = $candidateFolders.Count
        Deleted = $deleted
    }
}

$includedPostgresDump = $false
$includedUploads = $false
$includedDataProtectionKeys = $false
$pgRestoreListCreated = $false
$retentionApplied = $false
$retentionDeletedCount = 0

try {
    if ([string]::IsNullOrWhiteSpace($BackupRoot)) {
        throw 'BackupRoot is required.'
    }

    if (-not (Test-Path -LiteralPath $BackupRoot)) {
        if ($DryRun) {
            Write-Step "Dry-run: BackupRoot does not exist yet and would be created: $BackupRoot"
        }
        else {
            New-Item -ItemType Directory -Path $BackupRoot -Force | Out-Null
        }
    }

    $resolvedBackupRoot = if ($DryRun -and -not (Test-Path -LiteralPath $BackupRoot)) {
        $BackupRoot
    }
    else {
        (Resolve-Path -LiteralPath $BackupRoot).Path
    }

    $repositoryRoot = Get-RepositoryRoot
    if (-not $DryRun) {
        Assert-BackupRootOutsideRepository -RootPath $resolvedBackupRoot -RepositoryRootPath $repositoryRoot
    }
    elseif ($repositoryRoot) {
        $normalizedBackupRoot = $resolvedBackupRoot.TrimEnd('\')
        $normalizedRepositoryRoot = $repositoryRoot.TrimEnd('\')
        if ($normalizedBackupRoot.StartsWith($normalizedRepositoryRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "BackupRoot must be outside the Git repository. Repository root: $normalizedRepositoryRoot"
        }
    }

    $timestampFolderName = Get-TimestampFolderName
    $backupFolder = Join-Path $resolvedBackupRoot $timestampFolderName
    $dumpPath = Join-Path $backupFolder 'postgresql.dump'
    $dumpListPath = Join-Path $backupFolder 'postgresql.dump.list.txt'
    $uploadsArchivePath = Join-Path $backupFolder 'uploads.zip'
    $keysArchivePath = Join-Path $backupFolder 'data-protection-keys.zip'
    $metadataPath = Join-Path $backupFolder 'backup-metadata.json'

    if (-not $DryRun -and (Test-Path -LiteralPath $backupFolder)) {
        throw "Backup folder already exists and will not be overwritten: $backupFolder"
    }

    Write-Step "Backup folder: $backupFolder"
    if ($DryRun) {
        Write-Step 'Dry-run mode enabled: no dump/archive/delete operations will be executed.'
    }
    else {
        New-Item -ItemType Directory -Path $backupFolder -Force | Out-Null
    }

    $includeUploads = -not $SkipUploads -and -not [string]::IsNullOrWhiteSpace($UploadsPath)
    $includeDataProtectionKeys = -not $SkipDataProtectionKeys -and -not [string]::IsNullOrWhiteSpace($DataProtectionKeysPath)

    if (-not $DryRun) {
        if ($includeUploads -and -not (Test-Path -LiteralPath $UploadsPath)) {
            throw "UploadsPath was provided but does not exist: $UploadsPath"
        }

        if ($includeDataProtectionKeys -and -not (Test-Path -LiteralPath $DataProtectionKeysPath)) {
            throw "DataProtectionKeysPath was provided but does not exist: $DataProtectionKeysPath"
        }
    }
    elseif ($includeUploads) {
        Write-Step "Dry-run: uploads archive planned from $UploadsPath (existence not required in dry-run)."
    }
    elseif (-not $SkipUploads -and [string]::IsNullOrWhiteSpace($UploadsPath)) {
        Write-Step 'Dry-run: uploads archive skipped because UploadsPath was not provided.'
    }

    if ($DryRun) {
        if ($includeDataProtectionKeys) {
            Write-Step "Dry-run: Data Protection keys archive planned from $DataProtectionKeysPath (existence not required in dry-run)."
            Write-Warn 'Data Protection keys archives are sensitive. Protect, encrypt, and never commit them.'
        }
        elseif (-not $SkipDataProtectionKeys -and [string]::IsNullOrWhiteSpace($DataProtectionKeysPath)) {
            Write-Step 'Dry-run: Data Protection keys archive skipped because DataProtectionKeysPath was not provided.'
        }

        Invoke-PgDumpBackup -OutputPath $dumpPath -WhatIfOnly
        $includedPostgresDump = $true

        if (-not $SkipPgRestoreList) {
            Invoke-PgRestoreListVerification -DumpPath $dumpPath -ListPath $dumpListPath -WhatIfOnly
            $pgRestoreListCreated = $true
        }
        else {
            Write-Step 'Dry-run: pg_restore --list verification skipped.'
        }

        if ($includeUploads) {
            Invoke-DirectoryArchive -SourcePath $UploadsPath -ArchivePath $uploadsArchivePath -Label 'uploads' -WhatIfOnly
            $includedUploads = $true
        }

        if ($includeDataProtectionKeys) {
            Invoke-DirectoryArchive -SourcePath $DataProtectionKeysPath -ArchivePath $keysArchivePath -Label 'Data Protection keys' -WhatIfOnly
            $includedDataProtectionKeys = $true
        }

        $metadata = New-BackupMetadataObject `
            -IncludedPostgresDump $includedPostgresDump `
            -IncludedUploads $includedUploads `
            -IncludedDataProtectionKeys $includedDataProtectionKeys `
            -PgRestoreListCreated $pgRestoreListCreated
        Write-BackupMetadata -MetadataPath $metadataPath -Metadata $metadata -WhatIfOnly

        $retentionResult = Invoke-RetentionPrune -RootPath $resolvedBackupRoot -CurrentFolderName $timestampFolderName -WhatIfOnly
        if ($ApplyRetention -and $RetentionDays -gt 0) {
            $retentionApplied = $true
        }
    }
    else {
        Invoke-PgDumpBackup -OutputPath $dumpPath
        $includedPostgresDump = $true

        if (-not $SkipPgRestoreList) {
            Invoke-PgRestoreListVerification -DumpPath $dumpPath -ListPath $dumpListPath
            $pgRestoreListCreated = $true
        }
        else {
            Write-Step 'pg_restore --list verification skipped.'
        }

        if ($includeUploads) {
            Invoke-DirectoryArchive -SourcePath $UploadsPath -ArchivePath $uploadsArchivePath -Label 'uploads'
            $includedUploads = $true
        }
        elseif (-not $SkipUploads) {
            Write-Step 'Uploads archive skipped because UploadsPath was not provided.'
        }
        else {
            Write-Step 'Uploads archive skipped by -SkipUploads.'
        }

        if ($includeDataProtectionKeys) {
            Write-Warn 'Data Protection keys archives are sensitive. Protect, encrypt, and never commit them.'
            Invoke-DirectoryArchive -SourcePath $DataProtectionKeysPath -ArchivePath $keysArchivePath -Label 'Data Protection keys'
            $includedDataProtectionKeys = $true
        }
        elseif (-not $SkipDataProtectionKeys) {
            Write-Step 'Data Protection keys archive skipped because DataProtectionKeysPath was not provided.'
        }
        else {
            Write-Step 'Data Protection keys archive skipped by -SkipDataProtectionKeys.'
        }

        $metadata = New-BackupMetadataObject `
            -IncludedPostgresDump $includedPostgresDump `
            -IncludedUploads $includedUploads `
            -IncludedDataProtectionKeys $includedDataProtectionKeys `
            -PgRestoreListCreated $pgRestoreListCreated
        Write-BackupMetadata -MetadataPath $metadataPath -Metadata $metadata

        $retentionResult = Invoke-RetentionPrune -RootPath $resolvedBackupRoot -CurrentFolderName $timestampFolderName
        if ($ApplyRetention -and $RetentionDays -gt 0) {
            $retentionApplied = $true
            $retentionDeletedCount = $retentionResult.Deleted
        }
    }

    Write-Step 'Backup summary:'
    Write-Step "  folder: $backupFolder"
    Write-Step "  postgres dump: $(if ($includedPostgresDump) { 'yes' } else { 'no' })"
    Write-Step "  pg_restore list: $(if ($pgRestoreListCreated) { 'yes' } else { 'no' })"
    Write-Step "  uploads archive: $(if ($includedUploads) { 'yes' } else { 'no' })"
    Write-Step "  data protection keys archive: $(if ($includedDataProtectionKeys) { 'yes' } else { 'no' })"
    Write-Step "  retention applied: $(if ($retentionApplied) { "yes ($retentionDeletedCount deleted)" } else { 'no' })"

    if ($DryRun) {
        Write-Step 'Dry-run completed successfully.'
    }
    else {
        Write-Step 'Backup completed successfully.'
    }
}
catch {
    Write-Error $_.Exception.Message
    exit 1
}
