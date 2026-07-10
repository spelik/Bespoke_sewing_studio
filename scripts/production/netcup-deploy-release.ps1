[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory = $true)]
    [string]$ReleaseArchive,

    [string]$ComposeFile = "docker-compose.production.yml",
    [string]$SshKeyPath = "$env:USERPROFILE\.ssh\netcup_rs2000",
    [string]$RemoteUser = "dmitriy",
    [string]$RemoteHost = "159.195.196.104",
    [string]$RemoteRoot = "/opt/apps/projects/bespoke-studio",
    [string]$RemoteBackupRoot = "/opt/backups/bespoke-studio/releases"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "../..")
$releaseArchivePath = Resolve-Path $ReleaseArchive
$composePath = Resolve-Path (Join-Path $repoRoot $ComposeFile)
$serverScripts = @(
    Join-Path $repoRoot "scripts/production/netcup-backup.sh"
    Join-Path $repoRoot "scripts/production/netcup-check.sh"
) | ForEach-Object { Resolve-Path $_ }
$remote = "${RemoteUser}@${RemoteHost}"
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$remoteRelease = "$RemoteRoot/releases/bespoke-studio-release-$timestamp.zip"

if (-not (Test-Path -LiteralPath $SshKeyPath)) {
    throw "SSH key not found: $SshKeyPath"
}

$remotePrepare = @"
set -euo pipefail
mkdir -p '$RemoteRoot/current' '$RemoteRoot/releases' '$RemoteRoot/data/uploads' '$RemoteRoot/data/logs' '$RemoteRoot/data/keys' '$RemoteRoot/postgres' '$RemoteRoot/scripts/production' '$RemoteBackupRoot'
test -f '$RemoteRoot/.env'
"@

if ($PSCmdlet.ShouldProcess($remote, "prepare remote directories and verify .env")) {
    ssh -i $SshKeyPath $remote $remotePrepare
}

if ($PSCmdlet.ShouldProcess($remote, "upload compose and release archive")) {
    scp -i $SshKeyPath $composePath "${remote}:$RemoteRoot/docker-compose.yml"
    scp -i $SshKeyPath $releaseArchivePath "${remote}:$remoteRelease"
    foreach ($scriptPath in $serverScripts) {
        scp -i $SshKeyPath $scriptPath "${remote}:$RemoteRoot/scripts/production/"
    }
    ssh -i $SshKeyPath $remote "chmod +x '$RemoteRoot/scripts/production/netcup-backup.sh' '$RemoteRoot/scripts/production/netcup-check.sh'"
}

$remoteDeploy = @"
set -euo pipefail
cd '$RemoteRoot'
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
rm -rf current.new
mkdir -p current.new
unzip -q '$remoteRelease' -d current.new
rm -rf current.previous
if [ -d current ]; then mv current current.previous; fi
mv current.new current
docker compose --env-file .env -f docker-compose.yml up -d
sleep 20
docker compose --env-file .env -f docker-compose.yml ps -a
docker compose --env-file .env -f docker-compose.yml logs --since=5m bespoke-studio-app
"@

if ($PSCmdlet.ShouldProcess($remote, "deploy release and restart compose services")) {
    ssh -i $SshKeyPath $remote $remoteDeploy
}
