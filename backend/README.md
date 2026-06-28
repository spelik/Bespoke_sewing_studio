# Bespoke Sewing Studio backend

ASP.NET Core Web API skeleton for the Bespoke Sewing Studio project.

Current status:

- backend contains the persistence foundation and the first Orders module
- domain model and application contract drafts are in place
- EF Core persistence is configured for PostgreSQL
- the database migrations are included and applied in local development
- Orders/enquiries API persists clients, orders, statuses, and internal notes
- ASP.NET Core Identity stores administrator accounts and roles
- JWT Bearer authentication protects the Orders administration endpoints
- Portfolio/Gallery categories, items and images are managed through protected admin endpoints

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
orders and notes/attachments, portfolio items and categories, service offerings,
and uploaded-file metadata. Domain enums describe order status, service type,
portfolio publication status, and upload purpose.

The domain does not reference EF Core, database attributes, `DbContext`, or a
storage provider. Email, phone, and money value objects remain a future design
decision after validation and currency rules are agreed.

## Application contracts

`BespokeStudio.Application` contains request/response records and service
interfaces. These contracts are separate from domain entities so future HTTP
payloads do not expose persistence models directly.

No mock or production service implementation is registered yet. The existing
API surface is intentionally limited to system endpoints until persistence and
business rules are implemented.

## Planned modules

- Orders
- Clients
- Portfolio and categories
- Services
- Uploads

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

The repository currently contains `InitialCreate`, `AllowClientsWithoutEmail`,
`AddIdentityAuth`, and `AddSiteSettings`. All four migrations were applied to the local
development database. Installing the matching CLI tool, if it is missing
locally:

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

## Orders API

The first persistence-backed API module is available under `/api/orders`:

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
  "attachmentIds": null
}
```

At least one of `email` or `phone` is required. Client matching first checks a
normalised email and then an exact trimmed phone. A new client is created only
when neither value matches an existing client.

Start the API and use `/swagger` for interactive testing, or open
`BespokeStudio.Api/BespokeStudio.Api.http` and run its prepared requests. Copy
the id returned by `POST /api/orders` into the `OrderId` variable before running
the detail, status, and note examples.

New-order email notifications use the logging provider by default and can use SMTP.
The public frontend Order form calls
anonymous `POST /api/orders`. The admin frontend uses
login, current-user, Orders list/detail, status, note, Services and Settings
endpoints; the other admin dashboard sections remain prototypes.

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
both content type and filename extension. Allowed combinations are JPG/JPEG,
PNG, WebP, and PDF. Server-generated random filenames are used; the original
filename is retained only as metadata.

PostgreSQL stores only `UploadedFiles` metadata and `OrderAttachments` links.
Physical development files are stored under `backend/storage/uploads`, which
is excluded by `.gitignore`. Configuration is in `UploadStorage`:

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
`/admin`, open the enquiry, and select **Download** in Attachments.

### Public request rate limits

ASP.NET Core fixed-window rate limiting is applied per remote IP to the two
anonymous write endpoints:

- `POST /api/uploads/order-attachments`: 10 requests per 10 minutes
- `POST /api/orders`: 5 requests per 10 minutes

The values are configurable under `RateLimiting:PublicUploadPermitLimit`,
`RateLimiting:PublicOrderPermitLimit`, and `RateLimiting:WindowMinutes`.
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
yet. Production object storage, antivirus/deep-content scanning, and image
processing are also outside the current local-storage implementation.

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
notification enabled flags, business legal name, and other admin-only metadata. The update endpoint returns
`400 ValidationProblem` for an empty studio name, invalid email/phone values,
invalid non-HTTP(S) URLs, or configured values exceeding their limits.

The email notification enabled flag uses the same email shown on the public
site. The phone remains public contact information and is not a notification
destination. Migrations `NormalizeSiteSettingsContacts` and
`RemoveWhatsAppNotifications` remove the former duplicate and WhatsApp fields.

## Notification foundation

After `POST /api/orders` persists an enquiry, `INotificationService` loads the
order and current Site Settings. When email notifications are enabled it calls
`IEmailNotificationSender` with the Site Settings email. `Provider=Logging`
uses `LoggingEmailNotificationSender`; `Provider=Smtp` uses
`SmtpEmailNotificationSender`. Missing/invalid SMTP configuration and SMTP
delivery errors are logged and fall back to the logging provider without
changing the successful order response.

`POST /api/admin/notifications/test-email` is protected by the `AdminOnly`
policy. It requires enabled email notifications and a Site Settings email, then
uses the currently configured provider and returns a summary containing only
provider/result metadata—never SMTP credentials.

SMTP credentials must come from environment variables, `dotnet user-secrets`,
or an external secret store; they must not be stored in `SiteSettings`, source
control, or committed appsettings files. Configure local SMTP with:

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

Equivalent environment variables use double underscores, for example
`Notifications__Email__Smtp__Password`. WhatsApp and SMS notification channels
are intentionally not implemented.

## Administrator authentication

Authentication uses ASP.NET Core Identity with PostgreSQL-backed users and
roles. `POST /api/auth/login` accepts an email and password and returns a JWT.
`GET /api/auth/me` validates a Bearer token and returns the current user. The
`AdminOnly` policy requires the `Admin` role for Orders read/status/note routes.
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

Available endpoints after startup:

- `/swagger`
- `/api/health`
- `/api/version`
- `/api/auth/login`
- `/api/auth/me`
- `/api/orders`

With the API running, verify the existing endpoints from another PowerShell
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
upload-library management is not implemented. The public Order form, Portfolio,
Services, Settings and Orders admin modules use this backend, while the
remaining site/admin content stays in mock/prototype mode.
Refresh tokens, password
reset, email confirmation, MFA, and production secret rotation are not
implemented.
