# TECH DEBT - Bespoke Sewing Studio

## Task 91 - Production user data backup and disaster recovery

- This is **not** application rollback, Git history, or a release ZIP restore.
  Git and release ZIP **are not** a backup of user data.
- Backup stored only on the same netcup VPS is **not sufficient**.
- The task is **not done** without a working schedule, encrypted offsite copy,
  and a tested staging restore rehearsal with a recorded last-successful-restore date.
- Do not implement in the current IN STOCK / Admin UI stage — planning only until
  a dedicated backup/DR implementation pass.

### Scope — full PostgreSQL dump (custom format)

Must cover all durable application data, including at least:

- requests / заявки;
- orders / заказы;
- contact messages;
- users and roles;
- CMS / Website Content / repeatable content;
- Portfolio;
- IN STOCK catalogue;
- settings / Brand / SEO configuration stored in DB;
- admin audit log;
- email delivery log and email outbox;
- upload / file metadata (`UploadedFile` and related relations).

### Scope — permanent uploads and keys

- All permanent upload roots: Portfolio, IN STOCK, Brand/SEO, Website Content,
  order attachments, and any other user/admin uploaded files under storage;
- ASP.NET Core Data Protection keys;
- production configuration inventory **without raw secrets** (names/locations of
  env vars, secret stores, and mount paths — never dump live passwords/keys into
  the inventory artifact).

### Operations requirements

- Daily schedule (automated);
- retain at least **14 daily** copies;
- weekly / monthly retention tiers;
- **encrypted offsite copy outside the netcup VPS**;
- checksums + backup manifest per run;
- verify dump with `pg_restore --list`;
- verify uploads archive and Data Protection keys archive integrity;
- alert / notify on failed backup;
- prevent overlapping / parallel backup runs (single-flight lock);
- take a backup before migrations and before bulk/mass data changes;
- staging restore rehearsal that checks requests, images, attachments, Admin login,
  Portfolio, and IN STOCK;
- record and publish the **date of the last successful restore rehearsal**.

### Explicit non-goals / clarifications

- Restoring a previous app release from Git or a deploy ZIP does **not** restore
  customer data, uploads, or keys.
- Local-only VPS copies (same disk/host) do not meet the offsite requirement.
- Task 42 docs (`BACKUP_RESTORE_RU.md`) remain the manual procedure reference;
  this task is the production automated backup + DR capability on top of that.

## Task 92 - Fix netcup deploy SSH bash stdin CRLF (deployment tooling)

- Production deploy aborted before file upload with
  `: invalid option namepefail` / `Remote SSH command failed (prepare remote directories and verify .env). Exit code: 2`.
- Cause: `Invoke-RemoteBashScript` sent Windows here-string bodies with CRLF over the
  PowerShell pipeline to `ssh ... bash -s`, so remote bash received `set -euo pipefail\r`.
- Fix in `scripts/production/netcup-deploy-release.ps1`: normalize CRLF/CR to LF, then write
  raw UTF-8 bytes to ssh stdin (Process `StandardInput.BaseStream`) so PowerShell does not
  reintroduce CRLF. SSH host/keys/paths, migrations, backup and health-check logic unchanged.
- Validated with PowerShell 5.1 `-WhatIf` dry-run. Real deploy, commit and push were not performed.

## Task 90 - Fix netcup deploy remote health-check quoting (deployment tooling)

- After a successful netcup release switch, embedded post-switch health checks failed with
  `curl: (6) Could not resolve host: https` and `[: too many arguments`, yet the PowerShell
  deploy script still printed `Deployment completed successfully.`
- Cause: multiline remote bash (`$remotePrepare` / `$remoteDeploy`) was passed as an `ssh`
  command-line argument (`ssh ... $remoteDeploy`), so remote shell re-parsing mangled curl
  header quotes (`X-Forwarded-Proto: https` split so `https` was treated as a host). Manual
  curls with the same headers (no space after `:`) returned HTTP 200.
- Fix in `scripts/production/netcup-deploy-release.ps1`:
  - `Invoke-RemoteBashScript` pipes the script body to `ssh ... "bash -s"` on stdin;
  - non-zero `$LASTEXITCODE` throws and stops the PowerShell deploy (no success message);
  - `check_local_endpoint` uses `--noproxy 127.0.0.1` and headers without a space after `:`;
  - HTTP 200–399 succeeds; curl errors or other statuses exit remote script with code 25;
  - existing rollback hint via `trap` / `current.previous` is preserved.
- Updated `scripts/production/README_RU.md`. No frontend/backend/API/DB/compose changes.
  Real deploy, commit and push were not performed.
  Follow-up CRLF stdin hardening is Task 92.

## Task 88 - Fix production CMS asset URL resolution (frontend)

- Production bug: with `VITE_API_BASE_URL=/api`, CMS APIs returned HTTP 200, but the
  frontend threw `TypeError: Invalid URL` while resolving root-relative asset URLs
  such as `/api/portfolio/images/{id}` via `new URL(..., apiClient.baseUrl.replace(/\/api\/?$/, "") + "/")`.
- Local development did not show the failure because the default API base is absolute
  (`http://127.0.0.1:5099/api`).
- Added shared pure helper `src/api/resolveApiAssetUrl.ts`:
  - production relative `/api` keeps root-relative `/api/...` assets as-is;
  - absolute development API base rewrites assets to the backend absolute URL;
  - `javascript:`, `data:`, `blob:` and other unknown schemes are rejected (`null`);
  - no `window` / `document` dependency.
- Helper wired into Portfolio, Website Content and Brand Settings API modules.
- Admin Portfolio: error state no longer shows a false empty state; empty state only
  after a successful load of an empty list; reload resets `hasLoadedSuccessfully`.
- Public Portfolio: shared `loadPublicPortfolio()`; successful CMS response replaces
  typed fallback and sets `isFallback=false`; real API failure keeps fallback; DEV-only
  `console.error` on initial load failure.
- Added minimal Vitest infrastructure (`npm.cmd test`) and unit tests for the helper,
  Portfolio/Content/Brand API mapping, Admin Portfolio list-state helper and Public
  Portfolio loader (46 tests passed).
- Backend, API contracts, migrations, database schema and production data were not
  changed; re-uploading already saved images is not required.
- Validation: `npm.cmd test` (46 passed), `npm.cmd run typecheck`, `npm.cmd run build`,
  and a production-like build with `VITE_API_BASE_URL=/api` plus
  `VITE_PUBLIC_SITE_URL=https://oksanalogosha.com` all succeeded.
- Updated `README.md`. `backend/README.md` unchanged (no API contract change).

## Task 89 - IN STOCK ready-to-buy catalogue — Done

- Public page **IN STOCK** for finished pieces available for purchase (enquiry
  workflow; no checkout/cart/payment).
- Main navigation item immediately after **Services** and before **Portfolio**
  (desktop header, mobile menu, footer; Brand Settings label/visibility).
- Admin module (separate from Portfolio CMS) manages title, description, price,
  photographs, Available/Reserved/Sold, publication/display order, sizes/materials.
- Scope completed: backend entities/API, uploads, admin UI, shared upload progress,
  public catalogue + detail routes, Brand Settings navigation fields, SEO/JSON-LD
  and dynamic sitemap.
- Must not be mixed into the existing Portfolio gallery CMS.

### Progress status (Task 89 completed)

- **Backend completed** (catalogue entities/API, atomic upload hardening, tests).
- **Admin UI completed** (Work → IN STOCK module: list/create/edit/archive/restore,
  multi-image management with shared upload progress).
- **Shared Upload Progress completed** (one XHR transport + reusable progress
  control/state machine wired to Portfolio, IN STOCK, Brand/SEO, Website Content
  and Order attachments).
- **Public catalogue completed** (`/in-stock`, `/in-stock/:slug`, navigation,
  SEO/JSON-LD, dynamic `/sitemap.xml`, Vitest + backend sitemap tests).

### Progress — Backend foundation completed

- Added dedicated `InStockItem` / `InStockItemImage` entities (not PortfolioItem),
  GBP currency, `Available`/`Reserved`/`Sold` status, publish/archive fields and
  EF migration `20260726105255_AddInStockCatalogue`.
- Public API: `GET /api/in-stock`, `GET /api/in-stock/{slug}`,
  `GET /api/in-stock/images/{imageId}` (published + non-archived only;
  root-relative image URLs; Reserved/Sold remain visible).
- Admin API: CRUD, archive/restore, multi-image upload/order/alt/delete using
  existing upload storage + ClamAV + deletion outbox; no permanent item delete,
  no checkout/payment.
- Backend tests cover validation, EF configuration, public/admin service rules,
  uploads, authorization metadata, audit action shapes and opt-in PostgreSQL
  schema uniqueness.

### Progress — Backend atomic upload hardening (stage 1.1)

- IN STOCK image attach now follows: quarantine → signature → ClamAV scan →
  promote → single DB transaction linking `UploadedFile` + `InStockItemImage` +
  `UpdatedAt` (no early `SaveChanges` of metadata alone).
- After successful promote, `AddImageAsync` wraps nextOrder / entity creation /
  BeginTransaction / SaveChanges / Commit in one try/catch. Rollback is
  best-effort with an independent bounded cleanup token (not the cancelled
  request token). Pre-commit failures compensate immediately; ambiguous
  `CommitAsync` failures leave the promoted file for StorageMaintenance
  reconciliation (no immediate delete). Original exceptions are always rethrown.
- On DB link failure after promote: immediate safe file delete, else durable
  deletion-outbox compensation (`in_stock_image.link_compensation`); public
  endpoint cannot serve unlinked files; StorageMaintenance remains the last
  resort orphan collector.
- Image delete schedules `UploadFileDeletionJob` in the same DB transaction as
  relation/metadata removal; physical delete stays with the background worker
  after commit; scheduler/SaveChanges failure rolls back and keeps the relation.
- Invalid multipart `displayOrder` returns ValidationProblem (not silently null).
- Post-commit audit/cache failures are logged and must not turn a successful
  mutation into a false client error.
- **Frontend shared Upload Progress UX implemented (stage 2):**
  `src/api/uploadTransport.ts` (XHR, real byte progress, cookies/Bearer, 401
  refresh retry, ProblemDetails/ValidationProblem, abort/timeout) plus
  `UploadProgressControl` / upload state machine used by Portfolio, IN STOCK,
  Brand/SEO, Website Content and Order attachments. Sequential multi-file
  uploads by default; no fake ClamAV percentage.

### Progress — Public catalogue / SEO / sitemap (stage 3)

- Public routes: `/in-stock` catalogue and `/in-stock/:slug` detail (lazy SPA
  chunks; SPA fallback for direct open/refresh).
- Navigation: Services → IN STOCK → Portfolio via shared `NAV_LINKS` + Brand
  Settings `ShowInStockLink` / `InStockLabel` (defaults visible / `IN STOCK`).
- Catalogue/detail UX: status badges, GBP price, ordered images, loading/error/
  empty states, Contact enquiry CTAs with `subject`/`message` query prefill
  (no auto-submit, no client reservation).
- SEO: route metadata + detail overrides (canonical, OG image from first photo,
  404 `noindex`); ItemList/Product JSON-LD with safe serialization.
- Sitemap: static `public/sitemap.xml` removed; backend `GET /sitemap.xml`
  includes catalogue + published item URLs under `https://oksanalogosha.com`
  (draft/archived/admin excluded). Migration
  `20260726154354_AddInStockNavigationSettings` for Brand nav columns.

## Task 87 - Admin owner workflow navigation restructure

- Admin navigation reorganized from one flat developer-style list into owner-facing
  groups: **Work**, **Website**, **Administration** and **Account**.
- Renamed owner-facing sections without changing backend APIs or data:
  `Contact Messages` -> `Messages` in the menu, `Services` -> `Services & Prices`,
  `Content` -> `Site Pages`, `Repeatable Content` -> `Website Blocks`,
  `Settings` split into menu entries `Business Info` and `System Settings` that
  reuse the same settings forms/save behavior.
- Moved the large production-readiness block out of the Dashboard into a separate
  **Administration -> Production Health** page. Dashboard now focuses on recent
  customer activity, new orders/messages, email outbox summary and a compact
  system-status link.
- Added clearer page descriptions and empty states for owner workflows, including
  Portfolio and Site Pages. Website Blocks now labels privacy sections as
  legal/advanced content.
- Backend/API contracts, authentication/session flow, email delivery, uploads,
  database schema and migrations were not changed. Production deploy/cutover was
  not performed.

## Task 86 - Production release archive deployment fix

- Root cause of the interrupted netcup deploy was Windows `Compress-Archive`
  creating ZIP entry names with backslash separators (`wwwroot\index.html`).
  Linux `unzip` can extract such archives but emits `appears to use backslashes
  as path separators`; under `set -e` this can stop the deploy script.
- `scripts/production/netcup-build-release.ps1` now creates the release ZIP with
  .NET ZipArchive and explicit `/` separators, validates published app files,
  validates generated migration SQL and fails if any ZIP entry contains `\`.
- `scripts/production/netcup-deploy-release.ps1` now validates the local archive,
  validates/tests the uploaded archive on the server, extracts into clean
  `current.new`, checks `BespokeStudio.Api.dll`, `wwwroot/index.html`, non-zero
  file count and no backslash filenames before applying migration SQL.
- Migration SQL is now applied only after archive/current.new validation succeeds
  and before switching `current.new` to `current`; if validation or SQL fails,
  existing `current` remains untouched. After switch, the script recreates
  `bespoke-studio-app`, runs local health checks and prints a rollback path on
  post-switch failure.
- Updated `DEPLOY_NETCUP_RU.md`, `PRODUCTION_DEPLOYMENT_RU.md` and
  `scripts/production/README_RU.md`. No production server actions, secrets,
  `.env`, production appsettings, dumps or backup archives were added.

## Task 85 - Resend API email provider and real readiness checks

- Added owner-managed `ResendApi` email delivery beside existing
  `Configuration`/`GmailSmtp`.
- Resend API key is stored only as a Data Protection-protected value in
  `SiteSettings`, is never returned by admin APIs, and uses production defaults
  `noreply@oksanalogosha.com` plus `Reply-To: contact@oksanalogosha.com`.
- Outbox delivery continues through existing `EmailOutboxMessages`; Resend
  accepted message ids are written to the safe result message, while provider
  failures remain safe in Email Log/LastError and automatic retry/fallback
  behavior is preserved.
- Admin Settings exposes provider selection, Resend API key/From/Reply-To
  fields, key configured status, clear-key action and Send test email result
  feedback.
- Added protected `GET /api/admin/production-readiness` with real backend checks
  for email delivery, email outbox, ClamAV clean-file probe and DNS TXT/MX
  records for `oksanalogosha.com`; Dashboard readiness uses this backend summary
  for green Upload security/DNS statuses.
- Added migration `AddResendEmailDeliverySettings` and fixed
  `20260629210000_AddHumanReadableRequestReferences` idempotent SQL from
  `SELECT setval(...)` to `PERFORM setval(...)` inside the `DO` block.
- Fixed EF discovery for `20260710120000_AddResendEmailDeliverySettings` by
  adding the conventional migration designer metadata tied to
  `BespokeStudioDbContext`; production release generation now fails if the
  idempotent SQL misses Resend columns/migration id or regresses to
  `SELECT setval(...)`, and netcup deploy applies the generated SQL before
  swapping `current`.
- Updated `README.md` and `backend/README.md`. No production secrets, `.env`,
  production appsettings or `AGENTS_RU.md` were added.

## Netcup production deployment preparation

- Added netcup-specific runbooks [`PRODUCTION_DEPLOYMENT_RU.md`](PRODUCTION_DEPLOYMENT_RU.md)
  and [`DEPLOY_NETCUP_RU.md`](DEPLOY_NETCUP_RU.md), production compose
  [`docker-compose.production.yml`](docker-compose.production.yml), Caddy example
  and `scripts/production/netcup-*` scripts for build, deploy, backup, checks and
  home-server migration.
- Old home-server / `192.168.2.202` / home Nginx / Cloudflare Tunnel /
  `cloudflared` / systemd deployment is deprecated in the new runbooks and
  README; it remains only as a migration source for DB, uploads and Data
  Protection keys.
- Backend publish can now serve the Vite `dist/` output from `wwwroot` through
  ASP.NET Core static files plus SPA fallback. Production compose uses the real
  `ConnectionStrings__BespokeStudioDb` key, PostgreSQL 18, a separate ClamAV
  container and `127.0.0.1:5030 -> 8080`.
- No secrets, real `.env`, production appsettings, migrations or AGENTS_RU.md
  were added.
- Remaining production work: actual SSH deploy to netcup, Cloudflare DNS/Caddy
  update, DB/uploads/Data Protection keys migration, SMTP/email smoke test and
  final public smoke test require production access and operator approval.

## Закрыто

- Task 84 — Production deployment plan / server execution checklist (docs-only): добавлен [`PRODUCTION_DEPLOYMENT_PLAN_RU.md`](PRODUCTION_DEPLOYMENT_PLAN_RU.md) — практический deployment plan для `https://oksanalogosha.com` без изменения runtime C#/frontend, backend API endpoints/service logic, schema/migrations, public API JSON contracts, auth/session/2FA/email/order/contact/upload behavior, OutputCache и backup script logic. Разделы: assumptions; server prerequisites; directory layout examples (Linux/Windows placeholders); build-machine validation; frontend/backend artifacts (`dist/`, `dotnet publish`); production env placeholders; DB migrations (EF only, never dev compose); pre-deployment backup; deployment sequence; systemd/IIS concept snippets; reverse proxy/Cloudflare; post-deploy smoke test; rollback; monitoring; anti-patterns; deployment decision log. Обновлены `README.md`, `backend/README.md`, `RELEASE_READINESS_REVIEW_RU.md`, `PRODUCTION_GO_LIVE_RU.md`, `PRODUCTION_LAUNCH_CHECKLIST_RU.md`. Secrets не добавлялись; `.env`/production appsettings не создавались; `AGENTS_RU.md` в Git не добавлялся. Future tasks: real production deployment execution; infra-as-code only after server details known; external monitoring/alerting; backup encryption/offsite; automated restore test.

- Task 83 — Final pre-deployment audit / release readiness review (docs/audit-only): добавлен [`RELEASE_READINESS_REVIEW_RU.md`](RELEASE_READINESS_REVIEW_RU.md) — финальный Go/No-Go обзор перед production deploy без изменения runtime C#/frontend, backend API endpoints/service logic, schema/migrations, public API JSON contracts, auth/session/2FA/email/order/contact/upload behavior и OutputCache. Документ разделяет repository readiness vs production environment operator actions; таблицы blockers (Git/validation/PostgreSQL/Data Protection/uploads/scanner/SMTP/HTTPS/backup/smoke test); static grep audit classification; production config checklist (placeholders only); validation command set; release decision log template; warnings/limitations; «must not commit» list. Обновлены `README.md`, `backend/README.md`, `PRODUCTION_GO_LIVE_RU.md`, `PRODUCTION_LAUNCH_CHECKLIST_RU.md`. Secrets не добавлялись; `.env`/production appsettings не создавались; `AGENTS_RU.md` в Git не добавлялся. Future tasks: real production deployment execution, backup encryption/offsite automation, external monitoring/alerting, automated restore test, object storage adapter/CDN if needed, larger admin mobile redesign only if needed.

- Task 82 — Final admin UX polish / small operational fixes (frontend/docs-only): улучшен operational UX Admin Email Log и Dashboard без изменения backend API endpoints/service logic, schema/migrations, public API JSON contracts, auth/session/2FA/email/order/contact/upload behavior и OutputCache. Admin Email Log: явное разделение **Global outbox health** / **Retention cleanup** (global counts) vs **Current page** stats; main **Refresh** обновляет entries + monitoring/retention summaries; compact **Refresh status** в summary sections; timestamps через `formatAdminDate`; retention cleanup success message с counts из `EmailOutboxRetentionCleanupResult`; helper text для empty candidates; empty state с **Clear filters**; manual retry `title`/`aria-label`; local helpers `EmailLogSectionHeader`, `EmailLogInlineNotice`, `EmailLogOperationButton`. Dashboard: compact email card (`break-words`), readiness item `Email outbox is healthy.` / safe summary message. Обновлены `README.md`, `backend/README.md`, `PRODUCTION_GO_LIVE_RU.md`, `PRODUCTION_LAUNCH_CHECKLIST_RU.md`. `.env`/production appsettings не создавались; `AGENTS_RU.md` в Git не добавлялся.

- Task 81 — OutputCache invalidation after admin CMS updates (backend/test/docs-only): добавлены tag constants и `PublicOutputCacheInvalidation` helper (`IOutputCacheStore.EvictByTagAsync`) для public JSON OutputCache без изменения TTL (60 s), public API JSON contracts, schema/migrations, frontend и без новых NuGet/npm dependencies. Public endpoints tagged: `public-content` + area tags (`public-services`, `public-portfolio`, `public-page-content`, `public-repeatable-content`, `public-site-settings`, `public-brand-settings`). После successful admin mutations evict matching tag only: services create/update/delete/archive → `public-services`; portfolio item/category create/update/delete/archive → `public-portfolio`; page content create/update/delete/archive → `public-page-content`; repeatable content create/update/delete/archive → `public-repeatable-content`; site settings update → `public-site-settings`; brand settings update → `public-brand-settings`. Pure image uploads, admin/auth/forms/orders/contact/uploads/images/health/version остаются uncached/not evicted. Unit tests в `PublicOutputCachePolicyTests` (constants, invalidation helper). Обновлены `backend/README.md`, `README.md`, `PRODUCTION_GO_LIVE_RU.md`, `PRODUCTION_LAUNCH_CHECKLIST_RU.md`. Auth/session/2FA/email/order/contact/upload behavior не менялся; `.env`/production appsettings не создавались; `AGENTS_RU.md` в Git не добавлялся.

- Task 80 — PostgreSQL-backed integration tests (test/docs-only): добавлен opt-in PostgreSQL integration test infrastructure и небольшой набор persistence-sensitive tests без изменения runtime C#/frontend behavior, schema/migrations, secrets и без новых NuGet/npm dependencies. Новые файлы в `backend/tests/BespokeStudio.Tests/Integration/PostgreSql/`: `PostgreSqlIntegrationSettings` (env vars `BESPOKESTUDIO_RUN_POSTGRES_INTEGRATION_TESTS=true`, `BESPOKESTUDIO_POSTGRES_ADMIN_CONNECTION_STRING`), `PostgreSqlIntegrationFactAttribute` (skip by default с понятным reason), `PostgreSqlTestDatabase` (CREATE/DROP только generated `bespoke_studio_integration_<guid>`, `Database.MigrateAsync()`, no `EnsureCreated`/`EnsureDeleted` on operator DB, no connection string logging), `PostgreSqlIntegrationTestCollection` (disable parallelization), `PostgreSqlPersistenceIntegrationTests` (migrations apply; email outbox enum/body/log round-trip; `CK_EmailOutboxMessages_Body` check constraint; `EmailOutboxRetentionService` purge/delete/retain-failed on real PostgreSQL). Default `dotnet test backend\BespokeStudio.sln` skips integration tests when env not set (CI/dev safe). Обновлены `backend/README.md`, `README.md`, `PRODUCTION_GO_LIVE_RU.md`, `PRODUCTION_LAUNCH_CHECKLIST_RU.md`. Auth/session/2FA/email/order/contact/upload runtime behavior не менялся; backup scripts не менялись; `.env`/production appsettings не создавались; `AGENTS_RU.md` в Git не добавлялся.

- Task 79 — Production backup automation draft (scripts/docs-only): добавлен draft/reference PowerShell script для production backup без изменения C#/frontend/runtime behavior, без secrets и без tracked backup artifacts. Новые файлы: `scripts/production/Backup-Production.ps1` (PostgreSQL custom dump `postgresql.dump`, optional `uploads.zip` / `data-protection-keys.zip`, `backup-metadata.json`, `pg_restore --list` verification, `-DryRun`, retention prune через `-ApplyRetention -RetentionDays N`, safety check что `BackupRoot` вне Git repo, no password parameter / no `.env` read / no secrets logging) и `scripts/production/README_RU.md` (назначение, preconditions, dry-run/real-run examples с placeholders, verification, retention, scheduling notes, security). Обновлены `.gitignore` (backup artifacts must never be committed), `BACKUP_RESTORE_RU.md` (Draft backup automation script + обновлён «Что пока не автоматизировано»), `PRODUCTION_GO_LIVE_RU.md` (pre-launch backup: dry-run, outside repo, metadata), `PRODUCTION_LAUNCH_CHECKLIST_RU.md` (draft script reviewed/dry-run/real backup/list/metadata/retention dry-run), `README.md`, `backend/README.md`, `TECH_DEBT_RU.md`. Future tasks остаются: encryption/offsite upload automation, automated restore test, backup monitoring/alerting, Linux systemd timer/cron hardening. Auth/session/2FA/email/order/contact/upload behavior не менялся; migrations/schema нет; `.env`/`.env.local`/`.env.production`/`appsettings.Production.json`/`appsettings.Staging.json` не создавались; backup files/key XML/TLS certs не добавлялись; `AGENTS_RU.md` в Git не добавлялся.

- Task 78 — Email outbox retention cleanup: добавлен безопасный retention cleanup для terminal email outbox messages без изменения SMTP/automatic retry/manual retry/monitoring behavior и без migration. Backend: options `EmailOutboxRetention` в `appsettings.json` (worker disabled by default; body/message retention days; batch size; placeholder `[Email body purged by retention policy.]`), contracts `EmailOutboxRetentionSummaryResponse` и `EmailOutboxRetentionCleanupResponse` (только counts/config/safe messages — без body/secrets), pure helper `EmailOutboxRetentionPolicy` с unit-тестами (`EmailOutboxRetentionPolicyTests`), `IEmailOutboxRetentionService` + `EmailOutboxRetentionService` (summary aggregate counts; cleanup: сначала delete very old `Succeeded`/`Skipped` outbox rows до `BatchSize`, затем body purge для remaining rows между body и message retention с `HtmlBody=null`, `TextBody=placeholder`, `UpdatedAt=now`; `EmailDeliveryLogEntry` не удаляется; failed/pending/processing/retrying не трогаются), optional `EmailOutboxRetentionWorker` (если `WorkerEnabled=false` — один safe log и exit; если enabled — periodic cleanup с safe count logs). Endpoints: `GET /api/admin/email-log/retention`, `POST /api/admin/email-log/retention/cleanup` (AdminOnly; manual cleanup audit `email_outbox.retention_cleanup_ran` с count metadata only). Frontend: types + `getEmailOutboxRetentionSummary()` / `runEmailOutboxRetentionCleanup()`; `AdminPage` state/loaders; Admin Email Log retention section с worker status, candidate counts, failed retained, **Run cleanup** (disabled если no candidates). Automatic retry/backoff, manual retry (Task 76), monitoring summary (Task 77), auth/session/2FA, public Order/Contact/Upload behavior не менялись. Email bodies/secrets не выводятся в API/UI/audit/logs. Schema migration нет, новых npm/NuGet зависимостей нет, `.env`/`.env.local`/`.env.production`/`appsettings.Production.json`/`appsettings.Staging.json` не создавались, `AGENTS_RU.md` в Git не добавлялся. Обновлены `backend/README.md`, `README.md`, `PRODUCTION_GO_LIVE_RU.md`, `PRODUCTION_LAUNCH_CHECKLIST_RU.md`.

- Task 77 — Email outbox monitoring / alerting: добавлен read-only admin monitoring email outbox в Admin Dashboard и Admin Email Log без изменения SMTP/outbox/manual-retry behavior. Backend: новый contract `EmailOutboxMonitoringSummaryResponse` (только агрегированные counts + `OldestPendingCreatedAt`/`OldestFailedUpdatedAt`/`GeneratedAt`/`StalePendingThresholdMinutes`/`HealthStatus`/`SummaryMessage` — без recipient/subject/body/secrets), pure helper `EmailOutboxMonitoringPolicy` (`ResolveHealthStatus`: `Critical` если `ExhaustedFailedCount>0`, иначе `Warning` если `StalePendingCount>0||FailedCount>0||RetryingCount>0`, иначе `Healthy`; `ResolveSummaryMessage` → safe message) с unit-тестами (`EmailOutboxMonitoringPolicyTests`). Метод `IEmailDeliveryLogService.GetOutboxMonitoringSummaryAsync` + реализация в `EmailDeliveryLogService` считает counts по `EmailOutboxMessages.AsNoTracking()` простыми aggregate-запросами (`CountAsync`) и не выбирает `HtmlBody`/`TextBody`; stale pending threshold — константа 15 минут. Endpoint `GET /api/admin/email-log/summary` (AdminOnly, 200/401/403), не конфликтует с `/{id:guid}/retry`. Frontend: тип `EmailOutboxMonitoringSummary` + `getEmailOutboxMonitoringSummary()`; `AdminPage` грузит summary при открытии, обновляет при realtime `EmailDeliveryLog` и после manual retry, 401/403 → logout, прочие ошибки → safe message. Admin Dashboard: верхняя карточка `Email outbox` (Healthy/Warning/Critical/Checking/Unavailable, caption по состоянию, клик → Email Log) + отдельный readiness item `Email outbox`. Admin Email Log: global outbox summary row (`Outbox health`/`Failed`/`Retrying`/`Pending / stale`/`Sent 24h`) отдельно от page-entries cards + warning/info панели для exhausted failed / stale pending / retrying. Nav badge для `emailLog` (newCount = exhaustedFailed+stalePending, totalCount = failed+retrying+pending). Monitoring read-only: automatic retry/backoff (`EmailOutboxProcessor`/`EmailOutboxRetryPolicy`), manual retry (Task 76), SMTP provider behavior, auth/session/2FA/refresh, public Order/Contact/Upload behavior не менялись. Email bodies/secrets не выводятся в API/UI/audit/logs. Schema migration нет, новых npm/NuGet зависимостей нет, `.env`/`.env.local`/`.env.production`/`appsettings.Production.json`/`appsettings.Staging.json` не создавались, `AGENTS_RU.md` в Git не добавлялся. Обновлены `backend/README.md`, `README.md`, `PRODUCTION_GO_LIVE_RU.md`, `PRODUCTION_LAUNCH_CHECKLIST_RU.md`.

- Task 76 — Manual retry in Admin Email Log: добавлен ручной retry для exhausted failed email outbox messages из Admin → Email Log. Backend: новый contract `EmailDeliveryManualRetryResponse` (без body/secrets — только ids/status/resultMessage/messageType/relatedEntityLabel/queuedAt), pure helper `EmailOutboxManualRetryPolicy.IsManualRetryEligible` (eligible только если `Status==Failed` && `Attempts>=MaxAttempts` && `NextAttemptAt==null`; scheduled automatic retry, `Pending`/`Processing`/`Succeeded`/`Skipped` — not retriable) с unit-тестами, метод `IEmailOutboxService.QueueManualRetryAsync` + реализация в `EmailOutboxService` (находит outbox message по `EmailDeliveryLogEntryId`; not found → `EmailOutboxMessageNotFoundException`; not eligible → `EmailManualRetryNotAllowedException`; при успехе сбрасывает message в `Pending`, `Attempts=0`, `MaxAttempts=options`, `NextAttemptAt=now`, очищает `ProcessingStartedAt`/`SentAt`/`LastError`, обновляет linked log entry в `Provider="Outbox"`/`Status="Queued"`/`SentExternally=false`/`ResultMessage="Manual retry queued for background delivery."`/`ErrorMessage=null`/`CompletedAt=null`, шлёт realtime notification). Endpoint `POST /api/admin/email-log/{id:guid}/retry` (AdminOnly): 200 с response, 404 если message/log не найден, 409 если not eligible, 401/403 как у group; успешный retry пишет audit `email_outbox.manual_retry_queued` (entityType `EmailOutboxMessage`, entityId outbox id, safe label, metadata только emailDeliveryLogEntryId/outboxMessageId/messageType — без body/secrets). Frontend: `retryEmailDeliveryLogEntry(id)` в `emailDeliveryLogApi.ts` и кнопка **Retry** в Admin Email Log (только для `Failed`, disabled `Retrying…` во время запроса, success info `Manual retry queued.` + reload, 401/403 → `onUnauthorized`, 409 → «This email is not eligible for manual retry anymore.», без показа body). Attempts увеличивается только когда worker реально claim'ит message; automatic retry/backoff (`EmailOutboxProcessor`/`EmailOutboxRetryPolicy`), SMTP provider behavior, auth/session/2FA/refresh, public Order/Contact/Upload behavior не менялись. Email bodies/secrets/cookies/tokens/SMTP password/Gmail App Password не выводятся в API/UI/audit/logs. Schema migration нет (используются существующие поля), новых npm/NuGet зависимостей нет, `.env`/`.env.local`/`.env.production`/`appsettings.Production.json`/`appsettings.Staging.json` не создавались, `AGENTS_RU.md` в Git не добавлялся. Обновлены `backend/README.md`, `README.md`, `PRODUCTION_GO_LIVE_RU.md`, `PRODUCTION_LAUNCH_CHECKLIST_RU.md`; future пункт «manual retry» убран из Email Outbox future-списка.

- Task 75 — Final production checklist pass / Go-live runbook (docs/checklist-only): добавлен короткий day-of-launch runbook `PRODUCTION_GO_LIVE_RU.md` (RU) со ссылками на подробные runbooks и разделами: A. назначение (production domain `https://oksanalogosha.com`, список подробных runbooks); B. Go/No-Go критерии (Git clean, release commit, CI green, typecheck/build/`dotnet test`/`dotnet build` green, verified backup + `pg_restore --list`, restore rehearsal, env/secrets вне Git, persistent Data Protection keys, ClamAV/scanner решение, SMTP strategy + test email, HTTPS/reverse proxy, admin login/2FA/session, public Order/Contact/upload smoke tests, нет blockers); C. freeze перед запуском; D. pre-launch backup (DB/uploads/keys/secrets/proxy-TLS, `pg_restore --list`, no backup files в repo); E. production configuration checklist (`VITE_PUBLIC_SITE_URL`, `VITE_API_BASE_URL` не localhost, `Cors__AllowedOrigins__0`, `DataProtection__ApplicationName`/`KeysPath`, `UploadStorage__RootPath`, `MalwareScanner__Provider` не Disabled, `TreatScannerErrorAsRejection=true`, SMTP provider выбран, `Provider=Logging` не prod sender, точные `ForwardedHeaders__KnownProxies`/`KnownNetworks`, secrets не в Git); F. deployment order (backup→backend→migrations→start→health→frontend→reverse proxy→Cloudflare/DNS→public smoke test→traffic); G. final smoke test через public HTTPS (домен, direct reload SPA routes, robots/sitemap, health `/health/live`|`/health/ready`|`/healthz`|`/readyz`|`/api/version`, security headers, HSTS, admin login/2FA/refresh/active sessions, SignalR, public Contact/Order, clean upload, too-large rejected, EICAR controlled, owner/customer email, Email Log/outbox, Storage Maintenance scan, Audit Log, no secrets в logs); H. SEO/legal smoke test (canonical, sitemap только public routes, admin не в sitemap, `/admin`+`/admin/login` noindex/nofollow, OG/Twitter, Privacy/Terms, нет фиктивных данных); I. Go/No-Go decision log (mini-template); J. rollback quick plan; K. after launch monitoring; L. known future improvements (не blockers). Обновлены `PRODUCTION_LAUNCH_CHECKLIST_RU.md` (вводный блок-ссылка на go-live runbook; домен `https://oksanalogosha.com` подтверждён везде, admin routes не в sitemap, placeholder-домена нет), `README.md` (day-of-launch runbook + требования green checks/backup+rehearsal/HTTPS/SMTP/uploads/Data Protection/final smoke test), `backend/README.md` (backend launch smoke tests входят в `../PRODUCTION_GO_LIVE_RU.md`). Production preparation tasks 69–75 закрыты. C#/frontend/API behavior, docker compose, migrations, appsettings production, `public/robots.txt`/`public/sitemap.xml` (уже с правильным доменом) и runtime поведение не менялись; auth/session/2FA/email/order/contact/upload behavior не менялось; secrets, real backup files, Data Protection key XML, TLS private keys/certs не добавлялись; `.env`/`.env.local`/`.env.production`/`appsettings.Production.json`/`appsettings.Staging.json` не создавались; `AGENTS_RU.md` в Git не добавлялся. Future tasks остаются future (не blockers): scheduled backup automation, backup encryption/offsite upload, monitoring/alerting, email manual retry, object storage adapter, PostgreSQL-backed integration tests, CDN/cache strategy.

- Task 74 — Backup / restore final production pass (docs/checklist-only): `BACKUP_RESTORE_RU.md` дополнен разделом «Production final pass» (не переписан с нуля) с подразделами: A. full production backup inventory (таблица: PostgreSQL dump, uploads storage root, Data Protection keys folder, env/secret store metadata, SMTP/Gmail settings без raw secrets, reverse proxy/TLS config, TLS certs/private keys только в защищённом backup, Git commit SHA, migrations list, runbooks version) с явными взаимозависимостями (DB-only недостаточен, uploads-only недостаточен, restore без Data Protection keys ломает protected Gmail App Password/2FA, proxy/TLS config хранится отдельно); B. backup classification (repository state = Git; runtime state = DB/uploads/keys; secrets/config; docs/runbooks; Git не является backup runtime state); C. cadence/retention (daily DB+uploads, 7–14 копий, weekly/monthly, manual backup перед deploy, offsite/encrypted, retention выбирает владелец, персональные данные по policy); D. consistent backup (остановить backend/maintenance window, риск рассинхронизации, matching DB+uploads+keys); E. backup verification (`pg_restore --list`, размеры, открываемость archive, наличие key files, Git commit SHA, secrets не в Git, периодический rehearsal); F. restore rehearsal / disaster recovery drill (staging, не production; DB→uploads→keys→secrets→migrations→запуск→smoke test→запись результата; test email только на controlled address; не трогать production DNS/Cloudflare); G. consolidated post-restore smoke test (health `/health/live`|`/health/ready`|`/healthz`|`/readyz`|`/api/version`, admin login, 2FA, refresh after reload, active sessions, orders, contact messages, attachment download, Storage Maintenance scan, clean upload, delete attachment, Gmail test email, Email Log/outbox, public Contact/Order forms, public pages, robots/sitemap, HTTPS/reverse proxy); H. rollback plan before migration/deploy; I. security/privacy notes (encrypt at rest, restrict access, не в cloud/chat/email/Git, no EICAR в backup/repo, защита TLS/Data Protection keys). Обновлены `PRODUCTION_LAUNCH_CHECKLIST_RU.md` (раздел Backups: full inventory, DB/uploads/keys/secrets/proxy-TLS backup, `pg_restore --list`, restore rehearsal перед launch, post-restore smoke test, rollback plan, encrypted/restricted/offsite policy, no backup files in Git + ссылки на BACKUP_RESTORE/DATA_PROTECTION/UPLOADS/SMTP/REVERSE_PROXY runbooks), `README.md` (BACKUP_RESTORE_RU.md как final runbook, состав full backup, restore rehearsal перед launch), `backend/README.md` (final backup inventory/restore rehearsal, DB-only недостаточен для uploads/protected Gmail App Password/2FA, ссылка на `../BACKUP_RESTORE_RU.md`). C#/frontend/API behavior, docker compose, migrations, appsettings production files и runtime поведение не менялись; auth/session/2FA/email/order/contact/upload behavior не менялось; secrets, real backup files (`.dump`/`.sql`/`.backup`/`.bak`/`.zip`/`.tar.gz`), Data Protection key XML, TLS private keys/certs не добавлялись; `.env`/`.env.local`/`.env.production`/`appsettings.Production.json`/`appsettings.Staging.json` не создавались; `AGENTS_RU.md` в Git не добавлялся. Future automation (scheduled backup job, backup encryption/offsite upload, automated restore test, backup monitoring/alerting, object storage/CDN backup flow) остаётся как future tasks (раздел «Что пока не автоматизировано» в `BACKUP_RESTORE_RU.md`).

- Task 73 — Reverse proxy / HTTPS production readiness and runbook (docs-only): добавлен production runbook `REVERSE_PROXY_HTTPS_PRODUCTION_RU.md` (RU) — описаны архитектура (Internet → Cloudflare → reverse proxy → Kestrel; canonical apex `https://oksanalogosha.com`, www→apex redirect, Kestrel не выставляется в интернет), что НЕ хранить в Git (TLS private keys, Cloudflare Origin Certificate private key, Cloudflare API tokens, real private IP, `.env*`/`appsettings.Production.json`, полный prod proxy config с секретами), deployment topology (предпочтителен one-origin), Cloudflare checklist (DNS, www→apex, SSL/TLS mode Full/не Flexible как источник redirect loops, valid origin cert, аккуратный HSTS, trust только ближайшего proxy, Cloudflare Email Routing не sender), reverse proxy requirements (передача `X-Forwarded-For`/`X-Forwarded-Proto`/`X-Forwarded-Host`/`Host`, WebSocket upgrade для `/hubs/admin-notifications`, SPA fallback, не кэшировать admin/API, не публиковать upload storage, body size ≈ upload limit, logs без секретов), placeholder env examples (`ForwardedHeaders__ForwardLimit`/`KnownProxies`/`KnownNetworks`, `Cors__AllowedOrigins__0`, `VITE_PUBLIC_SITE_URL`, `VITE_API_BASE_URL` не localhost), cookies/auth/HTTPS, security headers/CSP/HSTS (backend отдаёт baseline API headers, document CSP задаёт фронтенд-хост), health checks за proxy (`/health/live`, `/healthz`, `/health/ready`, `/readyz`, `/api/version`), illustrative Nginx/Caddy snippets и IIS checklist только с placeholders и предупреждением «адаптировать», production smoke test, troubleshooting и operational checklist. Обновлены `README.md` (ссылка + Cloudflare/reverse proxy до launch, Kestrel не напрямую, API не на localhost), `backend/README.md` (ссылка на `../REVERSE_PROXY_HTTPS_PRODUCTION_RU.md` + forwarded headers/WebSockets/HSTS уточнения), `DEPLOYMENT_NOTES_RU.md` (reverse proxy секция: SPA fallback за proxy, `/api`/`/health`/`/healthz`/`/readyz`/`/hubs` проксируются в backend, `/admin` — frontend route, WebSocket для SignalR), `PRODUCTION_LAUNCH_CHECKLIST_RU.md` (расширенный reverse proxy/HTTPS/Cloudflare блок + ссылка) и `BACKUP_RESTORE_RU.md` (proxy/TLS config и certs — operational secrets вне Git; после restore проверять HTTPS/health/auth/SignalR/uploads). C#/frontend/API behavior, `Program.cs`/`ForwardedHeadersSettings.cs`/`CorsSettings.cs`/`SecurityHeadersSettings.cs`/frontend config не менялись; auth/session/2FA/email/order/contact/upload поведение и схема БД не менялись; migration, secrets, TLS/cert private keys, Cloudflare tokens и новые зависимости не добавлялись; `.env`/`.env.local`/`.env.production`/`appsettings.Production.json`/`appsettings.Staging.json` не создавались; `AGENTS_RU.md` в Git не добавлялся.

- Task 72 — Production Data Protection readiness and runbook (docs-only): добавлен production runbook `DATA_PROTECTION_PRODUCTION_RU.md` (RU) — описаны фиксированный `DataProtection:ApplicationName=BespokeSewingStudio`, обязательный `DataProtection:KeysPath` на production (fail-fast: startup падает с `DataProtection:KeysPath is required in Production.` при пустом path), persistent absolute keys path вне repo/publish folder, folder permissions, что защищается Data Protection (owner-managed Gmail SMTP App Password, 2FA challenge cookie) и чем ключи НЕ являются (не JWT signing key/не PostgreSQL password/не SMTP secret), placeholder config examples (Windows/Linux), smoke test перед launch, restore/redeploy test, troubleshooting, multi-instance note (общий key ring; external key store — future task) и operational checklist. Обновлены `README.md` (ссылка + KeysPath required/persistent/backed up), `backend/README.md` (ссылка на `../DATA_PROTECTION_PRODUCTION_RU.md` + уточнения fail-fast/стабильный ApplicationName/keys вне repo и в backup), `PRODUCTION_LAUNCH_CHECKLIST_RU.md` (расширенный Data Protection блок + ссылка), `BACKUP_RESTORE_RU.md` (keys обязательны для restore, последствия потери для Gmail App Password/2FA, test email + 2FA smoke test, re-enter/rotate при потере) и `SMTP_PRODUCTION_RU.md` (перекрёстная ссылка на Data Protection runbook). C#/frontend/API behavior, Data Protection runtime code, auth/session/2FA/email/order/contact/upload поведение и схема БД не менялись; `Program.cs`/`DataProtectionSettings.cs`/`EmailDeliverySettingsService.cs` не трогались; migration, secrets, real key XML files и новые зависимости не добавлялись; `.env`/`.env.local`/`.env.production`/`appsettings.Production.json`/`appsettings.Staging.json` не создавались.

- Task 71 — Production uploads / ClamAV readiness and runbook (docs-only): добавлен production runbook `UPLOADS_PRODUCTION_RU.md` (RU) — описаны текущая production-стратегия (local filesystem storage + ClamAV/CommandLine scanner), upload flow (quarantine → signature/magic bytes → malware scan → final storage), production `UploadStorage__RootPath` (placeholder env vars, writable, не public static directory, доступ только через backend API), folder layout (`quarantine/`, `order-attachments/yyyy/MM`, `portfolio-images/`, `content-images/`, `brand-images/`), установка/настройка ClamAV (Linux/Windows), runtime `UploadSecurity__MalwareScanner__*` env vars, EICAR/smoke test (controlled test, не в Git), failure/troubleshooting, backup/restore и operational checklist. Явно зафиксировано: `Provider=Disabled` только dev/local, `TreatScannerErrorAsRejection=true` — production-рекомендация (fail-closed), «security scan completed» вместо «100% safe», object storage adapter остаётся future task. Обновлены `README.md`, `backend/README.md` (ссылка на `../UPLOADS_PRODUCTION_RU.md` + уточнения про Disabled/secret store/TreatScannerErrorAsRejection), `PRODUCTION_LAUNCH_CHECKLIST_RU.md` (расширенный раздел «Upload security and storage» + ссылка), `BACKUP_RESTORE_RU.md` (uploads обязателен для restore, Storage Maintenance scan, clean upload smoke test, EICAR не в backup/repo). C#/frontend/API behavior, upload validation/quarantine/scanner/deletion-outbox поведение и схема БД не менялись; migration, secrets и новые зависимости не добавлялись; `.env`/`.env.local`/`.env.production`/`appsettings.Production.json`/`appsettings.Staging.json` не создавались; Docker ClamAV service не добавлялся.

- Task 70 — Production SMTP readiness and runbook (docs-only): добавлен production runbook `SMTP_PRODUCTION_RU.md` на русском — описаны две поддержанные стратегии (developer-managed SMTP через `Notifications__Email__Smtp__*` env vars/secret store и owner-managed Gmail SMTP через Admin → Settings → Email delivery), явно указано, что `Provider=Logging` — только dev/local fallback, а Cloudflare DNS отвечает за SPF/DKIM/DMARC, но не является SMTP sender (Cloudflare Email Routing как sender не используется). Добавлены: раздел «что не хранить в Git», Cloudflare DNS SPF/DKIM/DMARC checklist для `oksanalogosha.com` (без выдуманных значений, DMARC placeholder помечен как пример), production smoke test и troubleshooting. Обновлены `README.md`, `backend/README.md` (ссылка на `../SMTP_PRODUCTION_RU.md`, уточнения про Logging/secret store/persistent Data Protection keys), `PRODUCTION_LAUNCH_CHECKLIST_RU.md` (структурированный email checklist + ссылка) и `BACKUP_RESTORE_RU.md` (последствия потери Data Protection keys для protected App Password + test email после restore). В docs прежний Gmail App Password placeholder заменён на `<google-app-password-from-secret-store>`. C#/frontend код, API JSON contracts, email sender/outbox/retry поведение, auth/session/order/contact/upload поведение и схема БД не менялись; migration, secrets и новые зависимости не добавлялись; `.env`/`.env.local`/`.env.production`/`appsettings.Production.json`/`appsettings.Staging.json` не создавались. Будущие задачи: manual retry в Email Log, email monitoring/alerting, retention/cleanup старых outbox bodies, bounce/rejection handling.

- Task 69 — Production domain / SEO pass: прежний placeholder-домен заменён на реальный canonical origin `https://oksanalogosha.com` (apex, без `www`). Обновлены `public/robots.txt` (Sitemap URL, `Allow: /`, `Disallow: /admin`, `Disallow: /admin/login` сохранены) и `public/sitemap.xml` (все `<loc>` для `/`, `/services`, `/portfolio`, `/order`, `/about`, `/contact`, `/privacy`, `/terms`; admin routes не добавлялись; blocking-комментарий заменён на нейтральный про canonical origin). В `.env.example` добавлен документирующий комментарий с `VITE_PUBLIC_SITE_URL=https://oksanalogosha.com` (public example, без secrets). Обновлены `README.md` и `PRODUCTION_LAUNCH_CHECKLIST_RU.md` (production origin, `VITE_PUBLIC_SITE_URL`, проверки canonical/OG/Twitter/admin noindex). `SeoManager.tsx`/`appConfig.ts`/`index.html` не менялись — canonical/OG/Twitter уже строятся из `VITE_PUBLIC_SITE_URL` (`appConfig.publicSiteUrl`), hardcoded домен в React не добавлялся. www→apex redirect и DNS/HTTPS остаются отдельным Cloudflare/server deployment-шагом. Backend/API contracts, auth/session/email/order/contact/upload поведение и схема БД не менялись; migration, secrets и новые зависимости не добавлялись.

- Task 68 — Upload storage abstraction: добавлен интерфейс `IUploadStorage` и local-адаптер `LocalUploadStorage` (`backend/src/BespokeStudio.Infrastructure/Storage`), инкапсулирующие storage root и все `File`/`Directory`/`FileStream`/`Path` операции; path-resolution и traversal-protection по-прежнему делегируются существующему `UploadStoragePath` (rooted/`../`/drive keys запрещены, storage keys остаются relative и safe). На abstraction переведены `LocalUploadService`, `UploadCleanupService`, `StorageMaintenanceService`, `UploadFileDeletionScheduler` и `UploadFileDeletionProcessor` — они больше не держат `_storageRoot` и не вызывают `UploadStoragePath` напрямую. Quarantine → signature/magic-bytes → `IMalwareScanner` (через `GetRequiredLocalPhysicalPath`) → move-to-final flow, размер/filename/content-type/extension валидации, quarantine/final layout, orphan cleanup, admin storage scan/delete-orphans (без absolute paths) и deletion outbox (missing → Skipped, safe generic errors) сохранены без изменений. `IUploadStorage -> LocalUploadStorage` зарегистрирован как `Scoped` в `DependencyInjection.cs`. Добавлены unit-тесты (`backend/tests/BespokeStudio.Tests/Storage/LocalUploadStorageTests.cs`) с temp-директориями и `ProjectReference` теста на Infrastructure. API JSON contracts, auth/session/email/order/contact/frontend поведение и схема БД не менялись; migration, secrets и новые NuGet/npm зависимости не добавлялись. Production object-storage adapter (S3/Azure/R2) пока не реализован и остаётся будущей задачей.

- Task 66 — Structured logging + correlation id: добавлен `CorrelationIdMiddleware` (`backend/src/BespokeStudio.Api/Middleware/CorrelationIdMiddleware.cs`), зарегистрированный в `Program.cs` сразу после `UseForwardedHeaders()` и до security headers, exception handler, CORS, auth, output cache, authorization и rate limiter. Header `X-Correlation-ID`: валидный входящий id (после trim непустой, ≤120 символов, без control-символов, только `A-Za-z0-9._:-`) переиспользуется, иначе генерируется новый через `Guid.NewGuid().ToString("N")`; resolved id возвращается в каждом response и сохраняется в `HttpContext.Items`. Downstream оборачивается в `ILogger.BeginScope` с безопасными полями `CorrelationId`/`TraceIdentifier`/`RequestMethod`/`RequestPath`; включён `ActivityTrackingOptions` (`TraceId`/`SpanId`/`ParentId`), сохранён Windows EventLog health-check filter. Добавлены unit-тесты (`DefaultHttpContext` + `NullLogger`). Тела запросов, cookies, токены, Authorization headers и секреты не логируются; API JSON contracts, auth/session/email/order/upload поведение и схема БД не менялись; migration и сторонние logging-пакеты не добавлялись. Подключение `EmailOutboxMessage.CorrelationId` намеренно оставлено на будущую задачу.

- Task 67 — Prototype mode cleanup: удалены недостижимые от `src/main.tsx` ранние generated leftovers (`usePrototypeForm`, Figma image fallback и неиспользуемый `components/ui` набор), неиспользуемый Figma asset resolver и prototype-only `apiClient.resolve`/`isPrototypeMode` shim. Локальный typed fallback сохранён как часть backend-first resilience, но теперь возвращается без фиктивного prototype mode. Npm package name приведён к `bespoke-sewing-studio`; связанные только с удалёнными generated components зависимости удалены. Frontend/backend behavior, API contracts и БД не менялись; migration не потребовалась.

- Task 54.1 — Remove unused frontend dependencies: после проверки imports, config/scripts и peer/runtime dependency graph удалены 11 неиспользуемых direct dependencies: `@emotion/react`, `@emotion/styled`, `@mui/icons-material`, `@mui/material`, `@popperjs/core`, `canvas-confetti`, `react-dnd`, `react-dnd-html5-backend`, `react-popper`, `react-responsive-masonry`, `react-slick`. `npm uninstall` синхронизировал `package.json`/`package-lock.json` и удалил 77 package nodes; frontend behavior и backend не менялись. `date-fns` сохранён как peer dependency используемого `react-day-picker`, а generated UI/tooling dependencies оставлены из-за фактических imports/config/scripts.

- Task 54.0 — Microsoft.OpenApi NU1903: `Swashbuckle.AspNetCore` точечно обновлён с `10.0.0` до `10.2.3`; транзитивный `Microsoft.OpenApi` обновился с уязвимой версии `2.3.0` до `2.7.5`. `dotnet list package --vulnerable --include-transitive` больше не находит уязвимых пакетов. Поведение Swagger/OpenAPI и backend API не менялось; migration не потребовалась.

- Task 50 — Split AdminPage into modules: без изменения поведения и дизайна из `AdminPage.tsx` вынесены admin section/hash navigation, Dashboard overview/cards/readiness helpers, live updates status, attention counters/badges и Orders CSV export. `AdminPage.tsx` уменьшен с 1297 до 590 строк и оставлен orchestrator'ом auth, state, data loading, realtime events и panel wiring. Backend/API contracts/migrations не менялись. Дальнейшее разделение orchestration/data-loading на специализированные hooks оставлено для отдельной задачи, чтобы refactor-only изменения не затрагивали admin behavior.

- Task 65 — Public content output caching: встроенный server-side ASP.NET Core Output Caching подключён одной явной политикой `PublicContent` с TTL 60 секунд только к anonymous JSON GET для services, portfolio metadata, page/repeatable content, public site settings и public brand/SEO settings. Admin/auth/forms/orders/contact/uploads/images/health/version не кэшируются. Query/path входят в cache key, framework defaults исключают authenticated, cookie-setting, non-200 и non-GET/HEAD responses. Browser/CDN `Cache-Control` намеренно не форсируется; migration и внешнее cache storage не требуются.
- Task 64 — Fix version and compat health endpoints: `/api/version` возвращает stable typed metadata (`application`, version, environment, framework, optional commit/build time и process start time) из `BUILD_VERSION`/`GIT_COMMIT`/`BUILD_TIME` с assembly fallback, без зависимости от PostgreSQL и без секретов. Добавлены `/healthz` и `/readyz` как aliases для tagged liveness/readiness checks; `/api/health` приведён к DB-independent compatibility liveness. Добавлены pure unit tests provider и обновлены HTTP examples/docs/checklist. Migration не требуется.
- Task 63 — Minimal CI GitHub Actions: добавлен `.github/workflows/ci.yml` для pull requests и push в `main`. Один `ubuntu-latest` job выполняет frontend `npm ci`, typecheck/build и backend restore, Release build/tests на Node.js 24.x и .NET 10.0.x. Используется npm cache по `package-lock.json`; migrations, PostgreSQL, secrets и deployment в CI не запускаются.
- Task 61 — Email outbox + retry: owner/customer Order и Contact emails теперь сохраняются в `EmailOutboxMessages` вместе со связанной Email Log записью `Queued`; `EmailOutboxWorker` выполняет отправку через существующий provider, атомарно claim'ит due jobs, восстанавливает stale `Processing`, применяет retry 1/5/15/60 минут с максимумом 5 попыток и обновляет Email Log в `Retrying`, `Sent` или `Failed`. Migration `20260704181514_AddEmailOutboxMessages` применена к локальной PostgreSQL. Public Order/Contact contracts и auth не изменены; SMTP secrets в outbox не хранятся.

- Task 51.2 — Admin Contact Messages server-side pagination: protected `GET /api/admin/contact-messages` возвращает typed page response и применяет status/search до `CountAsync`, затем стабильную newest-first сортировку и `Skip/Take`. Search покрывает reference, sender name/email/phone, subject, message и status. Admin UI загружает только текущую страницу, показывает total/page size, сбрасывает page при filters/search и refetch текущей страницы после status/delete/SignalR events. Default page size — 25, разрешены 10/25/50/100, максимум — 100. Public `POST /api/contact-messages`, validation, anti-spam и email side effects не изменены; migration не потребовалась.

- Task 51.1 — Admin Orders server-side pagination: protected `GET /api/orders` возвращает typed page response и применяет status/search до `CountAsync`, затем стабильную newest-first сортировку и `Skip/Take`. Search покрывает reference, client name/email/phone, service snapshot и description. Admin UI загружает только текущую страницу, показывает total/page size, сбрасывает page при filters/search и refetch текущей страницы после actions/SignalR events. Default page size — 25, разрешены 10/25/50/100, максимум — 100. Public `POST /api/orders`, details и attachment APIs не изменены; migration не потребовалась.

- Task 51.3 — Server-side pagination для Audit Log и Email Delivery Log: оба protected endpoint возвращают typed page response (`items`, `page`, `pageSize`, `totalItems`, `totalPages`), применяют фильтры до `CountAsync`, сохраняют newest-first sorting и загружают только текущую страницу через `Skip/Take`. Default page size — 25, разрешены 10/25/50/100, максимум — 100. Admin UI показывает page/total/page size, сбрасывает page на 1 при фильтрах и безопасно обновляет текущую Email Log page по realtime event. Migration не потребовалась.

- Task 49.1 — Backend test project foundation: в `backend/tests/BespokeStudio.Tests` добавлен xUnit-проект и включён в `backend/BespokeStudio.sln`. Первые unit-тесты проверяют валидную минимальную заявку, honeypot, формат email, обязательное consent и лимит вложений. Тесты не требуют PostgreSQL, SMTP, ClamAV, файлов или secrets; production behavior и схема БД не изменены.

- Task 56 — Admin 2FA: добавлены TOTP/authenticator setup в My Account, защищённый пятиминутный HttpOnly Data Protection challenge без выдачи JWT/refresh session до второй ступени, вход по TOTP или одноразовому recovery code, regeneration/disable/reset flows, Identity lockout, отдельный 2FA verify rate limit и audit events без secrets/codes. Использованы существующие Identity поля и `AspNetUserTokens`; migration не нужна. QR остаётся необязательным future UX improvement, manual key и `otpauth://` URI уже поддерживаются.

- Task 55.3 — Active Sessions UI: Admin → My account показывает безопасный список логических refresh sessions, current/active/revoked/expired status, browser/device и masked IP. Добавлены revoke одной session и revoke остальных sessions с audit actions `auth.session_revoked` / `auth.other_sessions_revoked`; raw token/hash/cookie не возвращаются. Новая migration не потребовалась.

- Task 55.2 — Auth invalidation + audit: access JWT проверяет актуального Identity user, Admin role, lockout и security stamp; смена собственного пароля и отключение admin user отзывают все refresh sessions; login success/failure, logout, failed/reused refresh и session revocation записываются в audit без паролей, токенов, hashes и cookies. Новая migration не потребовалась.

- Task 55.1 — Refresh token backend foundation: добавлена таблица `AdminRefreshTokens` с SHA-256 hash-only storage, 15-minute access JWT, 14-day HttpOnly refresh cookie, rotation, reuse-family revocation и idempotent backend logout. Frontend выполняет один refresh/retry и не хранит raw refresh token.

- Task 53 — Backend production hardening: добавлены отдельные liveness/readiness health endpoints с проверкой PostgreSQL, централизованные Problem Details для необработанных исключений, доверенные Forwarded Headers для reverse proxy и обязательный production-путь persistent Data Protection keys. Migration не нужна. Для каждого deployment всё ещё необходимо задать точные `KnownProxies`/`KnownNetworks`; внешний secret store и monitoring/alerting остаются операционными задачами.

- Task 52 — Admin modal accessibility: общий `AdminConfirmDialog` получил ARIA-связи, безопасный начальный фокус, focus trap для Tab/Shift+Tab, закрытие по Escape вне loading state и возврат фокуса к вызвавшему элементу.

- Admin-managed Gmail SMTP добавлен в Settings: владелец может выбрать Gmail SMTP, ввести Gmail address и Google App Password, пароль хранится как protected value на backend и никогда не возвращается в API.

- Production Email/SMTP checklist зафиксирован как обязательный pre-production блок: реальная SMTP-отправка, секреты вне Git, Gmail App Password, проверки test email/contact/order delivery, SPF/DKIM/DMARC, мониторинг, retry/background queue и ротация credentials.

- Contact Messages API реализован: public `POST /api/contact-messages` сохраняет сообщения Contact form в PostgreSQL, admin JWT endpoints позволяют просматривать сообщения и менять статусы `New` / `Read` / `Replied` / `Archived`.

- Public Contact page подключена к backend: добавлены loading/success/error states, backend validation handling, optional phone/subject и обязательное consent-подтверждение.

- Admin sidebar получил раздел **Contact Messages**: сообщения видны в списке, открываются в drawer, фильтруются по статусу и сохраняют изменения статуса после refresh.

- Contact messages подключены к существующей email notification foundation: при включённых email notifications и заданном email владельца Logging/SMTP provider получает уведомление о новом сообщении; ошибка отправки не отменяет создание сообщения.

- Неиспользуемая зависимость `recharts` удалена из `package.json`/`package-lock.json`; неиспользуемый shadcn/ui wrapper `src/app/components/ui/chart.tsx` удалён, так как больше не импортировался.

- Routing и deep links переведены на `react-router-dom`: доступны маршруты `/`, `/services`, `/portfolio`, `/order`, `/about`, `/contact`, `/privacy`, `/admin`.
- Неизвестные URL больше не редиректят на главную. Вместо этого используется отдельная `404` page.
- Основной `App.tsx` сокращён до router/layout orchestration.
- Lazy loading страниц включён; основной JS chunk после задачи N4 уменьшился примерно с `646 KB` до `~212 KB` (`212.42 KB` в текущей production-сборке).
- Добавлена отдельная строгая TypeScript-проверка: `npm.cmd run typecheck`.
- `typecheck`, `build` и `npm audit` проходят на текущем состоянии проекта.
- `npm audit` больше не показывает high-severity уязвимости после обновления `react-router`, `react-router-dom` и `vite` в пределах текущих major-веток.
- Home hero и About image вынесены в responsive-структуру `src/assets/images/optimized/` с `WebP + JPEG` fallback, оригиналы сохранены в `src/imports/`.
- Portfolio/card images переведены на оптимизированные локальные derivative-файлы и теперь загружаются с `loading="lazy"` и `decoding="async"`.
- Внешняя decorative image dependency в `HomeHero` больше не использует `images.unsplash.com`; фон переведён на локальный optimized asset.
- Production SPA fallback задокументирован в `DEPLOYMENT_NOTES_RU.md`.
- Backend skeleton создан в `backend/` как отдельный ASP.NET Core Web API solution на `net10.0` с проектами `BespokeStudio.Api`, `BespokeStudio.Domain`, `BespokeStudio.Application`, `BespokeStudio.Infrastructure`.
- В backend уже есть базовые system endpoints: `/api/health`, `/api/version`, Swagger UI и dev CORS под локальный frontend.
- Создан независимый от persistence черновик domain models для `Orders`, `Clients`, `Portfolio`, `Categories`, `Services` и upload metadata.
- Созданы application contracts/DTO и сервисные интерфейсы для будущих модулей `Orders`, `Clients`, `Portfolio`, `Services`, `Uploads`; domain entities не используются как transport responses.
- Backend `bin/obj` удалены из Git tracking без удаления физических файлов; build artefacts теперь игнорируются через `.gitignore`, housekeeping debt закрыт.
- Создан EF Core persistence skeleton для PostgreSQL: `BespokeStudioDbContext`, восемь `IEntityTypeConfiguration<T>`, Fluent API relationships, ограничения и строковый mapping enum находятся в Infrastructure.
- В development-конфигурацию добавлен локальный `ConnectionStrings:BespokeStudioDb`, а в корень проекта — `docker-compose.postgres.yml` для PostgreSQL 16.
- Создана initial migration `InitialCreate` в `BespokeStudio.Infrastructure/Persistence/Migrations`; migration применяется явно, без automatic migration при старте API.
- Реализован Orders/Enquiries API: создание и чтение заявок, обновление статуса, внутренние заметки, базовая validation и PostgreSQL persistence через `IOrderService`.
- Реализован простой client matching: сначала нормализованный email, затем точное совпадение trimmed phone; повторная заявка переиспользует существующего клиента.
- Migration `AllowClientsWithoutEmail` разрешает phone-only enquiries; обе migration применены к локальной PostgreSQL на порту `5433`.
- JSON enum сериализуются строками, поэтому API принимает значения вроде `Dressmaking`, `Contacted` и `MemoryBear`.
- Public Order form подключена к реальному `POST /api/orders`: payload mapping, loading/success/error states и обработка validation problem изолированы во frontend API layer.
- Добавлена backend-аутентификация на ASP.NET Core Identity + JWT Bearer; Identity users/roles хранятся в PostgreSQL, migration `AddIdentityAuth` применена локально.
- `POST /api/orders` остаётся публичным, а Orders list/detail/status/notes защищены policy `AdminOnly` и ролью `Admin`.
- Добавлены `POST /api/auth/login`, защищённый `GET /api/auth/me`, Swagger Bearer authorization и безопасный development seed без credentials в репозитории.
- Frontend admin login подключён к JWT API: access token хранится в `sessionStorage`, `/admin` защищён route guard, logout очищает сессию.
- Admin Orders page использует реальные list/detail/status/note endpoints; изменения статуса и заметки обновляют UI без перезагрузки.
- Order attachments реализованы двухшаговым flow: public multipart upload возвращает IDs, `POST /api/orders` связывает metadata с заявкой, а admin скачивает файл через JWT-protected endpoint.
- PostgreSQL хранит upload metadata и scan status; физические dev-файлы находятся в ignored `backend/storage/uploads`, с generated filenames, allowlist типов, file signature validation, quarantine flow и лимитом `5 MB` на файл.
- Public `POST /api/uploads/order-attachments` и `POST /api/orders` защищены configurable fixed-window rate limiting по remote IP; превышение возвращает `429`, JSON problem details и `Retry-After`.
- Добавлены `IUploadCleanupService`/`UploadCleanupService`: cleanup удаляет только OrderAttachment uploads старше configurable TTL, повторно проверяя отсутствие связи с order перед удалением.
- Ручной `POST /api/uploads/cleanup-orphans` доступен только Admin JWT и возвращает summary по scanned/deleted/missing/skipped; linked attachments не удаляются.
- Добавлен strongly typed singleton `SiteSettings` с migration `AddSiteSettings`, EF configuration, validation, application DTO и `ISiteSettingsService`/`SiteSettingsService`.
- Public contact/social/footer settings доступны через `GET /api/site-settings/public`; admin чтение и изменение защищены policy `AdminOnly` через `GET/PATCH /api/admin/site-settings`.
- Site Settings содержат один email и один phone: email используется публичным сайтом и как email notification destination, phone остаётся только публичным контактом. Legacy-колонки удаляются migrations `NormalizeSiteSettingsContacts` и `RemoveWhatsAppNotifications`.
- Admin Settings page сохраняет единые contact settings и notification toggles; Footer, Home contact section и Contact page используют backend settings с typed fallback при недоступном API.
- Добавлены `INotificationService`, `IEmailNotificationSender`, Logging и SMTP email providers. Новая заявка запускает email владельцу, а ошибки SMTP логируются, переходят на logging fallback и не отменяют создание order. WhatsApp/SMS notifications убраны и сейчас не планируются.
- Добавлен Services & Prices CMS: dynamic `ServiceOffering`, дочерние `ServicePriceOption`, CRUD API, Admin Services editor и public `GET /api/services` с typed frontend fallback.
- Public Home/Services и Order form используют active services из PostgreSQL; новые orders сохраняют nullable `ServiceOfferingId` и `ServiceNameSnapshot`, а legacy enum остаётся fallback для старых клиентов и заказов.
- Delete-or-archive закрыт: неиспользованная услуга удаляется, использованная архивируется и исчезает из новых заявок без потери истории order/email notification.
- English-only cleanup выполнен: переключатели EN/UA удалены из Header/MobileMenu flow, `Language`/`defaultLanguage` state удалён из frontend types/data, UI и fallback/default content остаются английскими.
- Repeatable Content CMS реализован: process steps, studio values, testimonials и privacy subsections перенесены в backend-backed модель `RepeatableContentItem` с public API, protected admin CRUD, EF migration и frontend fallback.
- Public Home/About/Privacy sections подключены к `GET /api/repeatable-content`; Admin sidebar получил раздел **Repeatable Content** для add/edit/hide/show/archive, карточки админки выровнены и расширены после visual polish.

- Admin Dashboard добавлен как backend-backed обзор: карточки новых Orders/Contact Messages, recent Orders, recent Contact Messages, статус Email delivery и подсказка по upload security помогают владельцу быстрее увидеть, что требует внимания.

## Оптимизация изображений

- `src/imports/d2-1.png` (`5.22 MB`, Home hero) -> responsive derivatives:
  - `home-hero-768.webp` `42.3 KB`
  - `home-hero-1280.webp` `90.8 KB`
  - `home-hero-1920.webp` `156.8 KB`
  - JPEG fallback до `291.2 KB` на desktop
- `src/imports/d1-1.png` (`4.44 MB`, About image) -> responsive derivatives:
  - `about-hero-768.webp` `42.5 KB`
  - `about-hero-1280.webp` `77.1 KB`
  - `about-hero-1920.webp` `124.3 KB`
  - JPEG fallback до `226.8 KB` на desktop
- Portfolio/card assets переведены на локальные derivative-файлы шириной до `960px`:
  - `1a.jpg` `333.4 KB` -> `portfolio-1a-960.webp` `245.4 KB`
  - `2.jpg` `429.7 KB` -> `portfolio-2-960.webp` `307.2 KB`
  - остальные portfolio derivatives находятся в диапазоне примерно `35-139 KB` для WebP и `74-205 KB` для JPEG fallback

## Осталось

- Две самые тяжёлые portfolio карточки (`portfolio-1a`, `portfolio-2`) всё ещё заметно крупнее остальных даже после downscale. Следующий шаг по изображениям - отдельные crop-aware thumbnails или AVIF pipeline.
- SPA fallback всё ещё должен быть настроен на production-сервере. В репозитории добавлена только документация, не серверная конфигурация.
- Contact Messages API реализован; Public Order form, Contact form и admin-разделы используют реальные backend endpoints.
- PostgreSQL и EF migrations проверены напрямую через connection string на `127.0.0.1:5433`; Docker CLI доступен, но sandbox не разрешил доступ к Docker daemon/pipe для отдельной проверки container health.
- Portfolio/Gallery CMS реализован: категории, items, active/featured/order, Admin image upload и backend-first public gallery работают через PostgreSQL. Локальные frontend assets остаются typed fallback при недоступном API.
- Website Content CMS реализован для основных текстов и page images Home/About/Services/Portfolio/Order/Contact/Privacy; public frontend использует backend-first данные с typed fallback.
- Repeatable Content CMS реализован для повторяемых блоков Home/About/Privacy: process steps, studio values, testimonials и privacy sections больше не являются только статическим typed data.
- Application services для остальных модулей и отдельные repository abstractions пока не реализованы.
- Value objects и правила нормализации/валидации для email, телефона и денежных значений пока не определены.
- Client matching пока не защищён уникальным normalized email/phone constraint; при конкурентных запросах возможны дубликаты.
- Ручную validation можно позже заменить или дополнить FluentValidation при росте числа команд и правил.
- Task 58 (backend security perimeter) реализован: HSTS вне Development, security headers (`X-Content-Type-Options`, `Referrer-Policy`, `X-Frame-Options: DENY`, `Permissions-Policy`) на всех ответах, baseline CSP (переключается `SecurityHeaders:EnableContentSecurityPolicy`) и per-IP rate limit на `POST /api/auth/login` (`RateLimiting:AuthLoginPermitLimit`/`AuthLoginWindowMinutes`). Оставшееся ограничение: этот backend не отдаёт SPA HTML, поэтому его CSP покрывает только API-ответы; document CSP фронтенда (включая `connect-src` для API и SignalR `wss:`/`ws:`) должен задавать хост фронтенда/reverse proxy. Login rate limit опирается на корректные Forwarded Headers за proxy.
- Для production auth остаются password recovery/email confirmation, session-retention policy и ротация JWT signing key через внешний secret store.
- Production storage provider (S3/Azure Blob/R2), deep content inspection, thumbnail/AVIF generation и image cropper пока не реализованы. Для локального storage добавлены quarantine flow, scan metadata и configurable ClamAV/command-line scanner; production ещё требует фактической настройки ClamAV и мониторинга обновления signatures.
- Автоматическая очистка orphan `PortfolioImage` пока не реализована; существующий cleanup обрабатывает только orphan order attachments. Архивирование portfolio item намеренно сохраняет физический файл.
- Автоматический background orphan cleanup пока не реализован; доступен защищённый ручной endpoint. Для production нужны distributed rate limiting/abuse protection и точные deployment-specific `KnownProxies`/`KnownNetworks` для уже добавленной forwarded-header обработки.
- SMTP provider реализован; есть два режима: developer-managed SMTP через user-secrets/env/secret store и owner-managed Gmail SMTP через Admin Settings с protected App Password. Persistent Data Protection path теперь обязателен при production startup; до production остаются его фактическая настройка, deliverability (SPF/DKIM/DMARC), мониторинг bounce/rejection и операционная ротация credentials.
- Task 59 реализован: admin access token хранится только в module-level memory и не записывается в `sessionStorage`/`localStorage`; reload восстанавливает сессию через HttpOnly refresh cookie и `/api/auth/refresh`, API выполняет не более одного refresh/retry, а SignalR берёт актуальный memory token при connect/reconnect. Оставшиеся browser storage usages отсутствуют; memory-only token снижает persistence risk при XSS, но не заменяет CSP и XSS prevention.
- Task 60 реализован: успешный DB save является границей создания Order; email notification/email delivery logging и SignalR выполняются после сохранения как независимые best-effort side effects. Их ошибка логируется с OrderId/reference и не превращает сохранённый заказ в HTTP 500; request cancellation token после commit не используется.
- Для Email Outbox остаются future-задачи: external alerting/notifications (email/Slack/webhook) по exhausted jobs, external archival/anonymization policy для long-term email metadata, multi-instance locking hardening. (Manual retry — Task 76; admin monitoring — Task 77; retention cleanup succeeded/skipped bodies — Task 78.) Базовый opt-in PostgreSQL integration test набор добавлен — Task 80; future: expand integration tests coverage (auth/2FA, order/contact/upload API flows).
- Service image upload пока не реализован; advanced money/currency model и drag-and-drop reorder для Services/Portfolio можно добавить позже. Rich text page CMS ещё не реализован.
- Полноценный rich-text editor/page builder не реализован: Content CMS и Repeatable Content CMS используют безопасные plain-text поля. Version history/drafts остаются будущими задачами. Multilingual CMS не планируется: проект принят как English-only.
- Production secret management для admin seed и JWT signing key ещё требует внешнего secret store и operational rotation process.


## Обязательный pre-production блок: Email / SMTP

- Выбрать режим реальной отправки: developer-managed `Provider=Smtp` через конфигурацию или owner-managed **Admin Settings > Email delivery > Gmail SMTP**.
- Для developer-managed SMTP настроить `Host`, `Port`, `Username`, `Password`, `FromEmail`, `FromName`, `UseSsl`.
- Для Gmail использовать Google App Password после включения 2-Step Verification; обычный пароль Gmail для SMTP не использовать.
- Локально хранить developer-managed SMTP credentials только в `dotnet user-secrets`; в production — только environment variables или внешний secret store.
- Owner-managed Gmail App Password хранить только как protected value в базе; API не должен возвращать пароль на frontend.
- Для production с owner-managed Gmail SMTP настроить persistent ASP.NET Core Data Protection keys.
- Не хранить raw SMTP credentials, Gmail App Passwords и production отправителей в Git, `appsettings*.json`, screenshots или документации.
- Проверить в Admin Settings включение email notifications и owner/public email.
- Проверить реальную доставку через **Send test email**, затем через public Contact form и public Order form.
- Разделять owner notifications и customer confirmation emails; подтверждения клиенту имеют отдельный toggle и редактируемые plain-text templates в Admin Settings.
- До production настроить SPF, DKIM, DMARC, мониторинг SMTP errors/bounce/rejection, operational credential rotation.
- Мониторить `Retrying`/`Failed` Email Log и outbox jobs; настроить alerting на исчерпанные попытки. Retention cleanup для succeeded/skipped outbox bodies реализован (Task 78); при необходимости включить scheduled worker после согласования retention periods.

## Рекомендации на следующие задачи

- Протестировать Task 30 после применения migration: customer confirmations OFF/ON для Contact form и Order form, отдельно от owner notifications.
- Подготовить фактическую production-конфигурацию хостинга с SPA fallback.
- Добить image pipeline для самых тяжёлых portfolio assets: AVIF или отдельные thumbnails под card layout.
- Спроектировать нормализованные уникальные ключи client matching и обработку конкурентного создания клиентов.

## Task 23 — Brand / Logo / SEO

- Brand/Logo/SEO settings добавлены в singleton `SiteSettings`; logo больше не является только hardcoded asset, но bundled logo сохранён как fallback.
- Header/footer logo, CTA, базовые meta/OG данные и labels/visibility навигации теперь backend-first.
- Brand images используют отдельный `BrandAsset` purpose и публичны только при ссылке из текущих settings; order attachments остаются private.
- SVG upload намеренно не реализован из-за security-рисков. Разрешены JPG, PNG и WebP.
- Task 43 закрыл базовую route-level SEO-разметку, robots.txt и sitemap.xml. Future debt: admin-editable per-page SEO, автоматическая генерация sitemap под production domain, image cropper/thumbnails и production CDN/object storage.

## Task 24 — CMS completeness audit

- Public data flow проверен: SiteSettings управляет контактами/footer, BrandSettings — logo/navigation/CTA/SEO, PageContent — основными page sections, Services/Portfolio APIs — карточками и ценами/галереей, Orders API — заявками.
- Удалены устаревшие inline public values (`Logosha Studio`, старый телефон, часы работы, старые email) и hardcoded footer services. Typed fallback email теперь `null`, пока владелец не задаст его через Site Settings.
- Inline PageContent copy больше не подменяет скрытую backend-секцию. Fallback сосредоточен в `src/data/pageContentData.ts` и используется только при недоступности Content API.
- Admin sidebar очищен от неработающих Overview/Clients/Campaigns/Analytics; видимы только работающие Orders, Services, Portfolio, Content, Brand/SEO и Settings.
- На момент Task 24 process steps, studio values, testimonials и подробные privacy subsections ещё оставались статическими typed data; это закрыто в Task 26 через Repeatable Content CMS. Contact form теперь использует реальный backend endpoint.
- Fallback не является основным источником при доступном backend. Multilingual CMS не планируется: сайт и админка English-only. Rich text editor/page builder остаётся future work.

## Task 25 — English-only cleanup

- Продуктовое решение зафиксировано: сайт и админка работают только на английском языке.
- Переключатели EN/UA удалены из публичного header/mobile navigation.
- Frontend `Language` type, `lang/setLang` state и `defaultLanguage` fallback удалены как неиспользуемые.
- Новые seed/default/fallback данные должны добавляться только на английском языке.
- Multilingual CMS больше не является будущей задачей; при необходимости локализация может быть переоценена отдельным продуктовым решением, но сейчас не планируется.
- Проверки: `npm.cmd run typecheck`, `npm.cmd run build`, `dotnet build backend/BespokeStudio.sln` прошли. Backend build предварительно требовал остановить запущенный `BespokeStudio.Api`, который блокировал DLL-файлы.


## Task 26 — Repeatable Content CMS

- Добавлена backend-модель `RepeatableContentItem`, EF configuration, `DbSet`, migration `AddRepeatableContentCms`, application contracts, validation и `IRepeatableContentService`/`RepeatableContentService`.
- Добавлены public endpoints `GET /api/repeatable-content` и `GET /api/repeatable-content/groups/{groupKey}`.
- Добавлены Admin JWT endpoints `/api/admin/repeatable-content` для просмотра, создания, изменения, hide/show и archive элементов.
- Seed data создан для групп `process-steps`, `studio-values`, `testimonials` и `privacy-sections` на основе текущих English-only fallback данных.
- Frontend public sections подключены backend-first: `ProcessSection`, `StudioValuesSection`, `TestimonialsSection`, About values block и Privacy subsections используют Repeatable Content API с typed fallback при недоступном backend.
- Admin sidebar получил раздел **Repeatable Content**. UI поддерживает фильтр групп, add/edit item, hide/show, archive и refresh публичного repeatable content после сохранения.
- Выполнен visual polish админки: рабочая область справа от sidebar центрирована, карточки шире, actions `Edit / Hide / Archive` отображаются в одну строку.
- Проверено вручную: `/api/health`, `/api/repeatable-content/groups/process-steps`, `/api/repeatable-content` возвращают `200`; frontend Network показывает успешные `200` для `process-steps`, `studio-values`, `testimonials`, `privacy-sections`.

## Task 27 — Remove unused recharts

- Удалена неиспользуемая npm dependency `recharts`.
- Удалён неиспользуемый wrapper `src/app/components/ui/chart.tsx`; поиск показал, что chart-компоненты больше нигде не импортировались.
- Изменены `package.json` и `package-lock.json`.
- Проверки: `npm.cmd run typecheck`, `npm.cmd run build`, `dotnet build backend\BespokeStudio.sln`.


## Task 28 — Contact Messages API

- Добавлена backend-модель `ContactMessage`, enum `ContactMessageStatus`, EF configuration, `DbSet`, migration `AddContactMessages`, application contracts, validation и `IContactMessageService`/`ContactMessageService`.
- Добавлен public endpoint `POST /api/contact-messages` с отдельным rate limit `RateLimiting:PublicContactPermitLimit`.
- Добавлены Admin JWT endpoints `GET /api/admin/contact-messages`, `GET /api/admin/contact-messages/{id}` и `PATCH /api/admin/contact-messages/{id}/status`.
- Public Contact page подключена к backend: форма отправляет реальные сообщения, показывает loading/success/error states, backend validation errors, optional phone/subject и consent checkbox.
- Admin sidebar получил раздел **Contact Messages**. UI поддерживает фильтр `All/New/Read/Replied/Archived`, drawer просмотра и изменение статуса с сохранением после refresh.
- Contact messages подключены к существующей email notification foundation. При включённых email notifications и заданном Site Settings email logging/SMTP provider получает owner notification; ошибка отправки логируется и не отменяет созданное сообщение.
- Runtime-проверки: прямой `POST /api/contact-messages` возвращает `201`, admin list возвращает сохранённые сообщения по Admin JWT, frontend Contact form создаёт сообщения, admin UI отображает их и меняет статус, logging email notification пишет письмо в backend log после clean rebuild/run.
- Проверки: `npm.cmd run typecheck`, `npm.cmd run build`, `dotnet build backend\BespokeStudio.sln`.

## Task 29.0 — Production SMTP checklist docs

- Production Email/SMTP checklist добавлен в обязательные pre-production требования.
- Зафиксировано, что `Provider=Logging` — только development fallback; реальная отправка owner notifications требует developer-managed `Provider=Smtp` или owner-managed `GmailSmtp`.
- Для Gmail зафиксировано требование использовать Google App Password и включённую 2-Step Verification, а не обычный пароль аккаунта.
- Зафиксировано правило хранения SMTP credentials: локально `dotnet user-secrets`, production environment variables/secret store, ничего не коммитить в Git/appsettings/docs.
- Добавлены обязательные проверки: Admin Settings email toggle/owner email, Send test email, Contact form delivery, Order form delivery.
- Customer confirmation emails вынесены в отдельную будущую задачу с отдельным toggle; owner notifications и клиентские подтверждения не смешивать.
- До production остаются SPF/DKIM/DMARC, SMTP error/bounce monitoring, credential rotation и background queue/retry policy.

## Task 29.2 — Admin-managed Gmail SMTP settings

- Добавлен backend/admin режим `GmailSmtp` для owner-managed отправки писем.
- `SiteSettings` расширен полями Email Delivery: provider, Gmail address, protected App Password, sender name, updated timestamp.
- Google App Password защищается через ASP.NET Core Data Protection и никогда не возвращается admin API/frontend.
- Добавлены Admin endpoints `GET/PATCH /api/admin/email-delivery`.
- `ConfiguredEmailNotificationSender` сначала проверяет admin-managed delivery mode, затем использует старый configuration-based Logging/SMTP режим.
- Admin Settings получил блок **Email delivery** с provider select, Gmail address, sender name, App Password replace/clear и короткой Gmail App Password инструкцией.
- Добавлена migration `AddAdminEmailDeliverySettings`.
- В sandbox прошли `npm run typecheck` и `npm run build`; `dotnet build` нужно выполнить локально, так как в sandbox нет .NET SDK.

## Task 30 — Customer confirmation emails

- `SiteSettings` расширен отдельным toggle `CustomerConfirmationEmailsEnabled` и редактируемыми plain-text templates для Order и Contact confirmation emails.
- Admin Settings получил блок **Customer confirmations** с subject/body полями и подсказкой по placeholders.
- Customer confirmations отделены от owner notifications: `Notify me about new requests` отправляет письма владельцу, а `Send automatic confirmation to customers` отправляет письмо клиенту на email из формы.
- Поддерживаются placeholders `{{studioName}}`, `{{customerName}}`, `{{customerEmail}}`, `{{customerPhone}}`, Order-only `{{serviceName}}`, `{{preferredDate}}`, Contact-only `{{messageSubject}}`. Сырые технические GUID/reference не показываются в дефолтных клиентских письмах.
- Ошибки отправки confirmation email логируются и не отменяют уже сохранённые order/contact message.
- Добавлена migration `AddCustomerConfirmationEmailTemplates`.

## Task 31 — Human-readable request numbers

- Orders получили `ReferenceNumber` формата `BSS-ORD-YYYY-000001`; Contact Messages получили `ReferenceNumber` формата `BSS-CON-YYYY-000001`.
- Внутренние GUID `Id` остаются primary key/API routing key, но публичные success screens, admin list/detail, owner notifications и customer template placeholders используют человекочитаемый reference.
- Добавлены PostgreSQL sequences `OrderReferenceSequence` и `ContactMessageReferenceSequence`; migration backfill заполняет reference для существующих записей и добавляет unique indexes.
- Placeholder `{{orderReference}}` теперь рендерит человекочитаемый order reference, `{{contactReference}}` — человекочитаемый contact message reference.


## Task 32 — Search by request reference in admin lists

- Admin Orders получил frontend-поиск по `BSS-ORD-...`, имени клиента, email, телефону, услуге и тексту сообщения с сохранением status filter.
- Admin Contact Messages получил frontend-поиск по `BSS-CON-...`, имени отправителя, email, телефону, subject и preview сообщения с сохранением status filter.
- Backend API не менялся; поиск работает по уже загруженным list DTO и остаётся маленьким UI-only улучшением.

## Task 31.2 — Settings module save buttons

- Admin Settings больше не полагается на одну общую кнопку Save Settings внизу страницы.
- Для модулей General, Contact, Notifications, Customer confirmations и Social links добавлены отдельные кнопки сохранения и локальные success/error сообщения.
- Email delivery сохранил отдельную кнопку Save Email Delivery и отдельную отправку test email.
- Backend API не усложнялся: модульные кнопки используют существующий валидируемый Site Settings update endpoint.

## Task 33 — Public form anti-spam hardening and multi-file upload fix

- Public Order form теперь накапливает выбранные файлы через несколько последовательных selections/drop actions вместо замены предыдущего выбора; лимит 5 файлов и existing frontend validation сохранены.
- Public Order и Contact form отправляют скрытый honeypot field и timestamp открытия формы. Backend validators отклоняют заполненный honeypot, отсутствующий timestamp, слишком быструю отправку и stale submissions до сохранения заявки.
- Anti-spam защита является lightweight hardening поверх существующего rate limiting, без reCAPTCHA/Google dependencies и без изменения UX для реальных клиентов.
- Backend persistence model и migrations не менялись.

## Task 34 — Admin attention counters

- Admin sidebar показывает badges `N new` для Orders и Contact Messages, когда есть новые заявки/сообщения.
- Admin Orders и Contact Messages получили summary cards `New ...` и `Total ...`, чтобы владелец сразу видел объём новых обращений.
- Contact Messages при изменении статуса обновляют счётчики без перезагрузки страницы. Backend API и migrations не менялись.


## Task 36 — Admin production-readiness overview

Status: Done locally after ZIP preparation; requires local verification before commit.

Scope:

- Added a Dashboard production-readiness section using existing admin APIs.
- Checks public contact details, owner notifications, customer confirmations, email delivery, upload security, admin data/API access and DNS email records.
- The checklist is informational and does not display secrets. ClamAV and SPF/DKIM/DMARC still require manual production verification.
- Backend and database schema were not changed.


## Task 37 — Admin visible-list CSV export

Status: Done locally after ZIP preparation; requires local verification before commit.

Scope:

- Added frontend-only CSV export for visible Admin Orders rows. The export respects the current status filter and search query.
- Added frontend-only CSV export for visible Admin Contact Messages rows. The export respects the current status filter and search query.
- CSV files include a UTF-8 BOM and escaped cells for safer opening in spreadsheet tools. Backend API and migrations were not changed.


## Task 38 — SignalR real-time admin updates

Status: Done locally after ZIP preparation; requires local verification before commit.

Scope:

- Added protected `/hubs/admin-notifications` SignalR hub for Admin JWT sessions.
- Backend broadcasts lightweight `AdminDataChanged` events after new Orders, Order status/note changes, new Contact Messages and Contact Message status changes.
- Admin frontend connects while signed in, shows live-update connection status and reloads Orders, Contact Messages, Dashboard counters and sidebar badges after realtime events.
- Manual Refresh buttons remain as fallback. Backend persistence and migrations were not changed.


## Task 39 — Admin users management — Done

Добавлена вкладка Admin → Users для управления администраторами сайта:

- список admin users;
- создание нового admin user с временным паролем;
- reset password без возврата пароля через API;
- enable/disable доступа через Identity lockout;
- delete admin user;
- backend-защита от удаления/отключения текущего пользователя и последнего активного admin;
- роли пока не усложнялись: все управляемые пользователи получают роль `Admin`.

Миграция не потребовалась, используются существующие таблицы ASP.NET Core Identity.

## Task 40 — Admin audit log — Done

Добавлена защищённая вкладка Admin → Audit Log и backend audit trail:

- новая таблица `AdminAuditLogEntries`;
- protected endpoint `GET /api/admin/audit-log` с фильтрами `take`, `search`, `action`, `entityType`, `actorEmail`;
- UI-фильтры, Refresh и Export CSV;
- audit-записи для Admin Users create/enable/disable/reset password/delete;
- audit-записи для Order status/note changes;
- audit-записи для Contact Message status changes;
- audit-записи для Site Settings, Email Delivery и Brand / SEO updates.

Пароли, Gmail App Password и SMTP secrets в audit log не сохраняются.
Следующий возможный шаг: расширить audit coverage на Services, Portfolio,
Website Content и Repeatable Content, если это понадобится владельцу сайта.



## Task 41 — My account / change own password — Done

Добавлена вкладка Admin → My account для текущего администратора:

- отображается текущий email и роли текущей сессии;
- текущий admin может сменить собственный пароль через `POST /api/auth/me/password`;
- смена пароля требует current password, new password и confirm new password;
- backend использует ASP.NET Core Identity и не возвращает/не логирует пароли;
- audit log получает запись `account.password_changed`;
- добавлена кнопка Sign out внутри My account;
- новая migration не потребовалась.

Уже выданные JWT теперь содержат security stamp и отклоняются после смены пароля,
disable/delete пользователя или удаления роли Admin. Смена собственного пароля также
отзывает все refresh sessions и выводит пользователя из admin.

## Task 42 — PostgreSQL backup & restore docs — Done

Добавлена документация `BACKUP_RESTORE_RU.md` для ручного backup/restore:

- PostgreSQL backup через `pg_dump --format=custom`;
- безопасный Windows dev backup через Docker Compose без PowerShell binary redirection;
- restore dev database через `dropdb`, `createdb`, `pg_restore`;
- отдельный backup/restore `backend/storage`;
- production Linux варианты для Docker Compose PostgreSQL и PostgreSQL без Docker;
- pre-deploy и post-deploy checklist;
- предупреждения по персональным данным, storage, SMTP secrets, Google App Password и ASP.NET Core Data Protection keys;
- проверка dump через `pg_restore --list`;
- список того, что нельзя коммитить в Git.

Код не менялся, migration не требовалась. Backup остаётся ручной процедурой;
автоматический scheduled backup, encryption, offsite upload, restore-test job и
retention policy пока не реализованы и зависят от будущего production-хостинга.



## Task 43 — SEO / robots / sitemap / Open Graph — Done

Добавлена базовая SEO-инфраструктура публичного сайта:

- `SeoManager` обновляет `title`, meta description, robots, canonical, Open Graph и Twitter card tags при смене маршрута;
- Home page использует Brand/SEO defaults из backend и добавляет JSON-LD `LocalBusiness`/`ProfessionalService`;
- public routes получили route-specific title/description без выдуманного адреса, графика работы, WhatsApp или лишней географии;
- `/admin` и `/admin/login` получают `noindex, nofollow`;
- `public/robots.txt` блокирует admin routes;
- `public/sitemap.xml` содержит только публичные страницы `/`, `/services`, `/portfolio`, `/order`, `/about`, `/contact`, `/privacy`, `/terms`;
- добавлен `VITE_PUBLIC_SITE_URL` для production canonical/OG origin.

Production canonical origin выбран в Task 69: `https://oksanalogosha.com` (apex, без `www`) и уже проставлен в `public/robots.txt` и `public/sitemap.xml` вместо прежнего placeholder. Позже можно сделать admin-editable per-page SEO и автоматическую генерацию sitemap после выбора production-хостинга.


## Task 44 — Privacy / Terms / data protection pages — Done

Добавлена production-oriented основа для публичных legal/data notices:

- добавлена публичная страница `/terms` с Terms & Service Information;
- Footer теперь ведёт на Privacy Policy и Terms;
- Privacy page расширена понятными блоками по Contact form, Order form, uploaded files и admin/audit records;
- Order form consent теперь ссылается на Privacy Policy и Terms, а также явно упоминает accepted attachments;
- Contact form consent теперь ссылается на Privacy Policy и Terms;
- SEO manager получил route-specific metadata для `/terms`;
- `public/sitemap.xml` дополнен `/terms`.

Тексты являются практическим website notice, а не юридической гарантией. Перед public launch владелец сайта должен финально проверить wording под реальную бизнес-модель, адрес/юрисдикцию, retention policy и payment/cancellation rules.

## Task 44.1 — Production launch checklist marker — Done

Добавлен отдельный `PRODUCTION_LAUNCH_CHECKLIST_RU.md`, чтобы перед публичным
launch не забыть production-only действия:

- заменить placeholder-домен в `public/robots.txt` и `public/sitemap.xml` (выполнено в Task 69: `https://oksanalogosha.com`);
- задать `VITE_PUBLIC_SITE_URL` для production frontend build;
- проверить canonical/Open Graph URLs, `/robots.txt`, `/sitemap.xml` и admin `noindex`;
- финально проверить Privacy/Terms wording, public contact data, services/prices;
- выполнить backup, migrations, SMTP/SPF/DKIM/DMARC, ClamAV, HTTPS, secrets и Data Protection keys checks.

Это checklist-документ, не новая runtime-фича.



## Task 45 — Email delivery log — Done

Добавлена основа Admin → Email Log для контроля email-отправок:

- новая backend entity `EmailDeliveryLogEntry` и service `IEmailDeliveryLogService`;
- protected endpoint `GET /api/admin/email-log` с фильтрами `take`, `search`, `messageType`, `status`, `recipientEmail`, `provider`;
- логируются owner order notifications, customer order confirmations, owner contact notifications, customer contact confirmations и test email;
- UI Admin → Email Log показывает status/type/recipient/subject/provider/related entity/result, auto-apply фильтры, CSV export и обновление через admin realtime events;
- email bodies, SMTP credentials, Gmail App Password, JWT tokens и другие секреты не сохраняются в email log.

Для этой задачи нужна EF migration `AddEmailDeliveryLog`, создаваемая локально командой:

```powershell
dotnet ef migrations add AddEmailDeliveryLog --project backend/src/BespokeStudio.Infrastructure --startup-project backend/src/BespokeStudio.Api --output-dir Persistence/Migrations
```

После генерации migration обязательно проверить, что она находится в
`backend/src/BespokeStudio.Infrastructure/Persistence/Migrations`, содержит
`CreateTable("EmailDeliveryLogEntries")`, имеет `.Designer.cs` и обновляет
`BespokeStudioDbContextModelSnapshot.cs`. Затем применить `dotnet ef database update`.

Future improvements: background retry queue, resend action, retention/cleanup policy, bounce/webhook integration and richer SMTP diagnostics after production provider is chosen.


## Task 46 — Upload cleanup / attachment management — Done

Добавлено безопасное удаление вложений заявки из Admin → Orders:

- protected endpoint `DELETE /api/orders/{orderId}/attachments/{attachmentId}`;
- удаляется связь `OrderAttachments` и соответствующая metadata-запись `UploadedFiles`;
- physical file в локальном `backend/storage/uploads` удаляется best-effort, missing file считается уже удалённым, ошибка file-system логируется;
- endpoint возвращает обновлённый `OrderResponse`, поэтому order drawer обновляется без F5;
- UI получил кнопку Delete на attachment card и confirmation modal в стиле админки;
- добавляется audit log запись `order_attachment.deleted`;
- Order realtime event отправляется после удаления, поэтому связанные admin views могут обновиться.

Migration не нужна: используется существующая схема `OrderAttachments` / `UploadedFiles`. Future improvements: отдельный storage health report, scheduled cleanup для orphan/non-linked site assets, retention policy и production object storage adapter.

## Task 47.1 — Final admin UI polish: filters/buttons/empty states — Done

Проведена первая часть финальной шлифовки admin UI без изменения backend API и
без migration:

- добавлены общие frontend-компоненты `AdminActionButton`, `AdminSearchInput`,
  `AdminFilterDropdown`, `AdminTableState`;
- Admin → Orders получил кастомный dropdown статусов в стиле админки вместо
  native browser select;
- Admin → Orders получил единый search input, Export CSV и Refresh buttons;
- Admin → Contact Messages получил такой же filter/search/actions layout;
- loading/empty states в таблицах Orders и Contact Messages приведены к единому
  визуальному стилю;
- backend-код не менялся, migration не требуется.

Оставшаяся шлифовка: пройти tablet/mobile layout, длинные значения в Settings,
Services/Portfolio/Content формы и единый стиль внутренних drawer status selects.



## Task 47.2 — Admin list deletion and pagination

- Orders and Contact Messages now hide manual Refresh buttons from the main list UI because SignalR realtime updates are the primary refresh path.
- Orders and Contact Messages now show 25 rows per page with styled pagination controls.
- Orders and Contact Messages now have destructive delete actions with confirmation modals in the admin style.
- Deleting an order removes linked attachment metadata/files and internal notes, then records `order.deleted` in the audit log.
- Deleting a contact message records `contact_message.deleted` in the audit log.
- Backend delete endpoints are Admin-only and emit realtime events so open admin sessions reload lists automatically.

## Task 47.5 — Safe admin delete operations

- Удаление Order теперь выполняет удаление notes, attachment links, upload metadata, самого заказа и запись `order.deleted` в одной DB-транзакции с одним `SaveChanges`.
- Удаление Contact Message и запись `contact_message.deleted` также выполняются атомарно в одной DB-транзакции.
- Физические файлы Order удаляются только после успешного commit как best-effort cleanup; отсутствующий или временно недоступный файл не меняет успешный API-ответ на HTTP 500.
- SignalR delete events отправляются после commit как best-effort и при ошибке только записывают warning.
- Migration не требуется. Защищённый ручной orphan cleanup уже существует; периодический background cleanup и production storage reconciliation остаются будущими задачами обслуживания.


## Task 47.3 — Admin Orders/Contact fixed table columns

- В таблицах Admin → Orders и Admin → Contact Messages включён `table-fixed` layout с явными `colgroup` widths.
- Ширина столбцов больше не скачет из-за длинных email, service names, subjects или message previews.
- Длинные значения в name/reference/contact/service/subject/message columns обрезаются через `truncate` или `line-clamp-2`, сохраняя стабильную сетку.
- Таблицы больше не должны выходить за правый край admin content area; длинные значения читаются через drawer/details.

Migration не нужна: это frontend-only UI polish.

## Task 48 — Upload orphan cleanup / storage maintenance — Done

- Добавлен защищённый Admin → Storage (`/admin#storage`) с сохранением секции после F5.
- `GET /api/admin/storage/scan` сверяет `UploadedFiles` с физическими файлами локального upload root и показывает DB/physical counts, общий размер, orphan physical files и missing physical files.
- `POST /api/admin/storage/delete-orphans` не принимает пути от клиента, повторно проверяет DB-ссылки и удаляет только файлы внутри настроенного upload root.
- Absolute server paths не возвращаются; missing DB files остаются report-only.
- Cleanup требует confirmation dialog и записывает `storage.orphan_cleanup` в Audit Log.
- Migration не требуется. Остаются ручной запуск cleanup, отсутствие scheduled background maintenance и отсутствие автоматического исправления missing metadata/production object storage reconciliation.

## Task 57 — Upload deletion outbox / automatic background cleanup — Done

- Добавлена таблица `UploadFileDeletionJobs` и migration `AddUploadFileDeletionOutbox`.
- Удаление одного OrderAttachment и целого Order создаёт deletion jobs в той же DB-транзакции, где удаляются links/metadata; delete API больше не выполняет физическое удаление.
- Hosted `UploadFileDeletionWorker` автоматически обрабатывает due jobs, безопасно проверяет relative storage path, считает attempts и применяет exponential retry/backoff.
- Missing physical file завершается как `Skipped`; ошибки сохраняют только безопасный текст без absolute server path.
- Admin → Storage показывает Pending/Processing/Failed/completed counts и таблицу failed jobs. Ручной orphan cleanup остаётся диагностическим fallback.
- Остаются ограничения: filesystem не участвует в distributed transaction с PostgreSQL, scheduled full reconciliation отсутствует, production object storage adapter ещё не реализован.

## Future backend testing

- Task 49.2: expand PostgreSQL-backed integration tests (auth/2FA, order/contact/upload API flows with dedicated test database lifecycle). Базовый opt-in persistence/migrations/outbox/retention набор выполнен — см. Task 80.
- Task 49.3: покрыть integration-сценарии auth/2FA, order/contact APIs и uploads без использования production credentials.
- Task 63 закрыла минимальную CI automation для frontend typecheck/build и backend build/tests. Остаются отдельными будущими задачами: deployment workflow, Docker/API image build, PostgreSQL integration tests, coverage reports и security/dependency scanning.
- Task 64 добавила безопасный build/version contract; автоматическая передача точных `BUILD_VERSION`, `GIT_COMMIT` и `BUILD_TIME` остаётся задачей будущего release/deployment pipeline.
- После Task 65 и Task 81 tag-based invalidation после admin CMS mutations выполнен; остаются production CDN/reverse-proxy cache headers/strategy и cache hit/miss metrics.
- Текущий foundation не является полным покрытием backend и не заменяет manual production smoke checklist.
- Для очень большого Orders dataset substring search по нескольким полям потребует отдельного анализа PostgreSQL full-text/trigram indexes; schema migration намеренно не добавлялась в Task 51.1.
- Для очень большого Contact Messages dataset substring search по reference/name/email/phone/subject/message также потребует отдельного анализа PostgreSQL full-text/trigram indexes; schema migration намеренно не добавлялась в Task 51.2.

## Task 47.4 — Admin table width correction

- Убраны чрезмерные `min-width` значения, из-за которых Orders/Contact Messages могли вылезать вправо за экран.
- Столбцы переведены на процентные `colgroup` widths внутри `w-full table-fixed`, чтобы таблица занимала доступную ширину admin content area.
- В Orders немного уменьшены Service, Created и Message columns; Message preview ограничен максимум двумя строками с обрезкой.
- В Contact Messages применён тот же принцип для Subject/Created/Message columns.
- Padding в table cells немного уменьшен, чтобы сохранить читаемость без горизонтального переполнения.

Migration не нужна: это frontend-only UI polish.
