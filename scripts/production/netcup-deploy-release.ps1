[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory = $true)]
    [string]$ReleaseArchive,

    [string]$MigrationScript = "publish/netcup/migrations/bespoke-studio-idempotent.sql",
    [string]$ComposeFile = "docker-compose.production.yml",
    [string]$SshKeyPath = "$env:USERPROFILE\.ssh\netcup_rs2000",
    [string]$RemoteUser = "dmitriy",
    [string]$RemoteHost = "159.195.196.104",
    [string]$ProductionHost = "oksanalogosha.com",
    [string]$RemoteRoot = "/opt/apps/projects/bespoke-studio",
    [string]$RemoteBackupRoot = "/opt/backups/bespoke-studio/releases"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Assert-ExistingFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$Description
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Deploy validation failed: missing $Description at $Path."
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
            throw "Deploy validation failed: release archive contains no entries."
        }

        $badEntries = @($entries | Where-Object { $_.FullName.IndexOf([char]92) -ge 0 } | Select-Object -First 5 -ExpandProperty FullName)
        if ($badEntries.Count -gt 0) {
            throw "Deploy validation failed: release archive contains Windows backslash path separators: $($badEntries -join ', ')"
        }
    }
    finally {
        $archive.Dispose()
    }
}

function Invoke-RemoteBashScript {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Script,

        [Parameter(Mandatory = $true)]
        [string]$Description
    )

    # Pass the multiline bash script on stdin so remote quoting is preserved.
    # Do not pass the script body as an ssh command-line argument.
    #
    # Normalize CRLF / lone CR to LF before send. Here-strings from Windows
    # PowerShell often contain CRLF; if "set -euo pipefail\r" reaches remote
    # bash, it fails with ": invalid option name". Write raw UTF-8 bytes to
    # ssh stdin (not via the PowerShell pipeline) so LF is not rewritten to CRLF.
    # Redirect only stdin: leave stdout/stderr on the console for live output and
    # to avoid pipe-buffer deadlocks from RedirectStandardOutput/Error + ReadToEnd.
    $normalizedScript = ($Script -replace "`r`n", "`n") -replace "`r", "`n"

    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = "ssh"
    $psi.Arguments = "-i `"$SshKeyPath`" $remote `"bash -s`""
    $psi.UseShellExecute = $false
    $psi.RedirectStandardInput = $true
    $psi.RedirectStandardOutput = $false
    $psi.RedirectStandardError = $false
    $psi.CreateNoWindow = $false

    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $psi
    $exitCode = -1
    try {
        $null = $process.Start()

        $utf8NoBom = New-Object System.Text.UTF8Encoding $false
        $payload = $utf8NoBom.GetBytes($normalizedScript)
        $process.StandardInput.BaseStream.Write($payload, 0, $payload.Length)
        $process.StandardInput.BaseStream.Flush()
        $process.StandardInput.Close()

        $null = $process.WaitForExit()
        $exitCode = $process.ExitCode
    }
    finally {
        if (-not $process.HasExited) {
            try { $process.Kill() } catch { }
        }
        $process.Dispose()
    }

    if ($exitCode -ne 0) {
        throw "Remote SSH command failed ($Description). Exit code: $exitCode."
    }
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "../..")
$releaseArchivePath = (Resolve-Path $ReleaseArchive).ProviderPath
$migrationScriptPath = (Resolve-Path (Join-Path $repoRoot $MigrationScript)).ProviderPath
$composePath = (Resolve-Path (Join-Path $repoRoot $ComposeFile)).ProviderPath
$serverScripts = @(
    Join-Path $repoRoot "scripts/production/netcup-backup.sh"
    Join-Path $repoRoot "scripts/production/netcup-check.sh"
) | ForEach-Object { (Resolve-Path $_).ProviderPath }
$remote = "${RemoteUser}@${RemoteHost}"
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$remoteRelease = "$RemoteRoot/releases/bespoke-studio-release-$timestamp.zip"
$remoteMigrationRelease = "$RemoteRoot/releases/bespoke-studio-idempotent-$timestamp.sql"
$remoteMigrationStable = "$RemoteRoot/releases/bespoke-studio-idempotent.sql"

if (-not $WhatIfPreference -and -not (Test-Path -LiteralPath $SshKeyPath)) {
    throw "SSH key not found: $SshKeyPath"
}

if ([string]::IsNullOrWhiteSpace($ProductionHost) -or $ProductionHost.Contains("'")) {
    throw "ProductionHost must be a non-empty host name without single quotes."
}

Assert-ExistingFile -Path $releaseArchivePath -Description "local release archive"
Assert-ExistingFile -Path $migrationScriptPath -Description "local migration SQL"
Assert-ExistingFile -Path $composePath -Description "local production compose file"
Assert-ZipArchiveHasNoBackslashEntries -Path $releaseArchivePath

$remotePrepare = @"
set -euo pipefail
mkdir -p '$RemoteRoot/current' '$RemoteRoot/releases' '$RemoteRoot/data/uploads' '$RemoteRoot/data/logs' '$RemoteRoot/data/keys' '$RemoteRoot/postgres' '$RemoteRoot/scripts/production' '$RemoteBackupRoot'
test -f '$RemoteRoot/.env'
"@

if ($PSCmdlet.ShouldProcess($remote, "prepare remote directories and verify .env")) {
    Invoke-RemoteBashScript -Script $remotePrepare -Description "prepare remote directories and verify .env"
}

if ($PSCmdlet.ShouldProcess($remote, "upload compose, release archive and migration SQL")) {
    scp -i $SshKeyPath $composePath "${remote}:$RemoteRoot/docker-compose.yml"
    scp -i $SshKeyPath $releaseArchivePath "${remote}:$remoteRelease"
    scp -i $SshKeyPath $migrationScriptPath "${remote}:$remoteMigrationRelease"
    foreach ($scriptPath in $serverScripts) {
        scp -i $SshKeyPath $scriptPath "${remote}:$RemoteRoot/scripts/production/"
    }
    ssh -i $SshKeyPath $remote "chmod +x '$RemoteRoot/scripts/production/netcup-backup.sh' '$RemoteRoot/scripts/production/netcup-check.sh'"
    if ($LASTEXITCODE -ne 0) {
        throw "Remote SSH command failed (chmod production scripts). Exit code: $LASTEXITCODE."
    }
}

$remoteDeploy = @"
set -euo pipefail
cd '$RemoteRoot'
APP_HOST='$ProductionHost'
switched=0
print_rollback_hint() {
  if [ "`$switched" = "1" ]; then
    echo "ERROR: deployment failed after current was switched." >&2
    echo "Rollback path: inspect logs, then move current to current.failed-$timestamp, move current.previous back to current, and run docker compose --env-file .env -f docker-compose.yml up -d --force-recreate bespoke-studio-app." >&2
  else
    echo "ERROR: deployment failed before current switch. Existing current was left untouched." >&2
  fi
}
trap print_rollback_hint ERR
check_local_endpoint() {
  endpoint="`$1"
  status_file="`$(mktemp)"
  if http_status="`$(curl -sS --noproxy 127.0.0.1 -o "`$status_file" -w '%{http_code}' -H "Host:`$APP_HOST" -H "X-Forwarded-Proto:https" -H "X-Forwarded-Host:`$APP_HOST" "http://127.0.0.1:5030`$endpoint")"; then
    :
  else
    curl_exit="`$?"
    rm -f "`$status_file"
    echo "Post-switch health check failed for `$endpoint: curl exit `$curl_exit, HTTP status `${http_status:-000}." >&2
    echo "Hint: local production checks through 127.0.0.1:5030 must send Host: `$APP_HOST because ASP.NET Core AllowedHosts rejects raw localhost/127.0.0.1 hostnames." >&2
    exit 25
  fi
  rm -f "`$status_file"
  case "`$http_status" in
    ''|*[!0-9]*)
      echo "Post-switch health check failed for `$endpoint: invalid HTTP status '`$http_status'." >&2
      exit 25
      ;;
  esac
  if [ "`$http_status" -lt 200 ] || [ "`$http_status" -ge 400 ]; then
    echo "Post-switch health check failed for `$endpoint: HTTP status `$http_status." >&2
    echo "Hint: verify the Host header uses `$APP_HOST and matches the backend AllowedHosts configuration." >&2
    exit 25
  fi
  echo "Post-switch check passed: `$endpoint -> HTTP `$http_status"
}
backup_dir='$RemoteBackupRoot/predeploy-$timestamp'
mkdir -p "`$backup_dir"
if [ -d current ] && [ "`$(find current -mindepth 1 -maxdepth 1 2>/dev/null | wc -l)" -gt 0 ]; then
  tar -czf "`$backup_dir/current.tar.gz" -C current .
fi
docker compose --env-file .env -f docker-compose.yml ps >/dev/null || true
if docker ps --format '{{.Names}}' | grep -qx 'bespoke-studio-postgres'; then
  docker exec bespoke-studio-postgres pg_dump -U bespoke_studio_app -d bespoke_studio_prod -Fc -f /tmp/predeploy.dump || true
  docker cp bespoke-studio-postgres:/tmp/predeploy.dump "`$backup_dir/postgresql.dump" || true
  docker exec bespoke-studio-postgres rm -f /tmp/predeploy.dump || true
fi
test -f '$remoteRelease'
test -f '$remoteMigrationRelease'

archive_entries="`$(unzip -Z1 '$remoteRelease')"
if printf '%s\n' "`$archive_entries" | grep -F '\' >/dev/null; then
  echo "Release archive contains Windows backslash path separators; refusing to deploy before DB migration." >&2
  printf '%s\n' "`$archive_entries" | grep -F '\' | head -20 >&2
  exit 22
fi

unzip -tqq '$remoteRelease'
rm -rf current.new
mkdir -p current.new
unzip -q '$remoteRelease' -d current.new
if find current.new -print | grep -F '\' >/dev/null; then
  echo "Extracted current.new contains filenames with backslashes; refusing to deploy before DB migration." >&2
  find current.new -print | grep -F '\' | head -20 >&2
  exit 23
fi
file_count="`$(find current.new -type f | wc -l | tr -d ' ')"
if [ "`$file_count" -le 0 ]; then
  echo "Extracted current.new contains no files; refusing to deploy before DB migration." >&2
  exit 24
fi
test -f current.new/BespokeStudio.Api.dll
test -f current.new/wwwroot/index.html

cat '$remoteMigrationRelease' | docker exec -i bespoke-studio-postgres sh -lc 'psql -v ON_ERROR_STOP=1 -U "`$POSTGRES_USER" -d "`$POSTGRES_DB"'
cp '$remoteMigrationRelease' '$remoteMigrationStable'
rm -rf current.previous
if [ -d current ]; then mv current current.previous; fi
mv current.new current
switched=1
docker compose --env-file .env -f docker-compose.yml up -d --force-recreate bespoke-studio-app
sleep 20
check_local_endpoint /health/live
check_local_endpoint /health/ready
check_local_endpoint /api/version
check_local_endpoint /
check_local_endpoint /admin
docker compose --env-file .env -f docker-compose.yml ps -a
docker compose --env-file .env -f docker-compose.yml logs --since=5m bespoke-studio-app
switched=0
trap - ERR
echo "Deployment completed successfully."
"@

if ($PSCmdlet.ShouldProcess($remote, "deploy release and restart compose services")) {
    Invoke-RemoteBashScript -Script $remoteDeploy -Description "deploy release and restart compose services"
}
