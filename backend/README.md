# Bespoke Sewing Studio backend

ASP.NET Core Web API for the Bespoke Sewing Studio project. The backend powers the public CMS-driven website, contact messages, admin modules, uploads, authentication, PostgreSQL persistence and email notification foundation.

Current status:

- EF Core persistence is configured for PostgreSQL and migrations are applied explicitly in local development
- Orders/enquiries API persists clients, orders, selected services, statuses, internal notes and attachment links
- Contact Messages API persists public Contact form messages and admin workflow statuses
- ASP.NET Core Identity stores administrator accounts and roles
- JWT Bearer authentication protects admin endpoints
- uploads use local development storage plus PostgreSQL metadata; order attachments stay private
- Services & Prices CMS manages dynamic services and text-based price options
- Portfolio/Gallery CMS manages categories, work items, images, publication state and ordering
- Website Content CMS manages page sections, copy, CTA data and page images
- Repeatable Content CMS manages process steps, studio values, testimonials and privacy subsections
- Admin Contact Messages module loads paged message lists, applies search/status filters server-side and updates workflow state
- a protected SignalR admin-notifications hub broadcasts Order and Contact Message changes to open admin sessions
- Site Settings and Brand/Logo/SEO settings provide public contact, navigation, logo, CTA and metadata configuration
- public Order and Contact submissions use rate limits plus lightweight honeypot/timing anti-spam checks
- email notification foundation supports owner notifications for Orders and Contact Messages through Logging and SMTP providers; WhatsApp/SMS channels are intentionally not implemented
- email notifications use a PostgreSQL outbox and background worker with bounded retry/backoff; Email Log reflects queued, retrying, sent and failed states
- the product is English-only; multilingual CMS, language fields and EN/UA switching are not part of the current scope

## Production request pipeline and health checks

The backend launch smoke tests (health endpoints, admin auth/2FA/session, orders,
contact messages, uploads and email) are part of the final go-live runbook in
[`../PRODUCTION_GO_LIVE_RU.md`](../PRODUCTION_GO_LIVE_RU.md); backend-specific
details stay in this README and the linked production runbooks.

Release readiness review: [`../RELEASE_READINESS_REVIEW_RU.md`](../RELEASE_READINESS_REVIEW_RU.md)
collects the final Go/No-Go summary. Backend GO still requires validation commands,
production PostgreSQL, Data Protection keys, uploads/scanner, SMTP, reverse proxy
and backup/restore rehearsal on the live server.

Server execution: [`../PRODUCTION_DEPLOYMENT_PLAN_RU.md`](../PRODUCTION_DEPLOYMENT_PLAN_RU.md)
describes build artifacts, env/secrets placeholders, DB migrations, backup,
deployment sequence, reverse proxy, smoke tests and rollback. Backend deployment
requires all of the above plus post-deploy health and Email Log checks.
The concrete supported production target is netcup with Cloudflare Full (strict),
Caddy and Docker Compose; see [`../PRODUCTION_DEPLOYMENT_RU.md`](../PRODUCTION_DEPLOYMENT_RU.md)
and [`../DEPLOY_NETCUP_RU.md`](../DEPLOY_NETCUP_RU.md). The production compose
uses the real connection string key `ConnectionStrings__BespokeStudioDb` and
runs the published `BespokeStudio.Api.dll`.

Unhandled request exceptions are converted centrally to `application/problem+json`
responses through ASP.NET Core Problem Details. Production responses do not expose
stack traces or exception details; the response includes a `traceId` for correlation.
Existing endpoint-level validation, authentication, authorization, upload validation
and rate-limit responses keep their current status codes and payload semantics.

Health endpoints are anonymous and intentionally expose only an overall status:

- `GET /health` and `GET /health/live` are liveness checks for the API process;
- `GET /healthz` is a compatibility alias for `GET /health/live`;
- `GET /health/ready` is readiness and calls EF Core `CanConnectAsync` against the
  configured `ConnectionStrings:BespokeStudioDb` PostgreSQL database;
- `GET /readyz` is a compatibility alias for `GET /health/ready`;
- `GET /api/health` remains as a compatibility liveness endpoint and does not
  query PostgreSQL.

Readiness returns HTTP `503` when PostgreSQL is unavailable. Health responses do not
contain connection strings, exception messages, stack traces or other secrets.
Reverse proxies and container orchestrators should use `/health/live` or `/healthz`
for liveness and `/health/ready` or `/readyz` for readiness.

`GET /api/version` is also database-independent. It returns the typed fields
`application`, `version`, `environment`, `framework`, `commit`, `buildTime` and
`startedAt`. `BUILD_VERSION`, `GIT_COMMIT` and an ISO-8601 `BUILD_TIME` can be set
by CI or a release pipeline. Missing build version falls back to the API assembly
informational/assembly version; missing commit and build time are returned as null.
The endpoint never reads or returns connection strings, SMTP settings, credentials,
internal paths or exception details.

## Request correlation and structured logging

Every request flows through a correlation-id middleware registered right after
forwarded headers and before security headers, exception handling, CORS,
authentication, output cache, authorization and rate limiting.

- The header name is `X-Correlation-ID`.
- If the client or a reverse proxy sends a valid `X-Correlation-ID`, the backend
  reuses its trimmed value.
- A value is considered valid only when, after trimming, it is non-empty, at most
  120 characters long, contains no control characters and uses only
  `A-Z`, `a-z`, `0-9`, `.`, `-`, `_` and `:`.
- If the header is missing, empty, unsafe or too long, the backend generates a new
  id with `Guid.NewGuid().ToString("N")`.
- The resolved id is written back on every HTTP response `X-Correlation-ID` header
  (including error/Problem Details and `429` responses) and stored in
  `HttpContext.Items` for downstream use.

The middleware also opens an `ILogger` scope for the request with the safe
structured fields `CorrelationId`, `TraceIdentifier`, `RequestMethod` and
`RequestPath`, so log records for one request can be grouped during
troubleshooting. Built-in logging additionally tracks `TraceId`, `SpanId` and
`ParentId` activity ids. The middleware never logs request bodies, cookies,
tokens, Authorization headers, uploaded files, passwords or other secrets, and no
API JSON response contract is changed.

## Public content output caching

Selected anonymous read-only JSON endpoints use the built-in ASP.NET Core output
cache with the named `PublicContent` policy:

- `GET /api/services`;
- `GET /api/portfolio` and `GET /api/portfolio/categories`;
- `GET /api/content/pages/{pageKey}`;
- `GET /api/repeatable-content` and `GET /api/repeatable-content/groups/{groupKey}`;
- `GET /api/site-settings/public`;
- `GET /api/brand-settings/public`.

The policy has a 60-second in-memory TTL. Full path, route values and query string
remain part of the cache key. Only successful GET/HEAD responses are eligible;
authenticated requests, requests with an Authorization header, responses that set
cookies and non-200 responses follow the framework's default no-cache rules.
This task does not force browser/CDN `Cache-Control` headers; an `Age` header on a
repeat request can be used to verify a server-side cache hit.

No cache policy is attached to `/api/admin/*`, `/api/auth/*`, Order or Contact
submissions, uploads/downloads, public or admin image/file responses, Email/Audit
logs, health checks or `/api/version`. Public JSON responses are tagged by content
area (`public-services`, `public-portfolio`, `public-page-content`,
`public-repeatable-content`, `public-site-settings`, `public-brand-settings`, plus
the shared `public-content` tag). After a successful admin CMS or settings mutation,
matching tags are evicted through `IOutputCacheStore.EvictByTagAsync`, so public
pages and JSON endpoints reflect the change immediately without waiting for the
60-second TTL. Browser/CDN `Cache-Control` headers are still not forced; production
reverse proxy/CDN cache headers require a separate reviewed strategy and must never
cache private/admin responses. Cache hit/miss metrics remain a future improvement.

Forwarded headers are processed first, followed by security response headers, HSTS
(non-Development), exception handling, HTTPS redirection, CORS, authentication and
rate limiting. The API accepts `X-Forwarded-For`,
`X-Forwarded-Proto` and `X-Forwarded-Host` with `ForwardLimit=1` by default.
For the supported netcup topology, Kestrel is reachable only from Caddy/Docker
and from `127.0.0.1:5030`; it is not exposed directly to the internet. If a
future deployment exposes Kestrel outside that boundary, configure exact
trusted proxies/networks and do not trust arbitrary internet clients:
The production compose sets `ForwardedHeaders__KnownNetworks__0=172.16.0.0/12`
for the Docker bridge/private network used by the Caddy upstream. During cutover,
verify the actual external `web` Docker network subnet and tighten this value if
the server uses a narrower fixed subnet.

```powershell
$env:ForwardedHeaders__ForwardLimit = "1"
$env:ForwardedHeaders__KnownProxies__0 = "10.0.0.10"
$env:ForwardedHeaders__KnownNetworks__0 = "10.0.1.0/24"
```

For Cloudflare or another multi-hop topology, configure only the proxy/network that
connects directly to Kestrel and set `ForwardLimit` to the actual trusted hop count.
Verify the resulting request scheme and client address after deployment. The
reverse proxy must forward `X-Forwarded-For`, `X-Forwarded-Proto` and
`X-Forwarded-Host`, enable WebSocket upgrade for the SignalR admin realtime hub
(`/hubs/admin-notifications`), and rely on HSTS and HTTPS redirection which run
outside Development. The full production reverse proxy / HTTPS runbook is in
[`../REVERSE_PROXY_HTTPS_PRODUCTION_RU.md`](../REVERSE_PROXY_HTTPS_PRODUCTION_RU.md).

Data Protection uses the stable application discriminator
`BespokeSewingStudio`. Development can use the framework's default local key store.
Production startup fails unless `DataProtection:KeysPath` is configured, preventing
accidental use of ephemeral deployment storage. Use an absolute persistent path
outside the repository and grant access only to the API process identity:

```powershell
$env:DataProtection__ApplicationName = "BespokeSewingStudio"
$env:DataProtection__KeysPath = "D:\BespokeStudioSecrets\DataProtectionKeys"
```

Linux/netcup example: `DataProtection__KeysPath=/appdata/keys` inside the app
container, mounted from `/opt/apps/projects/bespoke-studio/data/keys`.
Back up this directory with the database and uploads. Never commit key files,
production connection strings, JWT signing keys or SMTP credentials. Environment
variables are supported by configuration; a managed secret store remains preferable
for production secrets.

Production startup intentionally fails without `DataProtection:KeysPath` to prevent
ephemeral key storage, and `DataProtection:ApplicationName` must stay stable
(`BespokeSewingStudio`) so previously protected values remain decryptable. The keys
folder must live outside the repository and publish/release folder and be included
in a protected backup. The full production Data Protection runbook (persistent keys
path, permissions, backup/restore/redeploy smoke tests and troubleshooting) is in
[`../DATA_PROTECTION_PRODUCTION_RU.md`](../DATA_PROTECTION_PRODUCTION_RU.md).

## Language and content scope

Bespoke Sewing Studio is maintained as an English-only product. Backend models,
seed/default content and CMS contracts should not add language or locale columns
without a new product decision. Public/admin labels and fallback/default content
should remain English-only. Multilingual CMS is not planned for the current scope.

## Portfolio/Gallery API

Public endpoints do not require JWT:

- `GET /api/portfolio` returns active items in active categories
- `GET /api/portfolio/categories` returns active categories
- `GET /api/portfolio/images/{id}` streams an image only when it is linked to an active, non-archived portfolio item in an active category

Admin JWT with the `Admin` role is required for:

- `GET|POST /api/admin/portfolio/items`
- `GET|PATCH|DELETE /api/admin/portfolio/items/{id}`
- `GET|POST /api/admin/portfolio/categories`
- `PATCH|DELETE /api/admin/portfolio/categories/{id}`
- `POST /api/admin/portfolio/uploads`
- `GET /api/admin/portfolio/images/{id}` for authenticated previews of inactive or archived items

Portfolio uploads accept one JPG, PNG or WebP file and use the configured
`UploadStorage:MaxFileSizeBytes` limit. Files are stored under
`backend/storage/uploads/portfolio-images` in local development; PostgreSQL
stores metadata and references, not image bytes. Archived items retain their
physical files for later cleanup or restoration.

Security boundary: `/api/portfolio/images/{id}` never exposes arbitrary upload
metadata or order attachments. Order attachments continue to be downloaded
only through the Admin-protected `/api/uploads/{uploadedFileId}` endpoint.

## Website Content API

- `GET /api/content/pages/{pageKey}` returns active sections without JWT.
- `GET /api/content/images/{id}` streams only SiteAsset images referenced by active content.
- `GET|POST /api/admin/content`, `GET|PATCH|DELETE /api/admin/content/{id}` require Admin JWT.
- `POST /api/admin/content/uploads` accepts one JPG, PNG or WebP up to the configured limit.
- `GET /api/admin/content/images/{id}` provides authenticated previews for inactive/archived content.

`PageKey` and `SectionKey` use lowercase safe keys and form a unique pair for
non-archived rows. Content images use `UploadedFileMetadata` with `SiteAsset`;
PostgreSQL never stores image bytes. Public content image routing cannot expose
PortfolioImage or OrderAttachment uploads.

## Repeatable Content API

Repeatable Content stores ordered CMS records for repeated public sections that
are not single page content rows. It currently backs process steps, studio
values, testimonials and privacy subsections.

Public endpoints:

- `GET /api/repeatable-content` returns all active non-archived groups.
- `GET /api/repeatable-content/groups/{groupKey}` returns one active group.

Admin JWT endpoints:

- `GET /api/admin/repeatable-content`
- `GET /api/admin/repeatable-content/{id}`
- `POST /api/admin/repeatable-content`
- `PATCH /api/admin/repeatable-content/{id}`
- `DELETE /api/admin/repeatable-content/{id}`

`GroupKey` and `ItemKey` use lowercase safe keys and are unique for
non-archived rows. Admin delete archives an item instead of physically deleting
it, so historical content can be preserved while hidden from public responses.
The frontend Admin panel exposes **Repeatable Content** for adding, editing,
hiding/showing and archiving items. Public pages keep typed fallback data if the
API is unavailable.

## Brand / Logo / SEO API

Public endpoints:

- `GET /api/brand-settings/public`
- `GET /api/brand/images/{id}`

Admin JWT endpoints:

- `GET|PATCH /api/admin/brand-settings`
- `POST /api/admin/brand/uploads`
- `GET /api/admin/brand/images/{id}` for previews before a setting is saved

Brand uploads accept one JPG, PNG or WebP file up to the configured
`UploadStorage:MaxFileSizeBytes`; SVG and non-image files are rejected. Files
use the dedicated `BrandAsset` purpose and local `brand-images` storage path.
The public image endpoint streams a file only when its ID is currently used as
the logo, favicon or default Open Graph image. It cannot expose order
attachments, portfolio images, content images or an unlinked brand upload.

## Domain model draft

`BespokeStudio.Domain` contains persistence-independent entities for clients,
orders and notes/attachments, contact messages, portfolio items and categories,
service offerings, and uploaded-file metadata. Domain enums describe order
status, contact message status, service type, portfolio publication status,
upload purpose and upload scan status.

Orders and Contact Messages store human-readable request references separately
from their internal GUID primary keys. Public/customer-facing references use
`BSS-ORD-YYYY-000001` for orders and `BSS-CON-YYYY-000001` for contact messages. Admin-only delete endpoints are intended for test, spam or obsolete records. Order/contact deletion and the corresponding audit entry are committed in one database transaction. Linked order-file metadata and one `UploadFileDeletionJobs` row per attachment are committed together; a hosted worker removes physical files later. SignalR deletion events remain best-effort after commit.

The domain does not reference EF Core, database attributes, `DbContext`, or a
storage provider. Email, phone, and money value objects remain a future design
decision after validation and currency rules are agreed.

## Application contracts

`BespokeStudio.Application` contains request/response records and service
interfaces. These contracts are separate from domain entities so future HTTP
payloads do not expose persistence models directly.

Infrastructure implementations are registered for Orders, Contact Messages,
Services, Portfolio, Page Content, Repeatable Content, Site/Brand Settings,
uploads and notification delivery.

## Implemented modules

- Orders and client records, including Admin-only enquiry deletion for test/obsolete requests
- Contact messages, including Admin-only message deletion
- Order attachments and upload cleanup
- Administrator authentication with Identity/JWT
- Site Settings
- Brand / Logo / SEO settings
- Services and Prices CMS
- Portfolio and Gallery CMS
- Website Content CMS
- Repeatable Content CMS
- Email notification foundation
- Email delivery log
- Admin audit log
- Admin account password change

The protected Audit Log and Email Delivery Log list endpoints use typed
server-side pagination. Both accept 1-based `page` and `pageSize` query values,
apply filters before counting, sort newest first and return `items`, `page`,
`pageSize`, `totalItems` and `totalPages`. The default page size is 25; supported
sizes are 10, 25, 50 and 100, with 100 as the maximum. Invalid values are safely
normalised. No database schema change is required.

The protected Orders and Contact Messages list endpoints use the same pagination
shape and limits. Contact Messages search covers reference, sender name, email,
phone, subject, message and matching workflow status values. Its status filter,
counting, newest-first ordering and `Skip`/`Take` are all applied server-side.

## PostgreSQL and EF Core

The EF Core context is `BespokeStudioDbContext` in
`BespokeStudio.Infrastructure/Persistence`. Entity mappings are defined with
Fluent API configurations in Infrastructure; Domain remains free of EF Core
attributes and references.

The development connection string is stored in
`BespokeStudio.Api/appsettings.Development.json` under
`ConnectionStrings:BespokeStudioDb`. Its credentials are local placeholders
matching the Docker Compose service. Use environment variables or a secret
store for non-development environments.

The current compose mapping exposes PostgreSQL on host port `5433` and maps it
to container port `5432`.

Start the local PostgreSQL 16 container from the repository root:

```powershell
docker compose -f docker-compose.postgres.yml config
```

```powershell
docker compose -f docker-compose.postgres.yml up -d
```

This command requires Docker with the Compose plugin. The compose file was not
started in the current development environment because the Docker CLI was not
installed.

Stop it without deleting the named data volume:

```powershell
docker compose -f docker-compose.postgres.yml down
```

Check that the PostgreSQL container is running and healthy:

```powershell
docker compose -f docker-compose.postgres.yml ps
```

Create a new migration after changing the persistence model:

```powershell
dotnet ef migrations add MigrationName --project backend/src/BespokeStudio.Infrastructure --startup-project backend/src/BespokeStudio.Api --output-dir Persistence/Migrations
```

Apply migrations to the configured development database:

```powershell
dotnet ef database update --project backend/src/BespokeStudio.Infrastructure --startup-project backend/src/BespokeStudio.Api
```



Deleting a linked order attachment removes the `OrderAttachments` link, deletes
the associated `UploadedFiles` metadata row, updates the order timestamp and
adds a safe relative-path deletion job in the same transaction. The endpoint
records an `order_attachment.deleted` audit entry and
returns the updated `OrderResponse` so the admin drawer can refresh without a
full page reload. API success never depends on immediate filesystem deletion.

Full order deletion differs from individual attachment deletion: it collects all
linked storage keys first, removes the order, notes, attachment links, upload
metadata, deletion jobs and `order.deleted` audit entry with one `SaveChanges`
inside one transaction. Contact-message
deletion similarly commits the message removal and `contact_message.deleted`
audit entry atomically. SignalR notifications are warning-logged best-effort operations.

The repository currently contains migrations for the initial schema, phone-only
orders, Identity/JWT, Site Settings, contact normalisation, removal of WhatsApp
notification fields, dynamic services/prices, Portfolio/Gallery CMS, Website
Content CMS, Brand/SEO settings, Repeatable Content CMS, Contact Messages,
customer confirmation email templates, human-readable request references, the admin audit log and the email delivery log. They have been
applied to the local development database during the corresponding tasks. Installing the matching
CLI tool, if it is missing locally:

```powershell
dotnet tool install --global dotnet-ef --version 10.0.9
```

If `dotnet-ef` is already installed, update it to the matching version:

```powershell
dotnet tool update --global dotnet-ef --version 10.0.9
```

Confirm the installed tool before applying migrations:

```powershell
dotnet ef --version
```

After `database update`, verify the applied migration directly in PostgreSQL:

```powershell
docker compose -f docker-compose.postgres.yml exec postgres psql -U bespoke_user -d bespoke_studio_dev -c 'SELECT "MigrationId" FROM "__EFMigrationsHistory";'
```

Registering the DbContext does not open a database connection during API
startup. The existing system endpoints and Swagger can therefore run while the
development database is offline. No database health check or automatic
migration is enabled yet.

## Backup and restore operations

Full backup/restore instructions live in `../BACKUP_RESTORE_RU.md`, the final
production backup/restore runbook (full backup inventory, restore rehearsal,
post-restore smoke test and rollback plan). A database-only backup is not enough:
it omits physical uploads and, without the ASP.NET Core Data Protection keys, the
protected owner-managed Gmail App Password becomes undecryptable and the 2FA
challenge cookie stops working. The short version for this backend is:

- create a PostgreSQL dump with `pg_dump --format=custom`;
- back up `backend/storage` separately because database dumps store upload
  metadata only, not physical files;
- keep backups outside the Git repository;
- never commit `.dump`, `.sql`, storage archives, `.env`, production appsettings
  files, SMTP credentials or Google App Passwords;
- preserve ASP.NET Core Data Protection keys in production when using protected
  owner-managed Gmail SMTP settings;
- verify every important dump with `pg_restore --list`;
- run a restore rehearsal on a test/staging environment before relying on a backup
  procedure, and back up the Data Protection keys together with the database and
  uploads.

Draft/reference production backup automation (operational helper only; backend
runtime behaviour unchanged):

- [`../scripts/production/Backup-Production.ps1`](../scripts/production/Backup-Production.ps1)
- [`../scripts/production/README_RU.md`](../scripts/production/README_RU.md)

Local Docker Compose backup example from the repository root:

```powershell
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$backupRoot = "C:\Backups\BespokeStudio\$stamp"
New-Item -ItemType Directory -Force $backupRoot | Out-Null

docker compose -f docker-compose.postgres.yml exec -T postgres pg_dump -U bespoke_user -d bespoke_studio_dev --format=custom --file=/tmp/bespoke_studio_dev.dump
docker compose -f docker-compose.postgres.yml cp postgres:/tmp/bespoke_studio_dev.dump "$backupRoot\bespoke_studio_dev.dump"
docker compose -f docker-compose.postgres.yml exec -T postgres rm -f /tmp/bespoke_studio_dev.dump

if (Test-Path .\backend\storage) {
    Compress-Archive -Path .\backend\storage -DestinationPath "$backupRoot\backend-storage.zip" -Force
}
```

Local restore example is also documented in `../BACKUP_RESTORE_RU.md`. Stop the
API before restoring, because restore recreates the development database and can
replace local storage.

## Admin Audit Log API

Admin JWT with the `Admin` role is required for `GET /api/admin/audit-log`.
The endpoint returns newest entries first and accepts optional query parameters:

- `take` from 1 to 200, default 100;
- `search` across actor, action, entity, reference/label and summary;
- `action`;
- `entityType`;
- `actorEmail`.

The audit scope records authentication login success/failure, logout, failed/reused
refresh, session revocation, administrator user management, own-account password
changes, order status/note changes, contact message status changes, Site Settings updates, Email Delivery
updates and Brand / SEO updates. The audit log intentionally stores no
passwords, access/refresh tokens, token hashes, cookies, Gmail App Passwords or raw SMTP secrets.

## Admin Users API

Admin JWT with the `Admin` role is required for all endpoints under
`/api/admin/users`:

- `GET /api/admin/users` lists admin users and safety flags.
- `POST /api/admin/users` creates an Admin user with an email and temporary password.
- `PATCH /api/admin/users/{id}/status` enables or disables an admin user.
- `POST /api/admin/users/{id}/reset-password` sets a new temporary password.
- `DELETE /api/admin/users/{id}` deletes an admin user.

The implementation uses ASP.NET Core Identity users and the existing `Admin`
role. It does not add a new migration: disabling a user is stored through
Identity lockout fields. The API refuses to disable/delete the current session
user and refuses to disable/delete the last active admin account. Passwords are
never returned by the API. Disabling an administrator revokes all active refresh
sessions with reason `user_disabled`; subsequent refresh and access-token validation fail.

## Orders API

The Orders API is available under `/api/orders`. Each order has an internal GUID
`id` and a customer-facing `referenceNumber` such as `BSS-ORD-2026-000001`:

- `POST /api/orders` anonymously creates an enquiry and returns `201 Created`
- `GET /api/orders?page=1&pageSize=25` returns a typed page of enquiries (Admin JWT required)
- `GET /api/orders/{id}` returns enquiry details (Admin JWT required)
- `PATCH /api/orders/{id}/status` updates the workflow status (Admin JWT required)
- `POST /api/orders/{id}/notes` adds an internal note (Admin JWT required)

The Admin list accepts `page`, `pageSize`, `search` and `status`. Search covers
the request reference, client name, email, phone, service snapshot and description.
Filters are applied server-side before `totalItems` is counted; results are ordered
by newest first and returned as `items`, `page`, `pageSize`, `totalItems` and
`totalPages`. Page size defaults to 25, supports 10/25/50/100 and is capped at 100.
The separate order details endpoint and anonymous `POST /api/orders` contract are
unchanged.

Example request:

```json
{
  "fullName": "Test Client",
  "email": "test@example.com",
  "phone": "074 6734 7194",
  "serviceType": "Dressmaking",
  "description": "I would like to discuss a custom dress order.",
  "preferredDate": null,
  "consent": true,
  "attachmentIds": null,
  "websiteUrl": null,
  "formLoadedAt": "2026-06-29T19:15:00Z"
}
```

`websiteUrl` is a hidden honeypot field and should stay `null`/empty.
`formLoadedAt` is the UTC timestamp from when the public form was opened;
submissions that are missing it, arrive too quickly, are stale or fill the
honeypot are rejected before persistence. At least one of `email` or `phone` is required. Client matching first checks a
normalised email and then an exact trimmed phone. A new client is created only
when neither value matches an existing client.

Start the API and use `/swagger` for interactive testing, or open
`BespokeStudio.Api/BespokeStudio.Api.http` and run its prepared requests. Copy the internal `id` returned by `POST /api/orders` into the `OrderId` variable
before running the detail, status, and note examples. Use `referenceNumber` in
customer-facing messages and admin communication.

New-order email notifications use the logging provider by default and can use SMTP.
The public frontend Order form calls anonymous `POST /api/orders`.
The public frontend Contact form calls anonymous `POST /api/contact-messages`.
The admin frontend uses login, current-user, own-password change, Orders list/detail/status/notes,
Contact Messages list/detail/status, Services, Portfolio, Content, Repeatable
Content, Site Settings and Brand/SEO endpoints. Open admin sessions can also
connect to `/hubs/admin-notifications` through SignalR/WebSocket with the Admin
JWT token; the hub requires the same Admin policy and broadcasts lightweight
change events only, never customer data payloads or secrets.



## Admin realtime updates

`/hubs/admin-notifications` is a protected SignalR hub for signed-in administrators.
The JWT token may be supplied as an `access_token` query parameter for WebSocket
connections; the same `AdminOnly` policy is enforced. Order creation/status/note
changes, Contact Message creation/status changes and Email Log writes broadcast
lightweight `AdminDataChanged` events containing the entity type, internal ID,
optional human-readable reference number and event timestamp. The frontend uses
these events to reload admin lists, dashboard counters, sidebar badges and the
Email Log panel. Manual Refresh buttons remain available as a fallback for
disconnected clients or proxy misconfiguration.

## Contact Messages API

Contact Messages are submitted by the public Contact page and persisted in
PostgreSQL. They are separate from Orders because they may be general questions
rather than service enquiries.

Public endpoint:

- `POST /api/contact-messages` anonymously creates a contact message and returns `201 Created` with a customer-facing `referenceNumber` such as `BSS-CON-2026-000001`

Admin JWT endpoints:

- `GET /api/admin/contact-messages?page=1&pageSize=25&search=sample&status=New` returns a typed page of newest messages after applying search and status filters server-side; supported page sizes are 10, 25, 50 and 100, with 25 as the default and 100 as the maximum
- `GET /api/admin/contact-messages/{id}` returns one message
- `PATCH /api/admin/contact-messages/{id}/status` updates the workflow status

Supported statuses are `New`, `Read`, `Replied` and `Archived`. The public
request requires name, email, message, `consent=true` and the hidden anti-spam
fields `websiteUrl` and `formLoadedAt`; phone and subject are optional.
Validation failures return `400 ValidationProblem` with JSON property
names matching the frontend form. Public contact message creation uses the
`PublicContactPolicy` fixed-window rate limit configured through
`RateLimiting:PublicContactPermitLimit` and `RateLimiting:WindowMinutes`. The
same lightweight honeypot/timing validation is used by public Order creation.

After a message is stored, `INotificationService` sends an owner notification
through the same email foundation used for Orders. `EmailNotificationsEnabled`
and the Site Settings email control delivery. The logging provider writes the
message to the backend log in development; SMTP can be enabled through
user-secrets or environment variables. Notification errors are logged but do not
cancel the successful contact message response.

## Services and Prices API

Migration `AddDynamicServicesAndPrices` expands `ServiceOffering`, adds
`ServicePriceOption`, and adds nullable `ServiceOfferingId` plus a required
`ServiceNameSnapshot` to orders. Existing order snapshots are backfilled from
the legacy enum. Default Tailoring, Dressmaking, Alterations and Memory Bears
services are inserted only when the services table is empty.

Public endpoint:

- `GET /api/services` — active, non-archived services with active price options

Admin JWT endpoints:

- `GET /api/admin/services`
- `GET /api/admin/services/{id}`
- `POST /api/admin/services`
- `PATCH /api/admin/services/{id}`
- `DELETE /api/admin/services/{id}`

Slugs are lowercase kebab-case and unique among non-archived services. Price
values are stored as `PriceText`, allowing values such as `from £45`, `+£15`,
or `Quote on request`. Deleting an unused service performs a hard delete. If
orders reference it, the service is archived and deactivated instead.

New orders accept `serviceOfferingId` or `serviceSlug`; the legacy `serviceType`
enum remains as a compatibility fallback. Each new order stores the selected
service name snapshot, so admin views and email notifications remain readable
after a service is renamed or archived.

## Order attachments

The public frontend uses a two-step flow that keeps the existing JSON Orders
contract stable:

1. `POST /api/uploads/order-attachments` receives one multipart batch and returns uploaded file IDs.
2. `POST /api/orders` receives those IDs in `attachmentIds` and creates `OrderAttachments` links atomically with the order.

The upload endpoint is anonymous because it is used before an order exists. It
accepts at most five non-empty files, each no larger than `5 MB`, and validates
content type, filename extension and basic file signature/magic bytes. Allowed
combinations are JPG/JPEG, PNG, WebP, and PDF. Server-generated random filenames
are used; the original filename is retained only as metadata.

PostgreSQL stores only `UploadedFiles` metadata, scan status and
`OrderAttachments` links. Physical development files are stored under
`backend/storage/uploads`, which is excluded by `.gitignore`. Uploads are first
written under `backend/storage/uploads/quarantine`, checked, and then moved to
their final folder only when accepted. Configuration is in `UploadStorage`:

```json
{
  "RootPath": "../../storage/uploads",
  "PublicBasePath": "/api/uploads",
  "MaxFileSizeBytes": 5242880,
  "MaxFilesPerRequest": 5,
  "OrphanCleanupAgeMinutes": 120
}
```

Administrators download linked files through protected
`GET /api/uploads/{uploadedFileId}` and can remove a linked order attachment through protected
`DELETE /api/orders/{orderId}/attachments/{attachmentId}`. Unauthenticated access returns `401`; the
frontend obtains the file as a Bearer-authenticated blob and downloads it using
the original filename. Files are not served from `wwwroot`.

All physical storage access goes through the `IUploadStorage` abstraction
(`backend/src/BespokeStudio.Infrastructure/Storage`). The only implementation
today is `LocalUploadStorage`, a local-filesystem adapter that owns the storage
root and all `File`/`Directory`/`FileStream` operations and delegates path
resolution and traversal protection to the existing `UploadStoragePath` helper.
The upload, cleanup, storage-maintenance and deletion services depend on the
interface rather than the filesystem directly, so a production object-storage
adapter (S3/Azure/R2) can be added later without touching those services. That
provider is not implemented yet. Storage keys stay relative and safe, and API
responses never expose absolute server paths. Physical development files still
live under `backend/storage/uploads` and public API contracts are unchanged.

To verify manually, submit an enquiry with an allowed file in `/order`, confirm
that a generated file appears under `backend/storage/uploads`, then sign in at
`/admin`, open the enquiry, confirm the attachment scan status is shown, and
select **Download** in Attachments.

### Upload security and ClamAV

Upload security is configured under `UploadSecurity:MalwareScanner`. The default
repository configuration keeps the provider `Disabled` so local development does
not require ClamAV. In this mode files are still written through quarantine and
validated by extension/content type/file signature, then stored with
`ScanStatus=Skipped`.

For production, configure the ClamAV daemon provider or another command-line
scanner through environment variables, secret store or an excluded production
settings file. The supported provider names are `Disabled`, `ClamAV` and
`CommandLine`; `ClamAV` uses a TCP ClamAV daemon (`clamd`) with the INSTREAM
protocol, while `CommandLine` uses a local executable such as `clamscan`.

```json
{
  "UploadSecurity": {
    "MalwareScanner": {
      "Provider": "ClamAV",
      "DisplayName": "ClamAV",
      "ClamAv": {
        "Host": "bespoke-studio-clamav",
        "Port": 3310,
        "MaxChunkSizeBytes": 8192
      },
      "TimeoutSeconds": 30,
      "TreatScannerErrorAsRejection": true
    }
  }
}
```

For a local executable scanner instead, use `Provider=CommandLine` with
`ExecutablePath=clamscan`, `Arguments=["--no-summary", "{filePath}"]` and the
configured clean/infected/error exit code lists.

When the scanner returns a clean result, metadata is stored with
`ScanStatus=Clean`, `ScanProvider` and `ScannedAt`, and the file is moved from
quarantine to final storage. Infected files or scanner failures are rejected and
not linked to orders. Admin order attachment cards show the recorded scan status.
Do not describe scanned files as "100% safe"; use wording such as "Security scan
completed".

`Provider=Disabled` is only for local/dev (files are stored with
`ScanStatus=Skipped`) and must not be used in production. Production scanner and
storage configuration must come from environment variables, a secret store or an
excluded server config file — never from committed appsettings. Keep
`TreatScannerErrorAsRejection=true` in production so scanner errors/timeouts
fail closed (`ScanFailed` → upload rejected). The full production uploads/ClamAV
runbook (storage path, ClamAV install, EICAR smoke test, backup/restore and
troubleshooting) is in
[`../UPLOADS_PRODUCTION_RU.md`](../UPLOADS_PRODUCTION_RU.md).


### Public request rate limits

ASP.NET Core fixed-window rate limiting is applied per remote IP to anonymous
write endpoints:

- `POST /api/uploads/order-attachments`: 10 requests per 10 minutes
- `POST /api/orders`: 5 requests per 10 minutes
- `POST /api/contact-messages`: 5 requests per 10 minutes

The values are configurable under `RateLimiting:PublicUploadPermitLimit`,
`RateLimiting:PublicOrderPermitLimit`, `RateLimiting:PublicContactPermitLimit`,
and `RateLimiting:WindowMinutes`.
Rejected requests return `429 Too Many Requests`, a JSON problem response, and
a `Retry-After` header. When the API is deployed behind a reverse proxy, trusted
forwarded-header configuration must be added before remote IP partitioning can
represent the original client address reliably.

### Login rate limit

`POST /api/auth/login` has its own per-remote-IP fixed-window rate limit
(`10` attempts per `15` minutes by default), configured through
`RateLimiting:AuthLoginPermitLimit` and `RateLimiting:AuthLoginWindowMinutes`.
This complements ASP.NET Core Identity account lockout: lockout protects a single
account, while the login rate limit slows password spraying and credential
stuffing from a single address. The limit is not applied to `POST /api/auth/refresh`
or to other admin endpoints, so normal session refresh and admin work are not
affected. Rejected requests return `429 Too Many Requests` with the same generic
problem response as other limited endpoints, so it does not reveal whether an
email exists. Because partitioning uses the remote IP, correct forwarded-header
configuration is required behind a reverse proxy.

### Security response headers

All responses include baseline security headers, applied via
`Response.OnStarting` so they are present on success, error (Problem Details) and
`429` responses:

- `X-Content-Type-Options: nosniff`
- `Referrer-Policy: strict-origin-when-cross-origin`
- `X-Frame-Options: DENY`
- `Permissions-Policy: camera=(), microphone=(), geolocation=(), payment=()`
- `Content-Security-Policy` (baseline, toggled by `SecurityHeaders:EnableContentSecurityPolicy`)

HTTP Strict Transport Security (`app.UseHsts()`) is enabled outside Development,
before HTTPS redirection, and Development is left untouched so localhost is not
pinned to HTTPS.

The baseline CSP is `default-src 'self'` with `frame-ancestors 'none'`,
`base-uri 'self'`, `form-action 'self'`, `object-src 'none'`,
`img-src 'self' data: blob:`, `font-src 'self' data:`,
`style-src 'self' 'unsafe-inline'`, `script-src 'self'` and `connect-src 'self'`.
This backend serves the JSON API and admin-served files only; it does not serve
the SPA HTML, so this CSP governs API responses. The host that serves the
frontend (Vite in development, static host/reverse proxy in production) must set
its own document CSP, including `connect-src` entries for the API origin and the
SignalR `wss:`/`ws:` endpoint. Set `SecurityHeaders:EnableContentSecurityPolicy`
to `false` to disable only the CSP header (other security headers stay on) if a
future single-origin deployment needs a different policy.

### Orphan upload cleanup

An orphan candidate is `OrderAttachment` upload metadata older than
`UploadStorage:OrphanCleanupAgeMinutes` (120 minutes by default). The cleanup
service rechecks that no `OrderAttachments` row references the file before it
deletes anything. Linked attachments are skipped. Missing physical files are
handled without failing the whole cleanup and their stale metadata is removed.

Run cleanup manually with an Admin JWT:

```powershell
$headers = @{ Authorization = "Bearer $($login.accessToken)" }
Invoke-RestMethod http://localhost:5099/api/uploads/cleanup-orphans `
  -Method Post `
  -Headers $headers
```

The response reports scanned candidates, deleted metadata, deleted physical
files, missing physical files, and skipped candidates. The endpoint returns
`401/403` without a valid Admin JWT. This endpoint handles old unlinked metadata
as a manual fallback; it is separate from automatic deletion jobs. There is no
scheduled full storage reconciliation yet. Production object storage, deep content inspection and image processing are
outside the current local-storage implementation.

### Automatic upload deletion outbox

Migration `AddUploadFileDeletionOutbox` adds `UploadFileDeletionJobs`. Order and
single-attachment deletion store only validated relative storage keys and create
jobs inside the same database transaction that removes attachment metadata.
No absolute filesystem path is stored or returned.

`UploadFileDeletionWorker` creates a DI scope every cycle, atomically claims due
Pending/Failed jobs, marks them Processing and deletes only paths verified under
the configured upload root. Missing files become Skipped; successful deletions
become Succeeded. Failures store a generic safe error, increment Attempts and
use exponential retry backoff up to `MaxAttempts`. Interrupted Processing jobs
are recovered after the configured timeout. Automatic successes use structured
server logging rather than noisy per-file Admin Audit Log records.

Configuration is under `UploadDeletion`: `PollIntervalSeconds`, `BatchSize`,
`MaxAttempts`, `BaseRetrySeconds` and `ProcessingTimeoutMinutes`.

### Admin storage maintenance

Admin → Storage uses protected `GET /api/admin/storage/scan` to compare all
`UploadedFiles` metadata rows with files under the configured local upload root.
The response contains full summary counts and sizes plus limited lists of orphan
physical files and missing physical files. API responses expose only safe
relative storage keys, never absolute server paths. Related Order, Portfolio,
Content or Site Settings information is included for missing files when available.

Protected `POST /api/admin/storage/delete-orphans` re-scans current physical
files and rechecks database references before every deletion. It accepts no file
path from the client, skips reparse-point entries, normalises every candidate
through the configured upload root and cannot delete outside that root. Missing
files encountered during cleanup are treated as already cleaned. Database rows
whose physical files are missing are report-only and are never automatically
removed by this endpoint. Every cleanup writes audit action
`storage.orphan_cleanup` with deleted/skipped/failed totals and deleted bytes.
Manual orphan cleanup remains a diagnostic/emergency operation. Normal order-file
cleanup is automatic; the page also shows Pending, Processing, Failed and
completed deletion-job counts plus safe failed-job retry details.

## Site Settings API

Site settings use one strongly typed singleton row rather than a generic
key/value store. Migration `AddSiteSettings` creates and seeds that row with
safe development defaults. If the row is removed, `SiteSettingsService`
recreates the same fixed-ID default on the next request.

Available endpoints:

- `GET /api/site-settings/public` — anonymous public contact, social and footer settings
- `GET /api/admin/site-settings` — complete settings including notification toggles (Admin JWT required)
- `PATCH /api/admin/site-settings` — validates and updates settings (Admin JWT required)

The public response exposes the single studio email and phone but excludes
notification enabled flags, business legal name, and other admin-only metadata. The Admin Settings UI groups
settings into modules with separate save actions while still using the same validated
site settings update endpoint. The update endpoint returns
`400 ValidationProblem` for an empty studio name, invalid email/phone values,
invalid non-HTTP(S) URLs, or configured values exceeding their limits.

The email notification enabled flag uses the same email shown on the public
site. The phone remains public contact information and is not a notification
destination. Migrations `NormalizeSiteSettingsContacts` and
`RemoveWhatsAppNotifications` remove the former duplicate and WhatsApp fields.

## Notification foundation

After `POST /api/orders` persists an enquiry or `POST /api/contact-messages`
persists a contact message, `INotificationService` loads the saved record and
current Site Settings. When owner notifications are enabled it queues a prepared
message in `EmailOutboxMessages` for the Site Settings owner email. When customer
confirmations are enabled it queues a separate plain-text confirmation for
the customer email from the Order or Contact form. Customer confirmation subject
and body templates are stored in `SiteSettings` and are editable in Admin
Settings. Supported placeholders include `{{studioName}}`, `{{customerName}}`,
`{{customerEmail}}`, `{{customerPhone}}`, Order-only `{{serviceName}}`,
`{{preferredDate}}`, `{{orderReference}}`, and Contact-only `{{messageSubject}}`,
`{{contactReference}}`. The reference placeholders render the human-readable
request numbers, not raw GUIDs.

For order creation, the successful business-record save is the commit boundary
and source of truth. Outbox enqueue and the SignalR admin event run afterward as
independent best-effort operations. Enqueue or provider failure does not replace
the public `201 Created` response. The public contracts and `Location` headers
are unchanged.

`EmailOutboxWorker` claims due `Pending`/`Failed` rows atomically, sends through
the existing `IEmailNotificationSender`, and updates the linked Email Log row.
Success becomes `Sent`; temporary failure becomes `Retrying` with delays of 1,
5, 15 and up to 60 minutes; exhausted attempts become `Failed`. Defaults are a
30-second worker interval, batch size 20 and maximum five attempts. Interrupted
`Processing` rows are recovered after five minutes. Configuration is under
`EmailOutbox`; it contains no secrets. Single-instance and basic concurrent
claiming are supported; stronger distributed coordination remains future work.

Admin-managed email delivery settings are checked first: `Configuration` keeps
the existing configuration-based provider, `GmailSmtp` sends through Gmail using
the owner-managed Gmail address and protected Google App Password stored on the
backend, and `ResendApi` sends through `POST https://api.resend.com/emails`
using a protected Resend API key. The Resend API key and Gmail App Password are
never returned by admin APIs. Production Resend defaults are
`From: Bespoke Sewing Studio <noreply@oksanalogosha.com>` and
`Reply-To: contact@oksanalogosha.com`. If admin delivery mode is
`Configuration`, `Provider=Logging` uses `LoggingEmailNotificationSender` and
`Provider=Smtp` uses `SmtpEmailNotificationSender`. Missing/invalid provider
configuration and provider delivery errors are logged and fall back to the
logging provider without changing the successful order or contact message
response.


`GET /api/admin/email-log` is protected by the `AdminOnly` policy. It returns
the newest email delivery attempts and supports `page`, `pageSize`, `search`,
`messageType`, `status`, `recipientEmail` and `provider` query filters. It is
used by Admin → Email Log. The frontend auto-applies filters, refreshes from
admin realtime events when entries are written and can export the visible rows
to CSV. Admin Email Log separates global outbox monitoring from current-page log
entries and supports manual retry and retention cleanup in the UI.

`POST /api/admin/email-log/{id}/retry` is protected by the same policy and lets
an admin queue a manual retry for an exhausted failed outbox message (status
`Failed`, attempts at the maximum and no scheduled next attempt). It resets the
existing outbox message to `Pending` with a fresh next-attempt time and zeroed
attempts so the background `EmailOutboxWorker` picks it up again; it does not
create a new email, alter the stored body or change the automatic retry/backoff
behaviour. Attempts increase only when the worker actually claims the message.
The endpoint returns `404` when no linked message exists and `409` when the
message is not eligible (for example already queued, processing, sent, or still
waiting for an automatic retry). Successful retries are recorded in the admin
audit log (`email_outbox.manual_retry_queued`) with safe metadata only. Email
bodies and secrets are never exposed in the request, response, audit metadata or
logs.

`GET /api/admin/email-log/summary` is protected by the same policy and returns
a read-only outbox monitoring summary for Admin → Dashboard and Admin → Email
Log. It aggregates `EmailOutboxMessages` counts only: pending, processing,
retrying (failed with attempts remaining and a scheduled next attempt), failed,
exhausted-failed (failed with attempts at the maximum and no next attempt), stale
pending (pending older than 15 minutes), sent in the last 24h and failed in the
last 24h, plus the oldest pending/failed timestamps and a derived
`healthStatus` (`Healthy`/`Warning`/`Critical`) and safe `summaryMessage`. The
endpoint is read-only: it does not enqueue, send or change automatic
retry/backoff or manual retry behaviour, and it never selects or exposes email
bodies (`HtmlBody`/`TextBody`), recipients, subjects or secrets.

`GET /api/admin/email-log/retention` and `POST /api/admin/email-log/retention/cleanup`
are protected by the same policy. Retention is configured through
`EmailOutboxRetention` in `appsettings.json` (worker disabled by default). The
summary endpoint returns safe counts only: body-purge and delete candidates for
old `Succeeded`/`Skipped` outbox messages, failed messages retained for review,
configured retention periods and worker status. The manual cleanup endpoint runs
one bounded batch per category: first deletes outbox rows older than message
retention, then replaces real bodies on remaining old `Succeeded`/`Skipped`
messages with the configured placeholder (`HtmlBody = null`,
`TextBody = "[Email body purged by retention policy.]"`) to satisfy the existing
body check constraint. `EmailDeliveryLogEntry` rows are never deleted. Failed,
retrying, pending and processing messages are never purged or deleted. An
optional `EmailOutboxRetentionWorker` hosted service can run the same cleanup on
a schedule when `WorkerEnabled=true`. Successful manual cleanup is recorded in
the admin audit log (`email_outbox.retention_cleanup_ran`) with count metadata
only. No schema migration is required; automatic retry/backoff and manual retry
behaviour are unchanged; email bodies and secrets are never exposed in API,
audit or logs.

Email log entries are written for owner order notifications, customer order
confirmations, owner contact notifications, customer contact confirmations and
test emails. Only metadata is persisted in the log: recipient, subject, provider,
queued/retrying/sent/failed status, external-delivery flag, result/error summary,
related entity reference and timestamps. Prepared bodies are stored in the
protected outbox table for deferred delivery but are not returned by Email Log
APIs. SMTP credentials, Google App Passwords and tokens are never stored there.

`POST /api/admin/notifications/test-email` is protected by the `AdminOnly`
policy. It requires enabled email notifications and a Site Settings email, then
uses the currently configured provider and returns a summary containing only
provider/result metadata—never SMTP credentials.

For Resend API, successful test email results include the Resend message id in
safe result metadata. Test email responses never return SMTP credentials, Gmail
App Passwords or Resend API keys.

`GET /api/admin/production-readiness` is protected by the same policy and
returns safe readiness checks for Admin Dashboard. It validates the selected
email provider configuration, reuses the outbox monitoring summary, runs a
lightweight clean-file ClamAV probe when `UploadSecurity:MalwareScanner:Provider`
is `ClamAV`, and performs DNS-over-HTTPS TXT/MX lookups for
`resend._domainkey.oksanalogosha.com`, `send.oksanalogosha.com` and
`_dmarc.oksanalogosha.com`. The response contains status, evidence and missing
items only; it never returns secrets or raw provider credentials. Cloudflare
Email Routing MX records on the apex domain are not treated as an error because
incoming mail is intentionally routed to Gmail.

Raw SMTP credentials and Resend API keys must not be stored in source control,
committed appsettings files, screenshots or project documentation.
Developer-managed SMTP credentials come from environment variables,
`dotnet user-secrets`, or an external secret store. Owner-managed Gmail SMTP
stores only a protected Google App Password in the singleton `SiteSettings` row
through ASP.NET Core Data Protection; owner-managed Resend API stores only a
protected API key in the same row. The protected values are never returned by
admin APIs. Production deployments that use owner-managed Gmail SMTP or Resend
API must persist Data Protection keys outside the app deployment directory so
protected values remain decryptable after restarts or redeployments.
`Provider=Logging` is only the local development/dev fallback and must not be
relied on for production delivery (it only writes to the log and does not send
externally). Production `Notifications:Email:Smtp:*` values must come from
environment variables or a managed secret store, never from committed config.

The full production email runbook (Resend API, Gmail SMTP fallback, Cloudflare
DNS SPF/DKIM/DMARC checklist and a production smoke test) is in
[`../SMTP_PRODUCTION_RU.md`](../SMTP_PRODUCTION_RU.md).

Mandatory production email checklist:

- use owner-managed **Admin > Settings > Email delivery > Resend API** for the
  primary production sender `noreply@oksanalogosha.com`
- keep incoming mail on Cloudflare Email Routing:
  `contact@oksanalogosha.com -> bespoke.studio.ni@gmail.com` and
  `orders@oksanalogosha.com -> bespoke.studio.ni@gmail.com`
- keep Gmail SMTP or developer-managed `Notifications:Email:Provider=Smtp` only
  as fallback unless a new production decision changes the provider
- for developer-managed SMTP, configure `Host`, `Port`, `Username`, `Password`,
  `FromEmail`, `FromName` and `UseSsl`
- use `dotnet user-secrets` only for local development
- use environment variables or a managed secret store in production for
  developer-managed SMTP secrets
- persist ASP.NET Core Data Protection keys in production when using
  owner-managed Gmail SMTP or Resend API
- never commit SMTP usernames/passwords, Google App Passwords or Resend API keys
- if Gmail is used, enable Google 2-Step Verification and create a Google App
  Password; do not use the normal Gmail account password for SMTP
- verify **Admin > Settings > Email notifications enabled** and the owner/public
  email address
- verify real delivery with `POST /api/admin/notifications/test-email`, then by
  submitting the public Contact form and Order form
- monitor `Retrying` and `Failed` Email Log/outbox records in production
- confirm Admin Dashboard **DNS email records** is green for
  `resend._domainkey.oksanalogosha.com`, `send.oksanalogosha.com` SPF/MX and
  `_dmarc.oksanalogosha.com`
- add operational monitoring for provider failures, bounce/rejection handling,
  exhausted retries and credential rotation

Apply pending migrations before starting the updated API:

```powershell
dotnet ef database update --project backend/src/BespokeStudio.Infrastructure --startup-project backend/src/BespokeStudio.Api
```

When generating an idempotent PostgreSQL migration script, verify that migration
`20260710120000_AddResendEmailDeliverySettings` is present with
`EmailDeliveryResendApiKeyProtected`, `EmailDeliveryResendFromEmail` and
`EmailDeliveryReplyToEmail`, and that migration
`20260629210000_AddHumanReadableRequestReferences` uses `PERFORM setval(...)`
inside its PL/pgSQL `DO` block. `SELECT setval(...)` inside that block is invalid
for PostgreSQL idempotent scripts.

Generic local SMTP setup:

```powershell
dotnet user-secrets set "Notifications:Email:Provider" "Smtp" --project backend/src/BespokeStudio.Api
dotnet user-secrets set "Notifications:Email:Smtp:Host" "smtp.example.com" --project backend/src/BespokeStudio.Api
dotnet user-secrets set "Notifications:Email:Smtp:Port" "587" --project backend/src/BespokeStudio.Api
dotnet user-secrets set "Notifications:Email:Smtp:Username" "your-user" --project backend/src/BespokeStudio.Api
dotnet user-secrets set "Notifications:Email:Smtp:Password" "your-password" --project backend/src/BespokeStudio.Api
dotnet user-secrets set "Notifications:Email:Smtp:FromEmail" "no-reply@example.com" --project backend/src/BespokeStudio.Api
dotnet user-secrets set "Notifications:Email:Smtp:FromName" "Bespoke Sewing Studio" --project backend/src/BespokeStudio.Api
dotnet user-secrets set "Notifications:Email:Smtp:UseSsl" "true" --project backend/src/BespokeStudio.Api
```

Gmail local SMTP example:

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

Equivalent environment variables use double underscores, for example
`Notifications__Email__Smtp__Password`. Customer confirmation emails use their
own Admin Settings toggle and templates instead of being mixed with owner
notifications. WhatsApp and SMS notification channels are intentionally not
implemented.

## Administrator authentication

Admin sessions use a 15-minute JWT access token plus a rotating 14-day refresh
token. Login returns only the access token in JSON and sets the refresh token in
an HttpOnly, SameSite=Lax cookie (`Secure` outside Development). PostgreSQL table
`AdminRefreshTokens` stores only a SHA-256 hash and bounded session metadata.

`POST /api/auth/refresh` rotates the refresh token and returns a new access token.
Reuse of a revoked token returns `401` and revokes the token family.
`POST /api/auth/logout` is idempotent, revokes the current token when present and
always expires the cookie. CORS credentials are allowed only for configured
frontend origins. Production refresh requires HTTPS. Missing, invalid, expired,
revoked and reused refresh attempts return a generic `401`, clear the cookie and
write a safe audit event. Raw tokens and hashes are never logged or written to audit.

The admin frontend keeps the returned access token in module memory only, never
in `sessionStorage` or `localStorage`. A page reload clears that memory and the
frontend restores the session by calling `/api/auth/refresh` with credentials,
then retries protected API requests at most once after a successful refresh.
Logout, password change and current-session revocation clear the in-memory token.
SignalR connection/reconnect reads the latest token from the same memory store.
The refresh cookie remains HttpOnly and inaccessible to frontend JavaScript.
This reduces persistence risk but does not remove the need for CSP and XSS
prevention.

Admin session management endpoints require the `AdminOnly` policy:

- `GET /api/auth/sessions` returns one safe logical session per refresh-token family;
- `POST /api/auth/sessions/{id}/revoke` revokes only a session owned by the current user;
- `POST /api/auth/sessions/revoke-others` revokes every other active refresh session and returns `revokedCount`.

The current session is identified by hashing the HttpOnly refresh cookie and matching
that hash to the current user's token record. A missing cookie still permits listing
sessions without a current marker; revoke-others returns `400` until the user signs in
again. Revoking the current session expires the cookie. Responses expose no token,
token hash or cookie value; user-agent text is bounded and IP addresses are masked.
Manual revocation writes `auth.session_revoked` or `auth.other_sessions_revoked` audit actions.

Authentication uses ASP.NET Core Identity with PostgreSQL-backed users and
roles. `POST /api/auth/login` accepts an email and password. Accounts without 2FA
receive the existing JWT/refresh session. Accounts with 2FA receive only a
five-minute Data Protection-protected HttpOnly challenge cookie and a
`requiresTwoFactor` response; no JWT, refresh token or session row is created yet.
`POST /api/auth/2fa/verify` accepts either an Identity authenticator TOTP or a
one-time recovery code, clears the challenge and then creates the normal session.
The login and verify steps have independent per-IP fixed-window limits, and failed
2FA attempts participate in Identity lockout accounting.

Authenticated current-admin 2FA endpoints are:

- `GET /api/auth/2fa/status`;
- `POST /api/auth/2fa/setup` and `POST /api/auth/2fa/enable`;
- `POST /api/auth/2fa/disable`;
- `POST /api/auth/2fa/recovery-codes/reset`;
- `POST /api/auth/2fa/authenticator/reset`.

Setup returns a manual key and `otpauth://` URI only to the authenticated current
admin. Enablement generates ten recovery codes and returns them once. Disable,
recovery-code regeneration and authenticator reset require the current password.
Because Identity changes the security stamp for authenticator and 2FA settings,
each authenticated 2FA mutation response also returns a replacement access-token
response. The frontend immediately replaces its module-memory token; it does not
persist that token or create another refresh session.
Identity's existing `TwoFactorEnabled` field and `AspNetUserTokens` storage are
used, so this feature requires no new migration. TOTP values, recovery codes,
authenticator keys, URIs, passwords and tokens are never logged or audited.

Issued JWTs contain an Identity security-stamp claim. Bearer validation loads the
current user and rejects deleted, locked/disabled, non-Admin or stale-stamp users;
this applies to protected HTTP endpoints and the SignalR query-string token flow.
`GET /api/auth/me` validates a Bearer token and returns the current user.
`POST /api/auth/me/password` requires the `Admin` role and lets the current admin
change their own password by providing the current password, new password and
confirmation. A successful change updates the security stamp, revokes every refresh
session with reason `password_changed`, clears the refresh cookie and requires a new
login. The `AdminOnly` policy requires the `Admin` role for Orders read/status/note routes.
Invalid email and invalid password both return the same `401 Unauthorized`
response. Login success/failure, logout, failed/reused refresh and bulk session
revocation are audited without passwords, tokens, hashes or cookie values.

The administrator seed runs only in Development and only when both
`SeedAdmin:Email` and `SeedAdmin:Password` are configured. It creates the role
and missing user, then assigns the role. It never replaces an existing user's
password. Do not put real credentials or production JWT keys in tracked JSON
files.

Configure local secrets from the repository root:

```powershell
dotnet user-secrets set "SeedAdmin:Email" "admin@example.com" --project backend/src/BespokeStudio.Api
dotnet user-secrets set "SeedAdmin:Password" "replace-with-a-strong-local-password" --project backend/src/BespokeStudio.Api
dotnet user-secrets set "Jwt:SigningKey" "replace-with-at-least-32-random-characters" --project backend/src/BespokeStudio.Api
```

Environment-variable equivalents for deployment or temporary local use are:

```powershell
$env:SeedAdmin__Email = "admin@example.com"
$env:SeedAdmin__Password = "replace-with-a-strong-local-password"
$env:Jwt__SigningKey = "replace-with-at-least-32-random-characters"
```

`appsettings.Development.json` contains a development-only JWT key so the API
can start without configuring secrets. Replace it through user-secrets for
shared development and always provide a separate strong key in production.
No seed email or password is stored in the repository.

After applying migrations and starting the API, request a token:

```powershell
$login = Invoke-RestMethod http://localhost:5099/api/auth/login `
  -Method Post `
  -ContentType "application/json" `
  -Body '{"email":"admin@example.com","password":"replace-with-a-strong-local-password"}'

$headers = @{ Authorization = "Bearer $($login.accessToken)" }
Invoke-RestMethod http://localhost:5099/api/auth/me -Headers $headers
Invoke-RestMethod http://localhost:5099/api/orders -Headers $headers
```

In Swagger, call `/api/auth/login`, copy `accessToken`, select **Authorize**,
and paste the token value. Swagger supplies the `Bearer` prefix. The protected
routes then become executable with that token.

Commands:

```powershell
dotnet restore backend/BespokeStudio.sln
```

Run the xUnit backend test foundation before commits that touch backend code and
before each production release:

```powershell
dotnet test backend\BespokeStudio.sln
npm.cmd run typecheck
npm.cmd run build
dotnet build backend\BespokeStudio.sln
```

The initial suite contains infrastructure-independent unit tests for order request
validation, pagination and email-outbox retry timing. It does not require
PostgreSQL, SMTP, ClamAV, uploaded files or local secrets. API tests backed by a
dedicated PostgreSQL test database, full auth/2FA flows and order/contact/upload
integration tests are deferred to Tasks 49.2 and 49.3.

## PostgreSQL integration tests

Optional PostgreSQL-backed integration tests live in
`tests/BespokeStudio.Tests/Integration/PostgreSql/`. They are **opt-in**:
default `dotnet test backend\BespokeStudio.sln` does **not** require PostgreSQL and
skips these tests when env vars are not set (CI/dev safe).

When enabled, each test:

- creates a temporary database `bespoke_studio_integration_<guid>`;
- applies EF Core migrations via `Database.MigrateAsync()`;
- runs PostgreSQL-sensitive persistence checks;
- drops only the generated database on cleanup (`DROP DATABASE ... WITH (FORCE)`).

**Never** point env vars at a production database. Use a dedicated admin/test
PostgreSQL connection with permission to create/drop temporary databases only.

Recommended local flow:

```powershell
cd C:\Projects\Bespoke_sewing_studio
docker compose -f docker-compose.postgres.yml up -d postgres
$env:BESPOKESTUDIO_RUN_POSTGRES_INTEGRATION_TESTS = "true"
$env:BESPOKESTUDIO_POSTGRES_ADMIN_CONNECTION_STRING = "<admin-test-postgres-connection-string>"
dotnet test backend\BespokeStudio.sln --filter "FullyQualifiedName~PostgreSql"
Remove-Item Env:\BESPOKESTUDIO_RUN_POSTGRES_INTEGRATION_TESTS -ErrorAction SilentlyContinue
Remove-Item Env:\BESPOKESTUDIO_POSTGRES_ADMIN_CONNECTION_STRING -ErrorAction SilentlyContinue
```

For local Docker Compose dev PostgreSQL, connection details are defined in
`docker-compose.postgres.yml` at the repository root. Build an admin connection
string from those values yourself; do not commit or copy production secrets into
Git.

GitHub Actions runs the suite automatically from `.github/workflows/ci.yml` on
every pull request and every push to `main`. CI restores and builds the solution
with the .NET 10.0.x SDK in Release configuration, then runs tests with
`--no-build`. It does not start PostgreSQL, apply migrations, use SMTP credentials
or deploy the application. The commands above remain the required local checks.

```powershell
dotnet build backend/BespokeStudio.sln
```

```powershell
dotnet run --project backend/src/BespokeStudio.Api/BespokeStudio.Api.csproj
```

If the local user-level `NuGet.Config` is inaccessible in your environment, the repository also includes `backend/NuGet.Config` and restore can be run with:

```powershell
dotnet restore backend/BespokeStudio.sln --configfile backend/NuGet.Config
```

Available core endpoints after startup include:

- `/swagger`
- `/health`
- `/health/live`
- `/health/ready`
- `/healthz`
- `/readyz`
- `/api/health`
- `/api/version`
- `/api/auth/login`
- `/api/auth/me`
- `/api/auth/me/password`
- `/api/orders`
- `/api/contact-messages`
- `/api/services`
- `/api/portfolio`
- `/api/content/pages/{pageKey}`
- `/api/repeatable-content`
- `/api/repeatable-content/groups/{groupKey}`
- `/api/admin/contact-messages`
- `/api/admin/users`
- `/api/admin/audit-log`
- `/api/site-settings/public`
- `/api/brand-settings/public`

With the API running, verify the system endpoints from another PowerShell
window:

```powershell
Invoke-WebRequest http://localhost:5099/api/health -UseBasicParsing
Invoke-WebRequest http://localhost:5099/api/version -UseBasicParsing
Invoke-WebRequest http://localhost:5099/health -UseBasicParsing
Invoke-WebRequest http://localhost:5099/health/live -UseBasicParsing
Invoke-WebRequest http://localhost:5099/health/ready -UseBasicParsing
Invoke-WebRequest http://localhost:5099/healthz -UseBasicParsing
Invoke-WebRequest http://localhost:5099/readyz -UseBasicParsing
Invoke-WebRequest http://localhost:5099/swagger/index.html -UseBasicParsing
```

Expected result with PostgreSQL running: all requests return HTTP `200`.
`/health/ready` and `/readyz` return `503` when PostgreSQL cannot be reached;
the liveness, version and compatibility endpoints remain available in that condition.

Portfolio/Gallery CRUD and its dedicated image upload are implemented. General
upload-library management is not implemented. Public pages are backend-first for
Site/Brand Settings, Services, Portfolio, Page Content, Repeatable Content and Brand/SEO settings;
centralised typed frontend fallbacks are used only when a public API is
unavailable. The backend does not implement multilingual content variants; the
current product scope is English-only. Refresh tokens, password reset, email
confirmation, MFA, and production secret rotation are not implemented.
