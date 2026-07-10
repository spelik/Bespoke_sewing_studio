#!/usr/bin/env bash
set -euo pipefail

PROJECT_ROOT="${PROJECT_ROOT:-/opt/apps/projects/bespoke-studio}"
BACKUP_ROOT="${BACKUP_ROOT:-/opt/backups/bespoke-studio}"
TIMESTAMP="$(date -u +%Y%m%d-%H%M%S)"
BACKUP_DIR="$BACKUP_ROOT/releases/$TIMESTAMP"

mkdir -p "$BACKUP_DIR"

cd "$PROJECT_ROOT"

if [ -f docker-compose.yml ]; then
  cp docker-compose.yml "$BACKUP_DIR/docker-compose.yml"
fi

if [ -d current ]; then
  tar -czf "$BACKUP_DIR/current.tar.gz" -C current .
fi

if [ -d data/uploads ]; then
  tar -czf "$BACKUP_DIR/uploads.tar.gz" -C data uploads
fi

if [ -d data/keys ]; then
  tar -czf "$BACKUP_DIR/data-protection-keys.tar.gz" -C data keys
fi

if docker ps --format '{{.Names}}' | grep -qx 'bespoke-studio-postgres'; then
  docker exec bespoke-studio-postgres pg_dump -U bespoke_studio_app -d bespoke_studio_prod -Fc -f /tmp/bespoke-studio.dump
  docker cp bespoke-studio-postgres:/tmp/bespoke-studio.dump "$BACKUP_DIR/postgresql.dump"
  docker exec bespoke-studio-postgres rm -f /tmp/bespoke-studio.dump
fi

cat > "$BACKUP_DIR/backup-metadata.json" <<EOF
{
  "createdAtUtc": "$(date -u +%Y-%m-%dT%H:%M:%SZ)",
  "projectRoot": "$PROJECT_ROOT",
  "containsCompose": $(test -f "$BACKUP_DIR/docker-compose.yml" && echo true || echo false),
  "containsCurrentRelease": $(test -f "$BACKUP_DIR/current.tar.gz" && echo true || echo false),
  "containsUploads": $(test -f "$BACKUP_DIR/uploads.tar.gz" && echo true || echo false),
  "containsDataProtectionKeys": $(test -f "$BACKUP_DIR/data-protection-keys.tar.gz" && echo true || echo false),
  "containsDatabaseDump": $(test -f "$BACKUP_DIR/postgresql.dump" && echo true || echo false)
}
EOF

echo "Backup created: $BACKUP_DIR"
