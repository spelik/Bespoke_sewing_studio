# Production deployment plan — Bespoke Sewing Studio

## A. Назначение

- Практический пошаговый план развертывания на сервере для `https://oksanalogosha.com`.
- **Не заменяет:**
  - [`RELEASE_READINESS_REVIEW_RU.md`](RELEASE_READINESS_REVIEW_RU.md) — Go/No-Go обзор;
  - [`PRODUCTION_GO_LIVE_RU.md`](PRODUCTION_GO_LIVE_RU.md) — day-of-launch runbook;
  - [`PRODUCTION_LAUNCH_CHECKLIST_RU.md`](PRODUCTION_LAUNCH_CHECKLIST_RU.md) — полный checklist;
  - специализированные runbooks (backup, SMTP, uploads, Data Protection, reverse proxy).
- Production domain: `https://oksanalogosha.com` (apex; `www` → apex).
- Все пути, пользователи, IP и секреты в этом документе — **placeholders/examples**.
- Перед выполнением адаптируйте шаги под конкретный сервер и secret store.

**Repo подготовлен к deploy; реальный GO требует настройки живого server environment и успешного public smoke test.**

> Current production target is netcup `prod01` + Cloudflare Full (strict) +
> Caddy + Docker Compose. Use [`PRODUCTION_DEPLOYMENT_RU.md`](PRODUCTION_DEPLOYMENT_RU.md)
> and [`DEPLOY_NETCUP_RU.md`](DEPLOY_NETCUP_RU.md) for the concrete supported
> runbook. Generic Linux/systemd/Nginx paths below are historical/reference
> examples only and are not the active deployment target.

## B. Deployment assumptions

| Компонент | Предположение |
|-----------|---------------|
| Frontend | React/Vite static files из `dist/` |
| Backend | ASP.NET Core API — `backend/src/BespokeStudio.Api/BespokeStudio.Api.csproj`, target **`net10.0`** |
| Database | PostgreSQL (отдельная production БД) |
| Uploads | Local filesystem storage; **не** public static folder |
| Data Protection | Persistent folder вне repo/publish |
| Network | Cloudflare → reverse proxy → Kestrel (Kestrel **не** в public internet) |
| SMTP | Developer-managed SMTP env vars **или** owner-managed Gmail через Admin |
| URL | Apex `https://oksanalogosha.com`; `www` redirect → apex |

OS не фиксируется как единственный вариант:

- **Linux** — systemd + Nginx/Caddy example placeholders ниже;
- **Windows** — IIS + ASP.NET Core Hosting Bundle; те же checklist-шаги, другой hosting.

## C. What must exist before deployment

- [ ] Server выбран, обновлён, SSH/RDP доступ ограничен;
- [ ] Domain `oksanalogosha.com` в Cloudflare;
- [ ] PostgreSQL установлен/доступен (managed или self-hosted);
- [ ] Reverse proxy установлен и настраивается;
- [ ] **ASP.NET Core Runtime / Hosting Bundle** для `net10.0` на server (или self-contained publish — по выбору оператора);
- [ ] Node.js/npm на **build machine** (на server не обязателен, если артефакты копируются);
- [ ] `pg_dump` / `pg_restore` доступны для backup/verify;
- [ ] Malware scanner (ClamAV или command-line scanner);
- [ ] Persistent directories (examples в разделе D);
- [ ] Secret store / secure env management выбран (не Git, не chat).

## D. Recommended directory layout examples

**Examples only.** Real paths не коммитятся в Git.

### Linux example

| Purpose | Example path |
|---------|--------------|
| Backend releases | `/opt/bespoke-studio/releases/<release-sha>/api` |
| Current backend symlink | `/opt/bespoke-studio/current` |
| Frontend static | `/var/www/oksanalogosha.com` |
| Uploads | `/var/lib/bespoke-studio/uploads` |
| Data Protection keys | `/var/lib/bespoke-studio/data-protection-keys` |
| Logs | `/var/log/bespoke-studio` |
| Backups | `/var/backups/bespoke-studio` |

### Windows example

| Purpose | Example path |
|---------|--------------|
| Backend releases | `C:\Sites\BespokeStudio\releases\<release-sha>\api` |
| Current backend | `C:\Sites\BespokeStudio\current` |
| Frontend static | `C:\Sites\oksanalogosha.com\wwwroot` |
| Uploads | `D:\BespokeStudioData\uploads` |
| Data Protection keys | `D:\BespokeStudioData\data-protection-keys` |
| Backups | `D:\ProtectedBackups\BespokeStudio` |

Uploads, Data Protection keys и backups должны быть **вне** repo, **вне** frontend static wwwroot и **вне** publish folder, который удаляется при redeploy.

## E. Local / build-machine release validation

На build machine **до** упаковки артефактов:

```powershell
cd C:\Projects\Bespoke_sewing_studio
npm.cmd run typecheck
npm.cmd run build
dotnet test backend\BespokeStudio.sln
dotnet build backend\BespokeStudio.sln
git diff --check
git status
git log --oneline -1
```

Optional PostgreSQL integration tests — **только dedicated test DB, never production**:

```powershell
# Set env vars per backend/README.md — use admin/test connection string only
dotnet test backend\BespokeStudio.sln --filter "FullyQualifiedName~PostgreSql"
```

Запишите release commit SHA в deployment decision log (раздел Q).

## F. Build artifacts

### Frontend build

```powershell
cd C:\Projects\Bespoke_sewing_studio
npm.cmd run build
```

Результат: `dist/` (static SPA + assets).

Production build env (на build machine, **не** в Git):

- `VITE_PUBLIC_SITE_URL=https://oksanalogosha.com`
- `VITE_API_BASE_URL=<production-api-base-url-or-same-origin-api>`

### Backend publish

```powershell
cd C:\Projects\Bespoke_sewing_studio
dotnet publish backend\src\BespokeStudio.Api\BespokeStudio.Api.csproj -c Release -o .\output\backend-publish
```

Результат: `output/backend-publish/`

**Важно:**

- `output/` — локальная staging folder, **не** production backup;
- не включать в artifact: `.env`, secrets, `appsettings.Production.json`, uploads, backups, Data Protection keys;
- привязать artifact к release commit SHA (имя folder / metadata).

## G. Production configuration placeholders

Задавать на server через secret store / secure env file **вне Git**. Не вставлять secrets в command history.

### Frontend (build-time)

| Variable | Placeholder / expected |
|----------|------------------------|
| `VITE_PUBLIC_SITE_URL` | `https://oksanalogosha.com` |
| `VITE_API_BASE_URL` | `<production-api-base-url-or-same-origin-api>` — **not** localhost |

### Backend (runtime)

| Variable / setting | Placeholder / expected |
|--------------------|------------------------|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ASPNETCORE_URLS` | `http://127.0.0.1:<kestrel-port>` (internal only) |
| `ConnectionStrings__BespokeStudioDb` | `<from-secret-store>` |
| `Jwt__SigningKey` | `<from-secret-store>` |
| `SeedAdmin__Email` | `<initial-admin-email>` |
| `SeedAdmin__Password` | `<from-secret-store-first-launch-only>` — сменить после launch |
| `Cors__AllowedOrigins__0` | `https://oksanalogosha.com` (if cross-origin) |
| `DataProtection__ApplicationName` | `BespokeSewingStudio` |
| `DataProtection__KeysPath` | `<persistent-path-outside-repo>` |
| `UploadStorage__RootPath` | `<persistent-uploads-root-outside-repo>` |
| `UploadSecurity__MalwareScanner__Provider` | `ClamAV` or `CommandLine` |
| `UploadSecurity__MalwareScanner__TreatScannerErrorAsRejection` | `true` |
| Email / SMTP | Per [`SMTP_PRODUCTION_RU.md`](SMTP_PRODUCTION_RU.md) |
| `ForwardedHeaders__ForwardLimit` | deployment-specific |
| `ForwardedHeaders__KnownProxies__0` or `KnownNetworks__0` | exact proxy topology |

**Warnings:**

- `Provider=Logging` — **not** production email sender;
- `Provider=Disabled` — **not** production upload scanner;
- do not put secrets in repo, screenshots, chat, or shared command logs.

## H. Database setup and migrations

1. Create production database and user **outside repo** (PostgreSQL admin tools).
2. Store connection string **only** in secret store.
3. Apply migrations **only after backup** and **only** against correct production DB.
4. **Never** use `docker-compose.postgres.yml` as production database.
5. **Do not** use `EnsureCreated` or manual schema hacks — use EF migrations only.

Prerequisite: `dotnet ef` tools available on operator machine (`dotnet tool install --global dotnet-ef` if needed).

Example placeholder (adapt; prefer env var over raw CLI password):

```powershell
# Prefer: set ConnectionStrings__BespokeStudioDb via secret store, then:
dotnet ef database update `
  --project backend\src\BespokeStudio.Infrastructure `
  --startup-project backend\src\BespokeStudio.Api
```

If connection must be passed explicitly, load from secure source — **do not paste production password into shell history**:

```powershell
dotnet ef database update `
  --project backend\src\BespokeStudio.Infrastructure `
  --startup-project backend\src\BespokeStudio.Api `
  --connection "<production-connection-string-from-secure-source>"
```

Record migration result in deployment decision log (раздел Q).

## I. Pre-deployment backup

См. [`BACKUP_RESTORE_RU.md`](BACKUP_RESTORE_RU.md) и [`scripts/production/README_RU.md`](scripts/production/README_RU.md).

1. Freeze / maintenance window if needed.
2. Backup script **dry-run** (placeholders):

```powershell
.\scripts\production\Backup-Production.ps1 `
  -BackupRoot "<protected-backup-root-outside-repo>" `
  -PostgresHost "<postgres-host>" `
  -PostgresPort 5432 `
  -PostgresDatabase "<production-db-name>" `
  -PostgresUsername "<backup-role-username>" `
  -UploadsRoot "<uploads-root-path>" `
  -DataProtectionKeysRoot "<data-protection-keys-path>" `
  -DryRun
```

3. Real backup (same parameters, **without** `-DryRun`; credentials via secure prompt/store — script does not read `.env`).
4. Verify:
   - `postgresql.dump`;
   - `postgresql.dump.list.txt` (`pg_restore --list`);
   - uploads archive (if enabled);
   - Data Protection keys archive (if enabled);
   - `backup-metadata.json` with release SHA.
5. Record backup id/path — **do not deploy without verified backup**.

## J. Server deployment sequence

Порядок выполнения на server (adapt to Linux/Windows):

1. Confirm release commit SHA and artifact folders (frontend `dist/`, backend publish).
2. Maintenance/freeze if needed.
3. **Backup** current production state (раздел I).
4. Copy backend artifact → `<releases>/<release-sha>/api`.
5. Copy frontend `dist/` → static site folder (or release folder + switch).
6. Set/update environment variables via secret store (раздел G).
7. Ensure persistent folders exist with correct permissions:
   - uploads: writable by backend process only;
   - Data Protection keys: read/write backend only;
   - logs: writable;
   - backups: restricted access.
8. **Apply DB migrations** (раздел H) — after backup.
9. Start/restart backend service (раздел K).
10. Check backend health **locally on server**:
    - `/health/live`, `/health/ready`, `/healthz`, `/readyz`, `/api/version`
11. Switch `current` symlink / IIS site / proxy upstream to new release.
12. Reload reverse proxy.
13. Run **public HTTPS smoke test** (раздел M).
14. Monitor logs, Admin Email Log / outbox (first hour).

> **Warning:** steps involving migration or backup restore can be destructive. Do not run restore commands without emergency backup and explicit decision.

## K. Backend service examples

Illustrative placeholders only — **do not commit** real unit files or env files.

### Linux systemd (concept)

```ini
# Example snippet — adapt paths and user
[Service]
User=<service-user>
WorkingDirectory=/opt/bespoke-studio/current
ExecStart=/usr/bin/dotnet /opt/bespoke-studio/current/BespokeStudio.Api.dll
EnvironmentFile=-/etc/bespoke-studio/environment  # outside repo; restricted permissions
Restart=always
```

Load secrets from `EnvironmentFile` or systemd credentials — not from Git.

### Windows IIS (concept)

- Install ASP.NET Core Hosting Bundle (`net10.0`);
- App pool: **No Managed Code**;
- Site physical path → backend publish folder;
- Environment variables via IIS / Windows env / secret store;
- Check Event Log + application logs;
- Do not store production secrets in repo or web.config committed to Git.

## L. Reverse proxy / Cloudflare sequence

См. [`REVERSE_PROXY_HTTPS_PRODUCTION_RU.md`](REVERSE_PROXY_HTTPS_PRODUCTION_RU.md).

- [ ] DNS apex → server/proxy;
- [ ] `www` → apex redirect;
- [ ] Cloudflare SSL/TLS **Full** or **Full (strict)** — **not Flexible**;
- [ ] Valid origin certificate / TLS on proxy;
- [ ] Proxy forwards: `Host`, `X-Forwarded-For`, `X-Forwarded-Proto`, `X-Forwarded-Host`;
- [ ] WebSocket upgrade for `/hubs/admin-notifications`;
- [ ] Kestrel bound to internal address only;
- [ ] `/api`, `/health`, `/healthz`, `/readyz`, `/hubs` → backend;
- [ ] SPA fallback for frontend routes;
- [ ] `/admin` not in sitemap;
- [ ] Upload storage root **not** publicly browsable;
- [ ] Do not enable HSTS until HTTPS validated end-to-end.

## M. Post-deployment smoke test

### Backend (local on server or internal)

- `GET /health/live` → 200
- `GET /health/ready` → 200 (when PostgreSQL up)
- `GET /healthz` → 200
- `GET /readyz` → 200 when DB ready
- `GET /api/version` → safe metadata, no secrets

### Public HTTPS (`https://oksanalogosha.com`)

- [ ] Home page loads;
- [ ] Direct reload: `/services`, `/portfolio`, `/order`, `/about`, `/contact`, `/privacy`, `/terms`, `/admin` (SPA, not 404);
- [ ] `/robots.txt`, `/sitemap.xml`;
- [ ] Public JSON: `/api/services`, portfolio, content, site/brand settings;
- [ ] Contact form submission;
- [ ] Order form submission;
- [ ] Clean file upload;
- [ ] Too-large upload rejected;
- [ ] Controlled EICAR test (only in safe controlled environment);
- [ ] Admin login;
- [ ] 2FA (if enabled);
- [ ] Session refresh after reload;
- [ ] Active sessions;
- [ ] SignalR admin notifications;
- [ ] Test email + Email Log / outbox health;
- [ ] Manual retry / retention UI (operational check);
- [ ] Audit Log writes;
- [ ] **OutputCache invalidation:** admin CMS change → public API/page fresh without full TTL wait.

## N. Rollback plan

**Before deployment** define and record:

| Item | Record |
|------|--------|
| Previous release folder / SHA | |
| Previous frontend static path | |
| DB rollback approach | restore-based if migrations applied |
| Backup id/path | |
| Rollback decision point | |

**Rules:**

- If migrations are destructive/non-reversible → rollback is **restore-based**; decide carefully.
- No destructive restore without fresh emergency backup.
- If app fails **before** migrations → switch proxy/symlink back to previous release.
- If app fails **after** migrations → follow [`BACKUP_RESTORE_RU.md`](BACKUP_RESTORE_RU.md).

## O. Operational monitoring after launch

First 24 hours:

- Health endpoints (`/health/live`, `/health/ready`, `/healthz`, `/readyz`);
- Backend application logs;
- Reverse proxy / Cloudflare error logs;
- Admin Dashboard readiness;
- Email outbox health (no exhausted failed, no stale pending);
- Upload scan failures;
- Storage Maintenance scan;
- Audit Log;
- Disk space: uploads, backups, logs, PostgreSQL.

## P. What not to do on production

- Do not deploy from dirty working tree;
- Do not use dev `docker-compose.postgres.yml` as production DB;
- Do not point frontend `VITE_API_BASE_URL` to localhost;
- Do not use `Provider=Logging` as production email sender;
- Do not use `Provider=Disabled` as production scanner;
- Do not store uploads under frontend static root;
- Do not store Data Protection keys in publish folder deleted on redeploy;
- Do not commit `.env`, production appsettings, backups, keys, certs;
- Do not expose Kestrel directly to internet;
- Do not enable HSTS before HTTPS validation;
- Do not run EICAR on customer paths without controlled plan;
- Do not paste production secrets into shell history or chat.

## Q. Deployment decision log template

| Field | Value |
|-------|-------|
| Date/time (UTC / local) | |
| Operator | |
| Release SHA | |
| Frontend artifact path | |
| Backend artifact path | |
| Backup id/path | |
| Migration result | |
| Health check result | |
| Public smoke test result | |
| SMTP test result | |
| Upload scanner test result | |
| Known warnings accepted | |
| **GO / NO-GO** | |
| Rollback target (SHA / path) | |

См. также release decision log в [`RELEASE_READINESS_REVIEW_RU.md`](RELEASE_READINESS_REVIEW_RU.md) §H.
