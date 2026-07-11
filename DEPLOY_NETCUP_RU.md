# Bespoke Sewing Studio — netcup deploy runbook

Этот runbook описывает новую production-схему `netcup + Caddy + Docker Compose`. Старый home-server deployment deprecated и не должен развиваться дальше. Старый сервер используется только как источник production data migration.

## A. Server layout

Создать на netcup:

```bash
sudo mkdir -p /opt/apps/projects/bespoke-studio/current
sudo mkdir -p /opt/apps/projects/bespoke-studio/data/uploads
sudo mkdir -p /opt/apps/projects/bespoke-studio/data/logs
sudo mkdir -p /opt/apps/projects/bespoke-studio/data/keys
sudo mkdir -p /opt/apps/projects/bespoke-studio/postgres
sudo mkdir -p /opt/backups/bespoke-studio/db
sudo mkdir -p /opt/backups/bespoke-studio/releases
sudo mkdir -p /opt/backups/bespoke-studio/configs
```

Project root on server:

```bash
cd /opt/apps/projects/bespoke-studio
```

The compose file is copied to:

```text
/opt/apps/projects/bespoke-studio/docker-compose.yml
```

The release content is deployed to:

```text
/opt/apps/projects/bespoke-studio/current
```

## B. Required server `.env`

Create `/opt/apps/projects/bespoke-studio/.env` manually on the server. Do not commit it and do not paste it into chat.

Required variables:

```bash
BESPOKE_STUDIO_DB_PASSWORD=<strong-random-password>
BESPOKE_STUDIO_JWT_SIGNING_KEY=<at-least-32-characters-random-secret>
```

Do not store Gmail App Password in `.env`. Owner-managed Gmail SMTP is configured only through Admin -> Settings.

## C. Production compose

Use [`docker-compose.production.yml`](docker-compose.production.yml). On the server it should be deployed as `docker-compose.yml`.

Important details:

- PostgreSQL image: `postgres:18-alpine`;
- PostgreSQL volume: `./postgres:/var/lib/postgresql`;
- PostgreSQL database: `bespoke_studio_prod`;
- PostgreSQL user: `bespoke_studio_app`;
- connection string key: `ConnectionStrings__BespokeStudioDb`;
- connection string includes `SSL Mode=Disable;GSS Encryption Mode=Disable`;
- app image: `mcr.microsoft.com/dotnet/aspnet:10.0`;
- app command: `dotnet BespokeStudio.Api.dll`;
- app binding: `127.0.0.1:5030:8080`;
- app volumes: `./current:/app:ro`, uploads/logs/keys under `./data`;
- ClamAV container: `bespoke-studio-clamav`, image `clamav/clamav:stable`;
- forwarded headers are limited to one hop and the Docker private network
  (`ForwardedHeaders__KnownNetworks__0=172.16.0.0/12` by default); verify the
  actual external `web` network subnet on netcup and tighten it if a fixed
  narrower subnet is configured;
- app joins both `bespoke_studio_internal` and external `web` network.

## D. Build release locally on Windows

From repo root:

```powershell
.\scripts\production\netcup-build-release.ps1
```

The script:

- runs `npm.cmd install`;
- builds the frontend with `VITE_API_BASE_URL=/api` and `VITE_PUBLIC_SITE_URL=https://oksanalogosha.com`;
- runs `dotnet restore`;
- runs `dotnet build -c Release`;
- runs `dotnet publish` for `backend/src/BespokeStudio.Api/BespokeStudio.Api.csproj`;
- copies `dist/` into published `wwwroot/`;
- generates an EF Core idempotent migration script with the selected build configuration;
- validates that the migration script contains
  `20260710120000_AddResendEmailDeliverySettings` and the Resend/Reply-To
  `SiteSettings` columns, contains `PERFORM setval(...)` for both reference
  sequences, and does not contain invalid `SELECT setval(...)` inside the
  PostgreSQL `DO` block;
- validates that the published app contains `BespokeStudio.Api.dll` and
  `wwwroot/index.html`;
- creates a Linux-compatible release zip under `publish/netcup/` with `/` path
  separators only;
- validates that the generated zip has no Windows `\` separators in entry names.

## E. Deploy release to netcup

Example from Windows:

```powershell
.\scripts\production\netcup-deploy-release.ps1 `
  -ReleaseArchive .\publish\netcup\bespoke-studio-release.zip `
  -SshKeyPath "$env:USERPROFILE\.ssh\netcup_rs2000" `
  -RemoteUser dmitriy `
  -RemoteHost 159.195.196.104
```

The deploy script does not create secrets. Before running it, ensure the server `.env` exists.
It uploads the generated idempotent SQL from
`publish/netcup/migrations/bespoke-studio-idempotent.sql`, creates predeploy
backups, validates the release archive on the server, extracts it into a clean
`current.new`, validates `current.new` (`BespokeStudio.Api.dll`,
`wwwroot/index.html`, non-zero file count and no backslash filenames), and only
then applies the SQL through the `bespoke-studio-postgres` container with
`psql -v ON_ERROR_STOP=1`.

The order is intentional:

1. upload archive and SQL;
2. validate/test/extract the archive into `current.new`;
3. validate `current.new`;
4. apply migration SQL;
5. switch `current.new` to `current`;
6. recreate `bespoke-studio-app`;
7. run local health checks.

If archive validation or extraction fails, deployment stops before DB migration
and the existing `current` stays untouched. If SQL fails, deployment stops before
the application directory is swapped. If a failure happens after the switch, the
script prints the rollback path using `current.previous`.

## F. Caddy

Add a Caddy block based on [`scripts/production/netcup-caddy.example.caddy`](scripts/production/netcup-caddy.example.caddy).

Generate a private health token only on the server and store only the resulting URL there:

```bash
token="$(openssl rand -hex 24)"
printf 'https://oksanalogosha.com/health-%s\n' "$token" | sudo tee /opt/apps/caddy/oksanalogosha-health-url.txt >/dev/null
sudo chmod 600 /opt/apps/caddy/oksanalogosha-health-url.txt
```

Do not write the actual token or URL into Git or documentation.

Public endpoints must behave as:

- `https://oksanalogosha.com/health` -> `404`;
- `https://oksanalogosha.com/health/ready` -> `404` or unavailable publicly;
- secret health URL from `/opt/apps/caddy/oksanalogosha-health-url.txt` -> proxied to internal `/health/ready`.

## G. Data migration from old home-server

Use [`scripts/production/netcup-restore-from-home-server.md`](scripts/production/netcup-restore-from-home-server.md).

Migration includes:

- production PostgreSQL database `bespoke_studio_prod`;
- admin users/roles/sessions;
- orders/contact messages;
- uploaded file metadata;
- Site Settings / SMTP settings / templates;
- audit log and email delivery log;
- uploads from `/var/lib/bespoke-studio/uploads`;
- Data Protection keys from `/var/lib/bespoke-studio/data-protection-keys`.

Always back up the current netcup DB before restoring, even if it is expected to be empty.

## H. Checks

On server:

```bash
cd /opt/apps/projects/bespoke-studio
docker compose ps -a
docker compose logs --since=5m bespoke-studio-app
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
```

Expected containers:

```text
bespoke-studio-app       Up      127.0.0.1:5030->8080/tcp
bespoke-studio-postgres  Up      5432/tcp
bespoke-studio-clamav    Up      healthy
caddy                    Up      80/443
```

Public checks:

```bash
curl -I https://oksanalogosha.com/
curl -I https://www.oksanalogosha.com/
curl -I https://oksanalogosha.com/health
curl -i "$(cat /opt/apps/caddy/oksanalogosha-health-url.txt)"
curl -i -H "Host: oksanalogosha.com" -H "X-Forwarded-Proto: https" -H "X-Forwarded-Host: oksanalogosha.com" http://127.0.0.1:5030/api/version
```

Manual smoke tests after deploy:

1. Home page opens.
2. Admin login works.
3. Admin Dashboard opens.
4. Order form creates an order.
5. Contact form creates a message.
6. File upload works.
7. Upload scan shows ClamAV Clean.
8. Owner email notification is delivered.
9. Customer confirmation email is delivered, if enabled.
10. Email outbox is healthy.
11. SignalR live updates connect.
12. Fresh logs contain no critical `ERR`/`WRN` entries.

## I. Troubleshooting

### `oksanalogosha.com` returns 502

- `docker compose ps`;
- `docker compose logs --since=10m bespoke-studio-app`;
- Caddy logs;
- check Docker network `web`;
- check app directly: `curl -i http://127.0.0.1:5030/api/version`.

### PostgreSQL does not start

- verify `./postgres:/var/lib/postgresql`;
- check `docker compose logs bespoke-studio-postgres`;
- verify `.env` contains `BESPOKE_STUDIO_DB_PASSWORD`.

### `libgssapi_krb5.so.2` / gssapi error

- verify the connection string includes `GSS Encryption Mode=Disable`.

### Upload scan does not work

- check `bespoke-studio-clamav` health/logs;
- verify `UploadSecurity__MalwareScanner__Provider=ClamAV`;
- check uploaded file metadata: `ScanProvider`, `ScanStatus`, `ScanMessage`.

### Emails do not send

- check Admin -> Settings;
- check Email Log / `EmailOutboxMessages`;
- check `EmailDeliveryLogEntries`;
- do not request or paste Gmail App Password in chat.

### Redirect goes to `http://`

- verify Caddy forwards `X-Forwarded-Proto` and `X-Forwarded-Host`;
- verify backend forwarded headers are active before auth/routing;
- run the local forwarded header curl from section H.

### `www` does not work

- verify Cloudflare `www` CNAME;
- verify Caddy site block includes both `oksanalogosha.com` and `www.oksanalogosha.com`.

## J. SignalR and logs

SignalR may pass `access_token` in the query string for `/hubs/admin-notifications`. Do not enable persistent access logs that store sensitive query strings for hub URLs. If Caddy access logs are enabled, configure them so JWT/access tokens do not persist in log files.
