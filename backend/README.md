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
- Admin Contact Messages module lists messages, filters by status and updates workflow state
- a protected SignalR admin-notifications hub broadcasts Order and Contact Message changes to open admin sessions
- Site Settings and Brand/Logo/SEO settings provide public contact, navigation, logo, CTA and metadata configuration
- public Order and Contact submissions use rate limits plus lightweight honeypot/timing anti-spam checks
- email notification foundation supports owner notifications for Orders and Contact Messages through Logging and SMTP providers; WhatsApp/SMS channels are intentionally not implemented
- the product is English-only; multilingual CMS, language fields and EN/UA switching are not part of the current scope

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
`BSS-ORD-YYYY-000001` for orders and `BSS-CON-YYYY-000001` for contact messages.

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

- Orders and client records
- Contact messages
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

Full backup/restore instructions live in `../BACKUP_RESTORE_RU.md`. The short
version for this backend is:

- create a PostgreSQL dump with `pg_dump --format=custom`;
- back up `backend/storage` separately because database dumps store upload
  metadata only, not physical files;
- keep backups outside the Git repository;
- never commit `.dump`, `.sql`, storage archives, `.env`, production appsettings
  files, SMTP credentials or Google App Passwords;
- preserve ASP.NET Core Data Protection keys in production when using protected
  owner-managed Gmail SMTP settings;
- verify every important dump with `pg_restore --list`;
- test restore before relying on a backup procedure.

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

The first audit scope records administrator user management, own-account password
changes, order status/note changes, contact message status changes, Site Settings updates, Email Delivery
updates and Brand / SEO updates. The audit log intentionally stores no
passwords, Gmail App Passwords or raw SMTP secrets.

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
never returned by the API.

## Orders API

The Orders API is available under `/api/orders`. Each order has an internal GUID
`id` and a customer-facing `referenceNumber` such as `BSS-ORD-2026-000001`:

- `POST /api/orders` anonymously creates an enquiry and returns `201 Created`
- `GET /api/orders?take=100` returns the newest enquiries (Admin JWT required)
- `GET /api/orders/{id}` returns enquiry details (Admin JWT required)
- `PATCH /api/orders/{id}/status` updates the workflow status (Admin JWT required)
- `POST /api/orders/{id}/notes` adds an internal note (Admin JWT required)

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

- `GET /api/admin/contact-messages?take=100&status=New` returns newest messages, optionally filtered by status
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
`GET /api/uploads/{uploadedFileId}`. Unauthenticated access returns `401`; the
frontend obtains the file as a Bearer-authenticated blob and downloads it using
the original filename. Files are not served from `wwwroot`.

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

For production, configure ClamAV or another command-line scanner through
environment variables, secret store or an excluded production settings file:

```json
{
  "UploadSecurity": {
    "MalwareScanner": {
      "Provider": "ClamAV",
      "DisplayName": "ClamAV",
      "ExecutablePath": "clamscan",
      "Arguments": ["--no-summary", "{filePath}"],
      "TimeoutSeconds": 30,
      "CleanExitCodes": [0],
      "InfectedExitCodes": [1],
      "ErrorExitCodes": [2],
      "TreatScannerErrorAsRejection": true
    }
  }
}
```

When the scanner returns a clean result, metadata is stored with
`ScanStatus=Clean`, `ScanProvider` and `ScannedAt`, and the file is moved from
quarantine to final storage. Infected files or scanner failures are rejected and
not linked to orders. Admin order attachment cards show the recorded scan status.
Do not describe scanned files as "100% safe"; use wording such as "Security scan
completed".


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
`401/403` without a valid Admin JWT. There is no scheduled background cleanup
yet. Production object storage, deep content inspection and image processing are
outside the current local-storage implementation.

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
current Site Settings. When owner notifications are enabled it calls
`IEmailNotificationSender` with the Site Settings owner email. When customer
confirmations are enabled it also sends a separate plain-text confirmation to
the customer email from the Order or Contact form. Customer confirmation subject
and body templates are stored in `SiteSettings` and are editable in Admin
Settings. Supported placeholders include `{{studioName}}`, `{{customerName}}`,
`{{customerEmail}}`, `{{customerPhone}}`, Order-only `{{serviceName}}`,
`{{preferredDate}}`, `{{orderReference}}`, and Contact-only `{{messageSubject}}`,
`{{contactReference}}`. The reference placeholders render the human-readable
request numbers, not raw GUIDs.

Admin-managed email delivery settings are checked first: `Configuration` keeps
the existing configuration-based provider, while `GmailSmtp` sends through Gmail
using the owner-managed Gmail address and protected Google App Password stored
on the backend. If admin delivery mode is `Configuration`, `Provider=Logging`
uses `LoggingEmailNotificationSender` and `Provider=Smtp` uses
`SmtpEmailNotificationSender`. Missing/invalid SMTP configuration and SMTP
delivery errors are logged and fall back to the logging provider without
changing the successful order or contact message response.


`GET /api/admin/email-log` is protected by the `AdminOnly` policy. It returns
the newest email delivery attempts and supports `take`, `search`,
`messageType`, `status`, `recipientEmail` and `provider` query filters. It is
used by Admin → Email Log. The frontend auto-applies filters, refreshes from
admin realtime events when entries are written and can export the visible rows
to CSV.

Email log entries are written for owner order notifications, customer order
confirmations, owner contact notifications, customer contact confirmations and
test emails. Only metadata is persisted: recipient, subject, provider, sent/failed
status, external-delivery flag, result/error summary, related entity reference
and timestamps. Email bodies, SMTP credentials, Google App Passwords and tokens
are intentionally never stored in the email log.

`POST /api/admin/notifications/test-email` is protected by the `AdminOnly`
policy. It requires enabled email notifications and a Site Settings email, then
uses the currently configured provider and returns a summary containing only
provider/result metadata—never SMTP credentials.

Raw SMTP credentials must not be stored in source control, committed appsettings
files, screenshots or project documentation. Developer-managed SMTP credentials
come from environment variables, `dotnet user-secrets`, or an external secret
store. Owner-managed Gmail SMTP stores only a protected Google App Password in
the singleton `SiteSettings` row through ASP.NET Core Data Protection; the
password is never returned by admin APIs. Production deployments that use
owner-managed Gmail SMTP must persist Data Protection keys outside the app
deployment directory so the protected value remains decryptable after restarts
or redeployments. `Provider=Logging` is only the local development fallback.

Mandatory production email checklist:

- choose either developer-managed `Notifications:Email:Provider=Smtp` or
  owner-managed **Admin > Settings > Email delivery > Gmail SMTP**
- for developer-managed SMTP, configure `Host`, `Port`, `Username`, `Password`,
  `FromEmail`, `FromName` and `UseSsl`
- use `dotnet user-secrets` only for local development
- use environment variables or a managed secret store in production for
  developer-managed SMTP secrets
- persist ASP.NET Core Data Protection keys in production when using
  owner-managed Gmail SMTP
- never commit SMTP usernames/passwords or Google App Passwords
- if Gmail is used, enable Google 2-Step Verification and create a Google App
  Password; do not use the normal Gmail account password for SMTP
- verify **Admin > Settings > Email notifications enabled** and the owner/public
  email address
- verify real delivery with `POST /api/admin/notifications/test-email`, then by
  submitting the public Contact form and Order form
- configure SPF, DKIM and DMARC before production use
- add operational monitoring for SMTP failures, bounce/rejection handling,
  credential rotation and a later background queue/retry policy

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
dotnet user-secrets set "Notifications:Email:Smtp:Password" "replace-with-google-app-password" --project backend/src/BespokeStudio.Api
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

Authentication uses ASP.NET Core Identity with PostgreSQL-backed users and
roles. `POST /api/auth/login` accepts an email and password and returns a JWT.
`GET /api/auth/me` validates a Bearer token and returns the current user.
`POST /api/auth/me/password` requires the `Admin` role and lets the current admin
change their own password by providing the current password, new password and
confirmation. The `AdminOnly` policy requires the `Admin` role for Orders read/status/note routes.
Invalid email and invalid password both return the same `401 Unauthorized`
response. JWT lifetime is four hours in development.

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
Invoke-WebRequest http://localhost:5099/swagger/index.html -UseBasicParsing
```

Expected result: all three requests return HTTP `200`. These endpoints do not
execute database queries, so they also remain available when PostgreSQL is
offline. Database connectivity is verified separately by `database update` and
the `__EFMigrationsHistory` query above.

Portfolio/Gallery CRUD and its dedicated image upload are implemented. General
upload-library management is not implemented. Public pages are backend-first for
Site/Brand Settings, Services, Portfolio, Page Content, Repeatable Content and Brand/SEO settings;
centralised typed frontend fallbacks are used only when a public API is
unavailable. The backend does not implement multilingual content variants; the
current product scope is English-only. Refresh tokens, password reset, email
confirmation, MFA, and production secret rotation are not implemented.
