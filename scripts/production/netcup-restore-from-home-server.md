# Restore production data from old home-server to netcup

This document is intentionally migration-only. The old home-server deployment is deprecated and must not be developed further.

Old source:

- host: `home-server` / `192.168.2.202`;
- database: `bespoke_studio_prod`;
- uploads: `/var/lib/bespoke-studio/uploads`;
- Data Protection keys: `/var/lib/bespoke-studio/data-protection-keys`.

New target:

- host: netcup `prod01`;
- project root: `/opt/apps/projects/bespoke-studio`;
- uploads: `/opt/apps/projects/bespoke-studio/data/uploads`;
- Data Protection keys: `/opt/apps/projects/bespoke-studio/data/keys`;
- database container: `bespoke-studio-postgres`;
- database: `bespoke_studio_prod`;
- user: `bespoke_studio_app`.

Do not paste secrets, `.env` values, Gmail App Password or the secret health URL into chat or repo files.

## 1. Create old-server artifacts

Run on the old home-server during a maintenance window:

```bash
set -euo pipefail
workdir="/tmp/bespoke-studio-migration-$(date -u +%Y%m%d-%H%M%S)"
mkdir -p "$workdir"

pg_dump -Fc -d bespoke_studio_prod -f "$workdir/bespoke_studio_prod.dump"
tar -czf "$workdir/uploads.tar.gz" -C /var/lib/bespoke-studio uploads
tar -czf "$workdir/data-protection-keys.tar.gz" -C /var/lib/bespoke-studio data-protection-keys
sha256sum "$workdir"/* > "$workdir/SHA256SUMS.txt"
```

The DB dump contains users/admin, roles, orders, contact messages, uploaded files metadata, site settings, SMTP settings, email templates, audit log and email delivery log.

## 2. Copy artifacts to netcup

Copy the dump and archives to a protected temporary directory on netcup, for example:

```bash
scp "$workdir/"* dmitriy@159.195.196.104:/opt/backups/bespoke-studio/incoming/
```

Use the real SSH key from the operator machine. Do not commit copied artifacts.

## 3. Back up current netcup state first

Run on netcup before restore, even if the target database is expected to be empty:

```bash
cd /opt/apps/projects/bespoke-studio
bash ./scripts/production/netcup-backup.sh
```

If `scripts/production` is not present on the server, run the equivalent backup commands from [`netcup-backup.sh`](netcup-backup.sh).

## 4. Restore database/uploads/keys

Run on netcup:

```bash
set -euo pipefail
cd /opt/apps/projects/bespoke-studio

docker compose --env-file .env -f docker-compose.yml stop bespoke-studio-app

docker cp /opt/backups/bespoke-studio/incoming/bespoke_studio_prod.dump bespoke-studio-postgres:/tmp/bespoke_studio_prod.dump
docker exec bespoke-studio-postgres dropdb -U bespoke_studio_app --if-exists bespoke_studio_prod
docker exec bespoke-studio-postgres createdb -U bespoke_studio_app bespoke_studio_prod
docker exec bespoke-studio-postgres pg_restore -U bespoke_studio_app -d bespoke_studio_prod --clean --if-exists /tmp/bespoke_studio_prod.dump
docker exec bespoke-studio-postgres rm -f /tmp/bespoke_studio_prod.dump

rm -rf data/uploads/*
tar -xzf /opt/backups/bespoke-studio/incoming/uploads.tar.gz -C data

rm -rf data/keys/*
tar -xzf /opt/backups/bespoke-studio/incoming/data-protection-keys.tar.gz -C data
if [ -d data/data-protection-keys ]; then
  mv data/data-protection-keys/* data/keys/
  rmdir data/data-protection-keys
fi

chmod -R u+rwX,go-rwx data/uploads data/keys
docker compose --env-file .env -f docker-compose.yml up -d
```

If Data Protection keys are restored, old protected values and cookies may survive better. It is still acceptable if users need to sign in again.

## 5. Validate

Run:

```bash
cd /opt/apps/projects/bespoke-studio
docker compose --env-file .env -f docker-compose.yml ps -a
docker compose --env-file .env -f docker-compose.yml logs --since=10m bespoke-studio-app
curl -i "$(cat /opt/apps/caddy/oksanalogosha-health-url.txt)"
curl -I https://oksanalogosha.com/
```

Manual checks:

1. Admin login works.
2. Dashboard opens.
3. Existing orders and contact messages are visible.
4. Existing uploads are readable where expected.
5. Order form creates a new order.
6. Contact form creates a new message.
7. Upload scan shows ClamAV Clean.
8. SMTP settings are present in Admin Settings.
9. Owner/customer email delivery works.
10. Email outbox is healthy.
