# Release readiness review — Bespoke Sewing Studio

## A. Назначение

- Это финальная проверка перед production deploy.
- Документ **не заменяет** подробные runbooks, а собирает итоговый Go/No-Go обзор.
- Production domain: `https://oksanalogosha.com`.
- Repo-level review **не означает** автоматический production GO. Реальный GO возможен
  только после выполнения production environment checks и public HTTPS smoke test.

**Repo is prepared for deployment; final GO requires live environment verification.**

После readiness review и записи Go/No-Go decision log оператор выполняет
[`PRODUCTION_DEPLOYMENT_PLAN_RU.md`](PRODUCTION_DEPLOYMENT_PLAN_RU.md) — практический
server execution checklist (build artifacts, env placeholders, migrations, backup,
deployment sequence, smoke test, rollback).

Связанные документы:

- [`PRODUCTION_GO_LIVE_RU.md`](PRODUCTION_GO_LIVE_RU.md) — day-of-launch runbook;
- [`PRODUCTION_DEPLOYMENT_PLAN_RU.md`](PRODUCTION_DEPLOYMENT_PLAN_RU.md) — server deployment / execution checklist;
- [`PRODUCTION_LAUNCH_CHECKLIST_RU.md`](PRODUCTION_LAUNCH_CHECKLIST_RU.md) — подробный checklist;
- [`BACKUP_RESTORE_RU.md`](BACKUP_RESTORE_RU.md), [`SMTP_PRODUCTION_RU.md`](SMTP_PRODUCTION_RU.md),
  [`UPLOADS_PRODUCTION_RU.md`](UPLOADS_PRODUCTION_RU.md),
  [`DATA_PROTECTION_PRODUCTION_RU.md`](DATA_PROTECTION_PRODUCTION_RU.md),
  [`REVERSE_PROXY_HTTPS_PRODUCTION_RU.md`](REVERSE_PROXY_HTTPS_PRODUCTION_RU.md),
  [`scripts/production/README_RU.md`](scripts/production/README_RU.md).

## B. Итоговый статус

| Область | Статус | Комментарий |
|--------|--------|-------------|
| Repository code/build/test readiness | **Ready** (после последней validation) | Код, docs, unit tests и CI foundation в repo; перед GO перезапустить validation commands (раздел G). |
| Production configuration | **Operator action required** | Env/secrets задаются только на сервере / в secret store, не в Git. |
| DNS / Cloudflare / HTTPS | **Operator action required** | Apex `oksanalogosha.com`, www→apex, SSL mode Full, reverse proxy. |
| PostgreSQL production DB | **Operator action required** | Отдельная production БД, migrations после backup, connection string из secret store. |
| Data Protection keys | **Operator action required** | Persistent `KeysPath`, backup, permissions. |
| Upload storage + scanner | **Operator action required** | Writable `UploadStorage__RootPath`, scanner не `Disabled`, EICAR controlled test. |
| SMTP / email sender | **Operator action required** | Выбранная strategy, test email, SPF/DKIM/DMARC; `Provider=Logging` не sender. |
| Backup / restore rehearsal | **Operator action required** | DB + uploads + keys, `pg_restore --list`, rehearsal на staging. |
| Final public HTTPS smoke test | **Operator action required** | Все public routes, admin, Order/Contact/upload/email после deploy. |

## C. Что уже готово в repository

- **Production domain / SEO:** canonical `https://oksanalogosha.com` в `public/robots.txt`,
  `public/sitemap.xml`, `.env.example`; admin routes не в sitemap.
- **Production runbooks:**
  - [`PRODUCTION_GO_LIVE_RU.md`](PRODUCTION_GO_LIVE_RU.md)
  - [`PRODUCTION_LAUNCH_CHECKLIST_RU.md`](PRODUCTION_LAUNCH_CHECKLIST_RU.md)
  - [`SMTP_PRODUCTION_RU.md`](SMTP_PRODUCTION_RU.md)
  - [`UPLOADS_PRODUCTION_RU.md`](UPLOADS_PRODUCTION_RU.md)
  - [`DATA_PROTECTION_PRODUCTION_RU.md`](DATA_PROTECTION_PRODUCTION_RU.md)
  - [`REVERSE_PROXY_HTTPS_PRODUCTION_RU.md`](REVERSE_PROXY_HTTPS_PRODUCTION_RU.md)
  - [`BACKUP_RESTORE_RU.md`](BACKUP_RESTORE_RU.md)
  - [`scripts/production/README_RU.md`](scripts/production/README_RU.md)
- **Security / auth:** baseline security headers, rate limits, JWT + refresh sessions,
  optional TOTP 2FA, audit log, auth invalidation on password change / lockout.
- **Email outbox:** automatic retry/backoff, manual retry (Admin Email Log),
  monitoring summary, retention cleanup (worker disabled by default).
- **Uploads:** quarantine, signature/magic-bytes, malware scanner abstraction,
  `IUploadStorage` / local adapter, Storage Maintenance, deletion outbox worker.
- **Backup:** draft/reference [`scripts/production/Backup-Production.ps1`](scripts/production/Backup-Production.ps1).
- **Tests:** foundation unit tests; opt-in PostgreSQL integration tests (Task 80).
- **Public content cache:** OutputCache TTL 60 s + tag invalidation after admin CMS
  mutations (Task 81).
- **Admin UX:** operational Email Log polish — global vs page stats, retry/retention
  clarity (Task 82).
- **CI:** `.github/workflows/ci.yml` — frontend typecheck/build + backend build/tests.

## D. Обязательные Go/No-Go blockers

| Area | Required before GO | How to verify | Runbook |
|------|-------------------|---------------|---------|
| Git / release artifact | Clean tree, pushed release commit, SHA recorded | `git status`, `git log --oneline -1`, CI green for SHA | [`PRODUCTION_GO_LIVE_RU.md`](PRODUCTION_GO_LIVE_RU.md) §B, §I |
| Frontend validation | typecheck + production build | `npm.cmd run typecheck`, `npm.cmd run build` | This doc §G |
| Backend validation | tests + build | `dotnet test backend\BespokeStudio.sln`, `dotnet build backend\BespokeStudio.sln` | This doc §G |
| Optional PostgreSQL integration tests | Recommended on dedicated test DB, **never production** | Env vars + `--filter "FullyQualifiedName~PostgreSql"` | [`backend/README.md`](backend/README.md) |
| Production PostgreSQL | DB exists, backup before migrations, migrations applied | Connection from secret store; `dotnet ef database update` on server | [`BACKUP_RESTORE_RU.md`](BACKUP_RESTORE_RU.md) |
| Data Protection | `ApplicationName=BespokeSewingStudio`, persistent `KeysPath`, backed up | Startup in Production; keys folder in backup inventory | [`DATA_PROTECTION_PRODUCTION_RU.md`](DATA_PROTECTION_PRODUCTION_RU.md) |
| Upload storage | `UploadStorage__RootPath` persistent, writable, not public static dir | Path outside repo; included in backup | [`UPLOADS_PRODUCTION_RU.md`](UPLOADS_PRODUCTION_RU.md) |
| Malware scanner | Production scanner not `Disabled`; EICAR controlled rejection | Admin Order upload test; scanner logs | [`UPLOADS_PRODUCTION_RU.md`](UPLOADS_PRODUCTION_RU.md) |
| SMTP / email | Sender configured; `Provider=Logging` **not** production sender; test email | Admin test email; Email Log `Sent`; DNS SPF/DKIM/DMARC | [`SMTP_PRODUCTION_RU.md`](SMTP_PRODUCTION_RU.md) |
| Reverse proxy / HTTPS | Cloudflare + proxy; no Flexible SSL; www→apex; forwarded headers; SignalR WS | Public HTTPS smoke; `/health/*`, `/api/version` | [`REVERSE_PROXY_HTTPS_PRODUCTION_RU.md`](REVERSE_PROXY_HTTPS_PRODUCTION_RU.md) |
| Backup / restore | Full backup + `pg_restore --list`; restore rehearsal | [`scripts/production/Backup-Production.ps1`](scripts/production/Backup-Production.ps1) or manual procedure | [`BACKUP_RESTORE_RU.md`](BACKUP_RESTORE_RU.md) |
| Public HTTPS smoke test | Routes, forms, upload, email, admin/2FA/session, OutputCache freshness | [`PRODUCTION_GO_LIVE_RU.md`](PRODUCTION_GO_LIVE_RU.md) §G | Same |

Если **хотя бы один** blocker не выполнен — **NO-GO**.

## E. Static grep audit

Перед GO выполните grep-команды ниже. **Не удаляйте** expected dev/test placeholders
из repo без понимания контекста.

```powershell
cd C:\Projects\Bespoke_sewing_studio
git grep -n "replace-with"
git grep -n "your-production-domain"
git grep -n "localhost"
git grep -n "127.0.0.1"
git grep -n "example.com"
git grep -n "Provider=Logging"
git grep -n "Provider=Disabled"
git grep -n "BEGIN PRIVATE KEY"
git grep -n "password="
git grep -n "Password="
git grep -n "appsettings.Production"
git grep -n ".env.production"
git grep -n ".env.local"
```

### Классификация expected findings

| Pattern | Allowed? | Notes | Blocker if… |
|---------|----------|-------|-------------|
| `replace-with` | Yes (dev/docs) | Local user-secrets examples, `.http` local password placeholder | Appears in production env / tracked production config |
| `your-production-domain` | **Should not appear** | — | Any match in repo before GO |
| `localhost` | Yes (dev/docs) | README dev URLs, Vite dev, docker docs, reverse proxy internal examples | Production `VITE_API_BASE_URL` or public CORS points to localhost |
| `127.0.0.1` | Yes (dev/docs) | `appsettings.Development.json`, docker-compose, local health examples | Production connection strings or proxy trust only loopback incorrectly |
| `example.com` | Yes (tests/examples) | Seed admin email in docs, `.http` samples, test fixtures | Used as real production sender/recipient domain |
| `Provider=Logging` | Yes (docs warnings) | Documented as dev/local SMTP fallback only | Set on production backend as email sender |
| `Provider=Disabled` | Yes (docs warnings) | Documented as dev/local upload scanner fallback | Set on production malware scanner |
| `appsettings.Production` | Yes (prohibition docs) | Listed in «do not commit/create» runbooks | File exists in Git or on server without secret store |
| `.env.production` / `.env.local` | Yes (prohibition docs) | `.env.local` in `.gitignore`; docs warn not to commit | Real secrets committed or deployed from Git |
| `BEGIN PRIVATE KEY` | **No** | TLS/key material must never be in repo | Any match |
| `password=` (lowercase) | **No** | — | Any match (use secret store / env on server) |
| `Password=` | Yes (limited) | `appsettings.Development.json` docker dev password; SMTP placeholder docs | Production secret in tracked files |

**Audit note (Task 83):** `your-production-domain` and `BEGIN PRIVATE KEY` not found;
`password=` not found. `Password=` appears only in dev `appsettings.Development.json`
and documentation placeholders. `localhost` / `127.0.0.1` / `example.com` / `replace-with`
appear in expected dev, test and documentation contexts only.

## F. Production config variables checklist

Задавать **только** на сервере / в secret store. Реальные значения и secrets **не**
хранить в Git.

### Frontend

| Variable | Expected |
|----------|----------|
| `VITE_PUBLIC_SITE_URL` | `https://oksanalogosha.com` |
| `VITE_API_BASE_URL` | Production API base URL (same origin or explicit HTTPS API), **not** localhost |

### Backend

| Variable / setting | Expected |
|------------------|----------|
| `ConnectionStrings__BespokeStudioDb` | From secret store only |
| `Jwt__SigningKey` | From secret store only (≥32 chars) |
| `SeedAdmin__Email` | Initial admin strategy (rotate if needed) |
| `SeedAdmin__Password` | Initial secret store only; change after launch |
| `Cors__AllowedOrigins__0` | `https://oksanalogosha.com` if cross-origin |
| `DataProtection__ApplicationName` | `BespokeSewingStudio` |
| `DataProtection__KeysPath` | Persistent path outside repo/publish |
| `UploadStorage__RootPath` | Persistent uploads root outside repo/publish |
| `UploadSecurity__MalwareScanner__Provider` | `ClamAV` or `CommandLine` (not `Disabled`) |
| `UploadSecurity__MalwareScanner__TreatScannerErrorAsRejection` | `true` (production recommendation) |
| Email provider | Per [`SMTP_PRODUCTION_RU.md`](SMTP_PRODUCTION_RU.md) (`Smtp` or `GmailSmtp`) |
| `ForwardedHeaders__KnownProxies` / `KnownNetworks` / `ForwardLimit` | Exact deployment topology |

## G. Final validation command set

Обязательно перед GO:

```powershell
cd C:\Projects\Bespoke_sewing_studio
npm.cmd run typecheck
npm.cmd run build
dotnet test backend\BespokeStudio.sln
dotnet build backend\BespokeStudio.sln
git diff --check
git status --ignored --short AGENTS_RU.md
git status --short backend/src/BespokeStudio.Infrastructure/Persistence/Migrations
git status --short
```

Optional (recommended, **dedicated test PostgreSQL only — never production**):

```powershell
docker compose -f docker-compose.postgres.yml up -d postgres
$env:BESPOKESTUDIO_RUN_POSTGRES_INTEGRATION_TESTS = "true"
$env:BESPOKESTUDIO_POSTGRES_ADMIN_CONNECTION_STRING = "<admin-test-postgres-connection-string>"
dotnet test backend\BespokeStudio.sln --filter "FullyQualifiedName~PostgreSql"
Remove-Item Env:\BESPOKESTUDIO_RUN_POSTGRES_INTEGRATION_TESTS -ErrorAction SilentlyContinue
Remove-Item Env:\BESPOKESTUDIO_POSTGRES_ADMIN_CONNECTION_STRING -ErrorAction SilentlyContinue
```

## H. Release decision log template

| Field | Value |
|-------|-------|
| Date/time (UTC / local) | |
| Release commit SHA | |
| Operator | |
| Validation commands result | typecheck / build / dotnet test / dotnet build |
| Backup id/path | |
| `pg_restore --list` checked | Yes / No |
| Restore rehearsal result | Pass / Fail / N/A |
| SMTP test result | Pass / Fail |
| Upload scanner test result | Pass / Fail |
| Public smoke test result | Pass / Fail |
| Known warnings accepted? | Yes / No — list |
| **GO / NO-GO decision** | |
| Rollback artifact / commit | |

## I. Warnings / accepted limitations

- Backup script is **draft/reference**; encryption, offsite upload and scheduling
  automation remain future work.
- Email outbox **retention worker** disabled by default until owner confirms retention
  policy (`EmailOutboxRetention:WorkerEnabled=false`).
- PostgreSQL integration tests are **opt-in** and skipped by default in CI/dev.
- External monitoring/alerting (exhausted outbox, disk, backup failures) is future
  unless separately configured by operator.
- **Object storage adapter** is future; current production strategy is local
  filesystem storage with backups.
- **CDN / reverse-proxy cache** beyond backend OutputCache (60 s + tag invalidation)
  requires separate reviewed policy.
- Admin UI is desktop-oriented; larger mobile redesign is future if needed.

## J. What must NOT be committed

- `.env`, `.env.local`, `.env.production`;
- `appsettings.Production.json`, `appsettings.Staging.json`;
- backup artifacts: `.dump`, `.sql`, `.backup`, `.bak`, `.zip`, `.tar.gz`, `.7z`;
- Data Protection key XML files;
- TLS private keys / certificates;
- Cloudflare API tokens;
- DB / SMTP / Gmail App Password / JWT signing secrets;
- `AGENTS_RU.md` (local agent notes only).

See also [`.gitignore`](.gitignore) and runbook «do not commit» sections.
