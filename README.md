
# Bespoke Sewing Studio frontend

Production-oriented React, Vite and TypeScript frontend for the Bespoke Sewing Studio website, integrated with the ASP.NET Core backend and PostgreSQL-backed CMS. The product is English-only; EN/UA language switching and multilingual CMS are not part of the current scope.

## Frontend data mode

The public site is backend-first and CMS-driven: contact settings, brand/navigation/SEO,
page content, repeatable content, services/prices, portfolio data and Contact form submissions load from or write to the ASP.NET Core API.
Centralised typed frontend defaults are used only when the corresponding public API
cannot be reached. The public Order form sends real requests to
`POST /api/orders`, which persists enquiries in PostgreSQL through the ASP.NET
Core backend. The database save is the source of truth for order creation;
email and admin realtime notifications run afterward as best-effort side effects,
so their failure cannot turn a persisted order into a failed public response.
The public Contact form sends real requests to
`POST /api/contact-messages`, which stores messages in PostgreSQL and can notify
the owner by email. The admin login, Orders and Contact Messages screens also use the backend API; the
admin Services, Portfolio, Content, Repeatable Content, Settings and Brand/SEO sections use protected backend APIs. Optional order
attachments are uploaded first and linked to the created enquiry by ID.

The UI is English-only. Header and mobile language switchers have been removed, and typed fallback/default content should remain English-only.

API configuration lives in `src/config/appConfig.ts`. `VITE_API_BASE_URL`
defaults to `http://localhost:5099/api` for local development. Production frontend
builds should set `VITE_PUBLIC_SITE_URL=https://oksanalogosha.com` (the canonical
apex origin, no `www`), which is used for client-side canonical and Open Graph
URLs, and typically use same-origin `VITE_API_BASE_URL=/api`. CMS asset URLs
(portfolio, content and brand images) are resolved by `resolveApiAssetUrl`, which
supports both the absolute development API base and production relative `/api`.
Copy `.env.example` to `.env.local` when an explicit override is needed;
`.env.local` is ignored by Git.

## Backend

The backend lives in `backend/` as a separate ASP.NET Core Web API skeleton (`net10.0`).

Current backend status:

- Swagger/OpenAPI is enabled
- `/health` and `/health/live` provide liveness checks, `/health/ready` verifies PostgreSQL readiness, and `/healthz` plus `/readyz` provide monitoring-compatible aliases
- `/api/health` remains a database-independent compatibility liveness endpoint, while `/api/version` returns safe application, assembly/build, environment, framework and process-start metadata
- public CMS JSON endpoints use a 60-second server-side ASP.NET Core output cache; authenticated/admin requests, forms and file responses are not cached; successful admin CMS/settings updates evict matching cache tags immediately (see `backend/README.md`)
- EF Core persistence is configured for local PostgreSQL development
- migrations are applied explicitly with `dotnet ef database update`
- Orders/enquiries API now persists data in PostgreSQL
- ASP.NET Core Identity, short-lived JWT Bearer access tokens and rotating HttpOnly refresh cookies protect administration routes
- `/api/auth/login`, `/api/auth/2fa/verify`, `/api/auth/refresh`, `/api/auth/logout`, `/api/auth/me` and protected `/api/auth/sessions` endpoints provide optional TOTP two-factor authentication and persistent revocable admin sessions
- the public Order form calls `POST /api/orders`
- the public Contact form calls `POST /api/contact-messages` and persists messages in PostgreSQL
- the public Order form accepts JPG, PNG, WebP and PDF attachments up to 5 MB each
- attachment metadata, including upload scan status, is stored in PostgreSQL; development files are stored under `backend/storage/uploads`
- public upload, order creation and Contact form endpoints use configurable per-IP rate limits and lightweight honeypot/timing anti-spam checks
- `POST /api/auth/login` and `POST /api/auth/2fa/verify` have separate per-IP rate limits (default 10 attempts / 15 minutes) in addition to Identity account lockout; `/api/auth/refresh` and other admin endpoints are not rate limited
- all API responses include baseline security headers (`X-Content-Type-Options`, `Referrer-Policy`, `X-Frame-Options: DENY`, `Permissions-Policy` and a configurable baseline `Content-Security-Policy`), with HSTS enabled outside Development; the frontend host still sets its own document CSP (see `backend/README.md`)
- order and attachment deletion schedules DB-backed physical-file cleanup jobs; a hosted worker processes them automatically with retry/backoff
- administrators can manually remove expired orphan uploads through a protected fallback cleanup endpoint
- Admin **Storage** shows automatic cleanup job health, compares database metadata with local files and provides diagnostic orphan cleanup
- public contact, social and footer settings load from `GET /api/site-settings/public`
- the public Footer shows a dynamic copyright year and a developer credit linking to `https://ds-cores.com` (Privacy / Terms / Studio Login unchanged)
- the Admin **Settings** section edits public contact and notification settings
- the Admin **Contact Messages** section lists Contact form messages and manages their workflow status
- public services/prices load from `GET /api/services`
- the Admin **Services** section creates, edits, hides, features, deletes or archives services and price options
- public portfolio/gallery data loads from `GET /api/portfolio`
- the Admin **Portfolio** section manages categories, work items, publication state, order and featured items
- portfolio images are uploaded to local development storage and served publicly only while linked to an active portfolio item
- IN STOCK ready-to-buy catalogue: public `/in-stock` and `/in-stock/:slug`, API `GET /api/in-stock` (+ admin `/api/admin/in-stock/*`), Work → **IN STOCK** admin UI, Brand Settings nav label/visibility, SEO/JSON-LD, and backend-generated `/sitemap.xml` (published items only; no checkout)
- Shared multipart upload progress uses an XHR transport (`src/api/uploadTransport.ts`) with a reusable admin/public upload progress control (real transfer %, then Scanning file…); Vitest covers the transport, upload state machine and IN STOCK admin/public helpers
- page headings, body text, CTAs and key page images load from the Website Content CMS
- repeatable public blocks such as process steps, studio values, testimonials and privacy subsections load from the Repeatable Content CMS
- logo, favicon, default SEO metadata, header CTA and navigation labels/visibility are managed in Admin **Brand / SEO**
- public routes update route-specific SEO metadata, canonical links, Open Graph/Twitter card tags and Home JSON-LD from frontend route state plus Brand/SEO settings
- the public Order form submits a dynamic `serviceOfferingId` while preserving legacy enum compatibility
- admin login, Orders list/detail/status/notes and Contact Messages list/detail/status use protected backend endpoints
- the admin sidebar groups backend-backed modules into **Work**, **Website**, **Administration** and **Account** so daily owner tasks are separated from technical operations
- the Admin Dashboard focuses on recent customer activity and compact status signals, while detailed production-readiness checks live under **Administration → Production Health**
- admin Orders, Contact Messages, Dashboard counters, sidebar badges and Email Log can refresh through the protected SignalR admin-notifications hub
- admin Orders and Contact Messages lists use styled filters, fixed-width tables, server-side pagination and shared destructive confirmation dialogs with keyboard focus management and ARIA labelling
- the Admin **Audit Log** section lists important administrator actions from the protected backend audit log
- the Admin **Email Log** section lists owner notifications, customer confirmations and test email attempts
- Order and Contact emails are queued in PostgreSQL and delivered by a background worker with bounded retry/backoff

Local PostgreSQL and backend setup:

```powershell
docker compose -f docker-compose.postgres.yml config
docker compose -f docker-compose.postgres.yml up -d
dotnet ef database update --project backend/src/BespokeStudio.Infrastructure --startup-project backend/src/BespokeStudio.Api
dotnet run --project backend/src/BespokeStudio.Api/BespokeStudio.Api.csproj
```

Check the container and existing API endpoints with:

```powershell
docker compose -f docker-compose.postgres.yml ps
Invoke-WebRequest http://localhost:5099/api/health -UseBasicParsing
Invoke-WebRequest http://localhost:5099/api/version -UseBasicParsing
Invoke-WebRequest http://localhost:5099/health/live -UseBasicParsing
Invoke-WebRequest http://localhost:5099/health/ready -UseBasicParsing
Invoke-WebRequest http://localhost:5099/healthz -UseBasicParsing
Invoke-WebRequest http://localhost:5099/readyz -UseBasicParsing
Invoke-WebRequest http://localhost:5099/swagger/index.html -UseBasicParsing
```

`/health/live`, `/healthz`, `/api/health` and `/api/version` do not query
PostgreSQL. `/health/ready` and `/readyz` return `503` when PostgreSQL is not
available. A CI or release pipeline may set `BUILD_VERSION`, `GIT_COMMIT` and
`BUILD_TIME`; otherwise `/api/version` uses assembly version metadata and null
values for unavailable optional build fields. None of these endpoints returns
connection strings, credentials, internal paths or exception details.

Public read-only CMS responses for services, portfolio data, page content,
repeatable content, public site settings and public brand/SEO settings are held
in the API's in-memory output cache for 60 seconds. Admin/auth APIs, Order and
Contact submissions, uploads/downloads, images, health and version endpoints do
not use this policy. After a successful admin CMS or settings mutation, matching
output-cache tags are evicted so public JSON reflects the change without waiting
for the full TTL. Browser/CDN cache headers are intentionally not
relied on; production reverse-proxy or CDN caching needs a separate reviewed policy.

Persistence, admin user-secrets, migration, login, and Swagger Bearer commands
are documented in `backend/README.md`. No administrator credentials are stored
in the repository.

Configure the development administrator before starting the backend:

```powershell
dotnet user-secrets set "SeedAdmin:Email" "admin@example.com" --project backend/src/BespokeStudio.Api
dotnet user-secrets set "SeedAdmin:Password" "replace-with-a-strong-local-password" --project backend/src/BespokeStudio.Api
```

The seed only creates a missing administrator and does not replace an existing
password. Apply migrations before starting the API, then open
`http://127.0.0.1:5173/admin/login`. The frontend keeps the short-lived JWT
access token only in module memory; it is never persisted in `sessionStorage`
or `localStorage`. After a browser reload, the admin session is restored through
`POST /api/auth/refresh` using the rotating HttpOnly refresh cookie. The refresh
token is not readable by frontend JavaScript, and only its SHA-256 hash is stored
in PostgreSQL. Signing out revokes
the refresh token and clears the cookie. Changing a password or disabling an
administrator revokes all of that user's refresh sessions. Access JWT validation
also checks the current Identity user, Admin role, lockout state and security stamp,
so stale tokens are rejected. SignalR reads the latest in-memory token for each
connection or reconnect. Memory-only storage reduces persistent token exposure
under XSS but does not replace CSP and normal XSS prevention. Passwords are never
stored by the frontend.

When two-factor authentication is enabled, a successful password check creates
only a five-minute Data Protection-protected HttpOnly challenge cookie. It does
not issue an access token, refresh token or refresh-session row. The final normal
session is created only after `POST /api/auth/2fa/verify` accepts a TOTP or one-time
recovery code. The challenge contains no password or token and is inaccessible to
frontend JavaScript.

Start the frontend in a second PowerShell window:

```powershell
npm.cmd run dev -- --host 127.0.0.1
```

The backend must be available at the configured `VITE_API_BASE_URL` before an
Order form submission or admin sign-in. Select up to five files in the public
Order form across one or multiple selections; after submission, open the enquiry
in `/admin` to download or delete its protected attachments. Attachment deletion is Admin-only: the database link, metadata and a safe relative-path deletion job are committed together, then a hosted worker removes the physical file independently. Full order deletion queues one job per attachment in the same transaction as the order, notes, metadata and audit deletion. Filesystem failures therefore cannot turn a successful delete request into HTTP 500. `backend/storage/` is ignored by Git.

Public `POST /api/uploads/order-attachments` requests are limited to 10 per 10
minutes per IP, public `POST /api/orders` requests to 5 per 10 minutes per IP,
and public `POST /api/contact-messages` requests to 5 per 10 minutes per IP by default. A rejected request returns `429` and a `Retry-After` header; the
Order form displays the API message without exposing server details. Order and
Contact submissions also include hidden honeypot/timing fields; filled honeypots,
missing timestamps and unrealistically fast or stale submissions are rejected before
records are saved.

An upload that is not linked to an order and is older than the configured
`UploadStorage:OrphanCleanupAgeMinutes` TTL (120 minutes by default) can be
removed by an administrator through `POST /api/uploads/cleanup-orphans` using
an Admin JWT. This manual cleanup is a diagnostic/emergency fallback; ordinary
order-file deletion is automatic through the DB-backed deletion queue. Local uploads are written to a
quarantine folder first, validated by file signature, optionally scanned through
configured ClamAV/command-line malware scanning, and moved to final storage only
when accepted. The default development scanner provider is `Disabled`; production
must configure ClamAV before accepting uploads. Production object storage is not
implemented.

Before accepting real customer uploads, configure the ClamAV/command-line scanner
and a production `UploadStorage__RootPath`. The full production uploads/ClamAV
runbook (storage path, ClamAV install, EICAR smoke test, backup/restore and
troubleshooting) is in [`UPLOADS_PRODUCTION_RU.md`](UPLOADS_PRODUCTION_RU.md).
Object storage (S3/Azure/R2) remains future work.

## Services and prices

Sign in at `http://127.0.0.1:5173/admin`, then select **Services & Prices**. The owner can
create and edit services, add text-based price options, control display order,
mark services Featured for the Home preview, and hide inactive services. Public
Home/Services pages and the Order form read the active list from PostgreSQL via
`GET /api/services`; typed fallback services keep the public UI available if the
API is offline.

Deleting an unused service removes it. A service referenced by an existing
order is archived and hidden from new enquiries instead, while the order keeps
its stored service-name snapshot. Service image upload and drag-and-drop order
editing are not implemented.


## Admin users

Sign in at `http://127.0.0.1:5173/admin`, then select **Administration → Users**. The
owner can list administrator accounts, create another Admin user, reset a
temporary password, disable/enable access, or delete an unused admin account.
All managed users currently receive the single `Admin` role; editor/viewer roles
are not implemented.

## Admin navigation structure

The Admin UI is organized for owner workflows:

- **Work**: Dashboard, Orders, Messages, Services & Prices and Portfolio.
- **Website**: Site Pages, Website Blocks, Business Info and Brand / SEO.
- **Administration**: Users, Email Log, Storage, Audit Log, Production Health and System Settings.
- **Account**: My Account remains available in the sidebar, with Sign out and Back to Website actions pinned at the bottom.

Legacy admin hash links such as `#settings` are still accepted and open the
current System Settings view. No backend routes or API contracts are changed by
this navigation grouping.

Safety rules are enforced on the backend: an administrator cannot disable or
delete their own current account, and the last active admin account cannot be
disabled or deleted. Passwords are accepted only when creating or resetting a
user and are never returned by the API.

## My account

Sign in to Admin and select **My account** to review the current administrator
session, sign out, or change your own password. Changing the password requires
the current password, a new password and confirmation. The backend uses ASP.NET
Core Identity password validation and records an `account.password_changed` audit
entry without storing the old or new password. A successful password change updates
the security stamp, revokes all refresh sessions, clears the cookie and signs the
administrator out so they must use the new password.

The same **My account** page lists logical refresh-session families with current,
active, revoked or expired status, safe browser/device details and masked IP data.
An administrator can revoke one session or all other sessions. Revoking the current
session clears its HttpOnly cookie and signs the browser out. Raw refresh tokens,
hashes and cookie values are never returned to the UI.

The **Two-factor authentication** section uses ASP.NET Core Identity authenticator
keys and TOTP. Setup shows a manual key and `otpauth://` URI to the current admin;
no external QR dependency is required. Enabling 2FA generates ten recovery codes
that are returned and shown once, so the administrator must store them securely.
The current administrator can regenerate recovery codes, disable 2FA or reset the
authenticator after confirming the current password. Secrets, codes and URIs are
never written to logs or audit entries.

Identity security-stamp changes during setup/enable/reset invalidate the previous
access JWT. Those authenticated responses therefore include a replacement JWT,
which the frontend installs directly into the existing module-memory token store;
the refresh cookie remains HttpOnly and no extra refresh session is created.

## Admin audit log

Sign in to Admin and select **Audit Log** to review important administrator
actions. The protected `GET /api/admin/audit-log` endpoint returns the newest
audit entries and supports filtering by search text, action, entity type and
actor email. Results use server-side pagination with a default page size of 25,
allowed sizes of 10/25/50/100 and a maximum of 100. The UI requests only the
current page, shows the filtered total and can export the visible page to CSV.

The audit scope records login success/failure, logout, failed/reused refresh,
individual/other-session revocation, admin user management, own-account password changes,
order status/note/attachment changes, contact message status changes, Site Settings, Email Delivery and Brand / SEO
updates. The audit log stores actor email, action, entity type, entity label,
summary and timestamp; it intentionally does not store passwords, access/refresh
tokens, token hashes, cookies or SMTP/Gmail App Password secrets.


## Email log

Sign in to Admin and select **Email Log** to review email delivery attempts for
owner notifications, customer confirmations and test emails. The protected
`GET /api/admin/email-log` endpoint returns the newest entries and supports
filtering by search text, message type, status, recipient email and provider.
Results use the same server-side pagination contract: 25 items by default and
100 maximum. The UI applies filters automatically, refreshes the current page
from admin realtime events without appending duplicate rows and can export the
visible page to CSV. Failed rows expose a **Retry** action that queues a manual
retry for the background worker after the SMTP/provider issue is fixed; email
bodies are never shown and the automatic retry/backoff behaviour is unchanged.

The Admin **Dashboard** and **Email Log** also show a read-only outbox health
summary (Healthy/Warning/Critical) with global failed, retrying, pending/stale
and sent-in-24h counts, so the owner can quickly spot exhausted failed messages,
scheduled retries or stale pending messages. The summary comes from
`GET /api/admin/email-log/summary`, is monitoring-only and never exposes email
bodies or secrets.

Admin **Email Log** also exposes retention cleanup status and a manual **Run
cleanup** action. Old `Succeeded`/`Skipped` outbox bodies are replaced with a
safe placeholder and very old outbox rows can be deleted; `EmailDeliveryLog`
entries remain and failed messages are retained for review/manual retry. See
`GET /api/admin/email-log/retention` and
`POST /api/admin/email-log/retention/cleanup`. The optional background worker is
disabled by default (`EmailOutboxRetention:WorkerEnabled=false`).

The Email Log UI separates **Global outbox health** and **Retention cleanup**
(global counts) from **Current page** log stats, refreshes entries and summaries
together from the main **Refresh** button, and shows safe cleanup result counts
after manual retention cleanup.

The log stores delivery metadata only: recipient email, subject, provider,
status, result/error summary, related Order/Contact reference and timestamps.
It intentionally does not expose email bodies, SMTP credentials, Google App
Passwords or JWT tokens. Prepared email bodies are stored only in the protected
`EmailOutboxMessages` table so the worker can deliver them after the request ends.
Queued rows appear as `Queued`, temporary failures as `Retrying`, successful
delivery as `Sent`, and exhausted retries as `Failed`.


## Order attachment management

In Admin **Orders**, open an enquiry drawer to review attachments. Administrators
can download a protected attachment or delete it after a styled confirmation
dialog. Deleting an attachment removes the `OrderAttachments` link, removes the
corresponding `UploadedFiles` metadata row, schedules a physical-file deletion
job in the same transaction and records an `order_attachment.deleted` audit log
entry. The background worker treats missing files as completed and retries safe
filesystem failures with backoff.

## Admin list layout

Admin **Orders** and **Contact Messages** use fixed-width table layouts with
short previews for long fields. Service/subject and message preview columns are
limited to two lines in the list view so the table does not shift or overflow
when rows contain long text. Full message/order details remain available by
opening the drawer.

Admin **Orders** uses server-side pagination and requests only the current page.
Search across reference, client name, email, phone, service and description, plus
the status filter, are applied by the backend before the total is calculated.
Pages are sorted newest first. The default page size is 25, available sizes are
10/25/50/100 and the maximum is 100. CSV export contains the currently visible
page. Order details, status/note actions, attachments and the public Order form
retain their existing APIs and behaviour.

Admin **Contact Messages** also requests only the current server-side page.
Reference, sender name, email, phone, subject, message and status search, plus the
status filter, are applied before the backend calculates the total. Results keep
newest-first ordering. The default page size is 25, supported sizes are
10/25/50/100 and the maximum is 100. Status changes, deletion and SignalR events
refetch the current page; the public Contact form submission flow is unchanged.

## Admin frontend structure

The admin frontend is split into focused modules for navigation/hash routing,
dashboard overview, realtime connection status, attention counters and Order CSV
export. `AdminPage.tsx` remains the orchestration layer for authentication, data
loading, realtime events, selected-section state and panel wiring; existing admin
API contracts and visual behaviour are unchanged.

## Backup and restore

Operational PostgreSQL and uploads backup/restore procedures are documented in
`BACKUP_RESTORE_RU.md`, which is the final production backup/restore runbook. A
complete production backup includes the PostgreSQL database dump, the full uploads
storage root, the ASP.NET Core Data Protection keys, protected secrets/config
kept outside Git, and the reverse proxy / TLS operational config. Before
production deploys or EF migration updates, make a PostgreSQL dump and a matching
`backend/storage` backup, verify the dump with `pg_restore --list`, and keep
backups outside the repository. A database-only backup is not enough: it omits
physical upload files, and owner-managed Gmail SMTP plus 2FA depend on persistent
Data Protection keys. Run a restore rehearsal on a test/staging environment at
least once before the production launch.

A draft/reference production backup script lives in
[`scripts/production/README_RU.md`](scripts/production/README_RU.md)
(`Backup-Production.ps1`). It helps automate dump/archives/metadata/verification,
but real secrets and backup artifacts must stay outside Git.

## Production launch checklist

Before the first public launch or a production-domain migration, complete
[`RELEASE_READINESS_REVIEW_RU.md`](RELEASE_READINESS_REVIEW_RU.md) — the final
release readiness review and Go/No-Go summary. It does not replace the detailed
runbooks; it collects repository readiness vs operator-only production steps,
blockers, warnings and the release decision log template.

Then adapt and follow [`PRODUCTION_DEPLOYMENT_PLAN_RU.md`](PRODUCTION_DEPLOYMENT_PLAN_RU.md)
— the practical server deployment checklist (build artifacts, env placeholders,
DB migrations, backup, deployment sequence, smoke tests, rollback). It comes after
the readiness review and before the final public HTTPS smoke test.

The current supported production target is netcup `prod01` behind Cloudflare
Full (strict), Caddy and Docker Compose. Use
[`PRODUCTION_DEPLOYMENT_RU.md`](PRODUCTION_DEPLOYMENT_RU.md) and
[`DEPLOY_NETCUP_RU.md`](DEPLOY_NETCUP_RU.md) for the concrete netcup runbook,
[`docker-compose.production.yml`](docker-compose.production.yml) for the compose
configuration, and `scripts/production/netcup-*` for release build, deploy,
backup, checks and home-server data migration. `netcup-deploy-release.ps1` sends
remote bash over SSH stdin with CRLF normalized to LF (Windows PowerShell
here-strings must not deliver `set -euo pipefail\r` to bash). The old home-server
deployment (`192.168.2.202`, home Nginx, Cloudflare Tunnel/cloudflared, systemd
service and old `/var/www`/`/var/lib` paths) is deprecated and must only be
referenced as the source of a one-time migration.

On the day of the release, follow
[`PRODUCTION_GO_LIVE_RU.md`](PRODUCTION_GO_LIVE_RU.md), a short day-of-launch
runbook with the deployment order and Go/No-Go criteria; the detailed checklist
stays in `PRODUCTION_LAUNCH_CHECKLIST_RU.md`. Before launch you need green checks
(typecheck, build, tests, CI), a verified backup with a restore rehearsal,
working HTTPS/reverse proxy, a chosen SMTP strategy, configured uploads/ClamAV,
persistent Data Protection keys and a final public HTTPS smoke test.

Before the first public launch or a production-domain migration, review
`PRODUCTION_LAUNCH_CHECKLIST_RU.md`. The canonical production origin is
`https://oksanalogosha.com` and is already used in `public/robots.txt` and the
backend-generated `/sitemap.xml` (includes `/in-stock` and published IN STOCK
item URLs). Set `VITE_PUBLIC_SITE_URL=https://oksanalogosha.com` for the
production frontend build, verify `/robots.txt` and `/sitemap.xml` resolve on
`https://oksanalogosha.com`, and complete the backup, email, upload-security,
HTTPS, secrets and legal-text checks.
The production reverse proxy must supply trusted forwarded headers and its exact
proxy addresses or networks must be configured. Cloudflare plus a reverse proxy
must be configured before the public launch, Kestrel must never be exposed
directly to the internet, and the production build/env must not point the API to
`localhost`. The full production reverse proxy / HTTPS runbook (Cloudflare, TLS,
forwarded headers, WebSockets, HSTS, health checks, smoke test and
troubleshooting) is in
[`REVERSE_PROXY_HTTPS_PRODUCTION_RU.md`](REVERSE_PROXY_HTTPS_PRODUCTION_RU.md).
`DataProtection__KeysPath` is
required in production (startup fails without it) and must point to persistent
storage outside the repository and deployment directory. These keys protect the
owner-managed Gmail SMTP App Password and the 2FA challenge cookie, so they must
be persistent and backed up. The full production Data Protection runbook is in
[`DATA_PROTECTION_PRODUCTION_RU.md`](DATA_PROTECTION_PRODUCTION_RU.md).

## Site settings

Sign in at `http://127.0.0.1:5173/admin` to open the Admin **Dashboard**.
The dashboard summarises new Orders, new Contact Messages, recent activity,
email delivery status, upload-security status and production-readiness checks.
The readiness section never displays secrets. Email delivery, Email outbox,
Upload security and DNS email records now come from the protected backend
`GET /api/admin/production-readiness` endpoint: it verifies configured Resend or
Gmail settings, checks outbox health, runs a lightweight ClamAV clean-file probe
when ClamAV is configured, and performs DNS TXT/MX lookups for the production
sender records. A green Upload security or DNS status means the backend observed
the check directly, not just a manual reminder.

Sign in at `http://127.0.0.1:5173/admin`, then select **Settings** in the
sidebar. The administrator edits one email and one contact phone, plus
contact/footer text, service area, social URLs, and the email notification
toggle. Settings are grouped into modules with their own save actions for
General, Contact, Notifications, Customer confirmations, Email delivery and
Social links. The email is shown on the public site and is also the owner notification
destination. The phone is public contact information only.

Enable owner new-request notifications with **Notify me about new requests** in
Admin Settings. The default development provider writes email content to the
backend log. The **Email delivery** block in Admin Settings can keep
developer-managed configuration (`Configuration`), use owner-managed `Gmail
SMTP`, or use production `Resend API`. Resend stores only a protected API key on
the backend and never returns it through admin APIs. Production defaults are
`From: Bespoke Sewing Studio <noreply@oksanalogosha.com>` and
`Reply-To: contact@oksanalogosha.com`. Gmail SMTP remains available as fallback;
its Google App Password is protected on the backend, never returned by the API,
and can be replaced or cleared from the admin UI.

Customer confirmation emails are separate from owner notifications. The
**Customer confirmations** block in Admin Settings has its own toggle and
plain-text subject/body templates for Order and Contact confirmations. Supported
placeholders include `{{studioName}}`, `{{customerName}}`, `{{customerEmail}}`,
`{{customerPhone}}`, plus Order-only `{{serviceName}}`, `{{preferredDate}}`,
`{{orderReference}}`, and Contact-only `{{messageSubject}}`, `{{contactReference}}`. WhatsApp and SMS notifications are not implemented or
planned for the current product scope. Public pages keep their typed fallback
content if the API cannot be reached.

Order and Contact notification preparation now writes an email job and a linked
Email Log entry to PostgreSQL. `EmailOutboxWorker` sends due jobs outside the
public request, retries temporary failures after 1, 5, 15 and up to 60 minutes,
and stops after five attempts by default. Public Order/Contact creation therefore
does not depend on immediate SMTP availability. Delivery can be delayed by the
configured worker interval (30 seconds by default).

Orders and Contact Messages now keep human-readable request references in addition
to their internal GUID IDs. Order references use `BSS-ORD-YYYY-000001`; Contact
Message references use `BSS-CON-YYYY-000001`. Admin lists, detail drawers, owner
notifications, customer template placeholders and public success screens use
these references so customers do not see raw database IDs. Admin Orders and
Contact Messages lists can also be searched by reference number, client/sender,
email, phone and message content while keeping the status filters. The currently
visible Orders and Contact Messages rows can be exported to CSV for Excel or
Google Sheets, and the export respects the active search and status filter. Admin
sidebar badges and page summary cards show new/total Orders and Contact Messages
so the owner can quickly see requests that need attention. The Admin Dashboard
gives a quick overview with new request counters, recent Orders, recent Contact
Messages, email delivery mode and upload security guidance. When the admin UI is
open, a protected SignalR/WebSocket connection listens for new Order and Contact
Message events so Dashboard counters, sidebar badges and visible admin lists can
refresh without a full browser reload. Manual Refresh buttons remain as a fallback.

Orders and Contact Messages use shared admin UI controls for filter dropdowns,
search fields, action buttons and table empty/loading states. This keeps the
core request-management screens visually aligned with Audit Log and Email Log
without changing backend APIs.

## Contact messages

The public Contact page sends real enquiries to `POST /api/contact-messages`.
The backend validates name, email, optional phone, optional subject, message,
consent and lightweight anti-spam fields, stores the message in PostgreSQL, assigns a human-readable reference
like `BSS-CON-2026-000001`, and returns `201 Created`. The form
shows loading, success and validation/API error states and clears after a
successful submission.

Sign in at `http://127.0.0.1:5173/admin`, then select **Contact Messages** to
view Contact form messages, filter by status, search by `BSS-CON-...` reference
or sender/contact details, and update the workflow status: `New`, `Read`,
`Replied` or `Archived`. New contact messages use the same owner email
notification foundation as Orders when email notifications are enabled in Site
Settings. In development the default logging provider writes the email
content to the backend log; SMTP can be configured through user-secrets or
environment variables.



## Production email / Resend checklist

Owner notifications for Orders and Contact Messages are already implemented, but
local development uses `Provider=Logging` by default. Real email delivery is a
mandatory production setup item and must be configured outside source control.

Cloudflare Email Routing is used for incoming mail:
`contact@oksanalogosha.com -> bespoke.studio.ni@gmail.com` and
`orders@oksanalogosha.com -> bespoke.studio.ni@gmail.com`. Resend API is used
for outgoing production mail from `noreply@oksanalogosha.com` with
`Reply-To: contact@oksanalogosha.com`. Gmail SMTP can remain as fallback, but it
should not be the primary production provider when Resend is configured.

The full step-by-step production runbook (email provider setup, Cloudflare DNS
SPF/DKIM/DMARC checklist for `oksanalogosha.com`, and a production smoke test)
lives in [`SMTP_PRODUCTION_RU.md`](SMTP_PRODUCTION_RU.md). Cloudflare DNS is
needed for SPF/DKIM/DMARC records and incoming Email Routing, but it does not
send outbound application email. Never commit Resend API keys, SMTP/Gmail
secrets or screenshots containing them to Git.

Before production release:

- choose one delivery mode:
  - owner-managed **Admin > Settings > Email delivery > Resend API** for the
    production sender `noreply@oksanalogosha.com`
  - developer-managed `Notifications:Email:Provider=Smtp` through user-secrets,
    environment variables or a secret store
  - owner-managed **Admin > Settings > Email delivery > Gmail SMTP** as fallback
- keep raw SMTP credentials out of Git, committed `appsettings*.json`, docs and
  screenshots
- if Resend API is used, the API key is stored only as a protected value in the
  database and is never returned to the frontend
- before deploying Resend API support, build the netcup release artifact and
  verify the generated idempotent SQL contains
  `20260710120000_AddResendEmailDeliverySettings` plus
  `EmailDeliveryResendApiKeyProtected`, `EmailDeliveryResendFromEmail` and
  `EmailDeliveryReplyToEmail`
- if owner-managed Gmail SMTP is used, the Google App Password is stored only as
  a protected value in the database and is never returned to the frontend
- persist ASP.NET Core Data Protection keys in production so protected admin
  SMTP secrets remain decryptable across deployments/restarts
- configure `Host`, `Port`, `Username`, `Password`, `FromEmail`, `FromName` and
  `UseSsl` when using developer-managed SMTP
- if Gmail is used, enable Google 2-Step Verification and use a Google App
  Password rather than the normal Gmail password
- verify **Admin > Settings > Email notifications enabled** and the owner/public
  email address
- test real delivery through **Admin > Settings > Send test email**
- test real delivery from the public Contact form and Order form
- keep owner notifications separate from customer confirmation emails and test both toggles independently
- monitor Admin Email Log for `Retrying`/`Failed` rows and alert on exhausted outbox jobs
- before production, confirm Admin Dashboard **DNS email records** is green for
  `resend._domainkey.oksanalogosha.com`, `send.oksanalogosha.com` SPF/MX and
  `_dmarc.oksanalogosha.com`
- before production, configure deliverability operations: bounce/rejection
  monitoring and credential rotation

Example local Gmail SMTP setup uses user-secrets only:

```powershell
dotnet user-secrets set "Notifications:Email:Provider" "Smtp" --project backend/src/BespokeStudio.Api
dotnet user-secrets set "Notifications:Email:Smtp:Host" "smtp.gmail.com" --project backend/src/BespokeStudio.Api
dotnet user-secrets set "Notifications:Email:Smtp:Port" "587" --project backend/src/BespokeStudio.Api
dotnet user-secrets set "Notifications:Email:Smtp:Username" "your-gmail-address@gmail.com" --project backend/src/BespokeStudio.Api
dotnet user-secrets set "Notifications:Email:Smtp:Password" "<google-app-password-from-secret-store>" --project backend/src/BespokeStudio.Api
dotnet user-secrets set "Notifications:Email:Smtp:FromEmail" "your-gmail-address@gmail.com" --project backend/src/BespokeStudio.Api
dotnet user-secrets set "Notifications:Email:Smtp:FromName" "Bespoke Sewing Studio" --project backend/src/BespokeStudio.Api
dotnet user-secrets set "Notifications:Email:Smtp:UseSsl" "true" --project backend/src/BespokeStudio.Api
```

Production equivalents for developer-managed SMTP must use environment
variables such as `Notifications__Email__Smtp__Password` or a managed secret
store. Owner-managed Gmail SMTP can instead be configured in **Admin > Settings
> Email delivery** after deployment.

## Language

The site and admin panel are English-only. There is no public language switcher, no `defaultLanguage` setting and no planned multilingual CMS. New CMS records, seed data and typed fallback data should be authored in English.

## Portfolio and gallery

Sign in at `http://127.0.0.1:5173/admin`, then select **Portfolio**. The Items
tab creates and edits gallery entries, uploads JPG/PNG/WebP images, controls the
category, alt text, display order, Featured state and public visibility. The
Categories tab manages category names, descriptions, ordering and visibility.

The Portfolio page and Home preview load active entries from PostgreSQL through
the backend. Existing optimized frontend images remain a typed fallback when
the API is unavailable. In development, newly uploaded portfolio images are
stored under `backend/storage/uploads/portfolio-images`; `backend/storage/` is
ignored by Git. Production object storage and generated thumbnails are future
work.

## Website content

Sign in to Admin and select **Content** to filter sections by page, edit titles,
subtitles, body text, CTA labels/URLs, ordering and visibility, or upload a
JPG/PNG/WebP page image. Home, About, Services, Portfolio, Order, Contact and
Privacy use backend-first content from `GET /api/content/pages/{pageKey}`.
Typed frontend defaults remain available when the backend cannot be reached. These defaults are English-only and should not introduce language-specific branches.
Content images are stored locally under `backend/storage/uploads/content-images`
in development. The existing logo remains a bundled frontend fallback; logo
upload is not part of this module.

## Repeatable content

Sign in to Admin and select **Repeatable Content** to manage repeated public
content blocks that are not single page sections. The current groups are:

- `process-steps` for the Home process section
- `studio-values` for values shown on Home/About
- `testimonials` for public testimonials
- `privacy-sections` for detailed Privacy page subsections

The public site loads these records from `GET /api/repeatable-content` and can
also read an individual group through `GET /api/repeatable-content/groups/{groupKey}`.
The Admin panel can add, edit, hide/show and archive items through protected
`/api/admin/repeatable-content` endpoints. Typed frontend defaults in
`src/data/siteData.ts` remain available only as an offline fallback.

## Privacy and terms pages

The public site includes a Privacy Policy at `/privacy` and Terms & Service
Information at `/terms`. Footer links point to both pages. The Order and Contact
forms link to these notices next to the user consent text, and the Order form
explains that accepted attachments are stored so the studio can review and
respond to the enquiry.

The Privacy page uses backend-first page content and repeatable privacy sections
where available, plus typed fallback notices for contact forms, order requests,
uploaded files and admin/audit records. The Terms page is a static frontend page
with plain-English service information for enquiries, consultations, guide prices,
client materials, uploads, timings, changes and cancellations. The text is
intended as a practical website notice and should be reviewed by the business
owner before public launch.

## Brand and SEO settings

Sign in to Admin and select **Brand / SEO** to upload a JPG, PNG or WebP logo,
favicon or default Open Graph image and edit the brand name, logo alt text,
header CTA, default title/description and navigation labels/visibility. The
public header, footer and default Home metadata load these settings from the
backend. Route-specific SEO metadata, canonical links, Open Graph/Twitter card
tags and Home `LocalBusiness`/`ProfessionalService` JSON-LD are managed by the
frontend SEO manager. If the backend is unavailable, the bundled logo and typed
defaults keep the public site usable. SVG upload is intentionally disabled.

The static `public/robots.txt` blocks `/admin` and `/admin/login` and points
`Sitemap:` at `https://oksanalogosha.com/sitemap.xml`. The API serves a dynamic
`/sitemap.xml` (static pages plus published IN STOCK slugs) using
`PublicSiteUrl` / `PUBLIC_SITE_URL` or the canonical origin
`https://oksanalogosha.com`. Admin, draft and archived routes must never appear
in the sitemap; admin pages use `noindex, nofollow` via the frontend SEO manager.

Commands:

```powershell
dotnet restore backend/BespokeStudio.sln
```

```powershell
dotnet build backend/BespokeStudio.sln
```

```powershell
dotnet run --project backend/src/BespokeStudio.Api/BespokeStudio.Api.csproj
```

If your environment cannot read the user-level `NuGet.Config`, use the repo-local fallback:

```powershell
dotnet restore backend/BespokeStudio.sln --configfile backend/NuGet.Config
```

## Continuous integration

The minimal GitHub Actions workflow is defined in `.github/workflows/ci.yml`.
It runs for every pull request and every push to `main`, using Node.js 24.x and
the .NET 10.0.x SDK to execute:

- `npm run typecheck`
- `npm run build`
- `dotnet build backend/BespokeStudio.sln --configuration Release --no-restore`
- `dotnet test backend/BespokeStudio.sln --configuration Release --no-build`

Frontend dependencies are installed with `npm ci`, and the npm download cache
uses the committed `package-lock.json`. The workflow does not deploy the site,
apply EF Core migrations, start PostgreSQL or require production secrets.

## Backend tests

The backend test foundation uses xUnit and currently covers pure order-request
validation, pagination normalization and email-outbox retry timing without
PostgreSQL, SMTP, ClamAV or production secrets. Run it before
commits that change backend code and before a production release:

```powershell
dotnet test backend\BespokeStudio.sln
npm.cmd run typecheck
npm.cmd run build
dotnet build backend\BespokeStudio.sln
```

Task 49.1 adds the test project and initial unit tests without changing production
runtime behaviour. Optional PostgreSQL integration tests are documented in
[`backend/README.md`](backend/README.md#postgresql-integration-tests) (opt-in via
env vars; default `dotnet test` skips them). Full auth/2FA/order/contact/upload
API integration coverage remains future work.

## Routing

The frontend uses `React Router` with lazy-loaded page routes.

Production hosting must support SPA fallback to `index.html` for client-side routes. See `DEPLOYMENT_NOTES_RU.md` for deployment notes and server-side examples.

## Commands (Windows PowerShell)

Install dependencies:

```powershell
npm.cmd install
```

Run strict TypeScript checks:

```powershell
npm.cmd run typecheck
```

Run frontend unit tests (Vitest):

```powershell
npm.cmd test
```

Create a production build:

```powershell
npm.cmd run build
```

Start the development server:

```powershell
npm.cmd run dev
```

Start the development server on the explicit loopback host:

```powershell
npm.cmd run dev -- --host 127.0.0.1
```

Vite prints the local URL in the terminal (normally `http://localhost:5173`).

## Output

The generated production bundle is written to `dist/` and is not committed.

## Images

Original exported source images are kept in `src/imports/`.

Optimised responsive derivatives live in `src/assets/images/optimized/`.
Current image usage is centralised in `src/data/imageAssets.ts`.

When updating large visual assets:

- keep the original source file in `src/imports/`
- generate new optimised derivatives instead of overwriting the original
- prefer responsive hero variants and smaller card-sized gallery assets
  
