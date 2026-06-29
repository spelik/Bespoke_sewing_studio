
# Bespoke Sewing Studio frontend

React, Vite and TypeScript frontend for the Bespoke Sewing Studio website. The current UI was exported from Figma Make and is being stabilised incrementally with an ASP.NET Core backend. The product is English-only; EN/UA language switching and multilingual CMS are not part of the current scope.

## Frontend data mode

The public site is backend-first and CMS-driven: contact settings, brand/navigation/SEO,
page content, repeatable content, services/prices, portfolio data and Contact form submissions load from or write to the ASP.NET Core API.
Centralised typed frontend defaults are used only when the corresponding public API
cannot be reached. The public Order form sends real requests to
`POST /api/orders`, which persists enquiries in PostgreSQL through the ASP.NET
Core backend. The public Contact form sends real requests to
`POST /api/contact-messages`, which stores messages in PostgreSQL and can notify
the owner by email. The admin login, Orders and Contact Messages screens also use the backend API; the
admin Services, Portfolio, Content, Repeatable Content, Settings and Brand/SEO sections use protected backend APIs. Optional order
attachments are uploaded first and linked to the created enquiry by ID.

The UI is English-only. Header and mobile language switchers have been removed, and typed fallback/default content should remain English-only.

API configuration lives in `src/config/appConfig.ts`. `VITE_API_BASE_URL`
defaults to `http://localhost:5099/api` for local development. Copy
`.env.example` to `.env.local` when an explicit override is needed; `.env.local`
is ignored by Git.

## Backend

The backend lives in `backend/` as a separate ASP.NET Core Web API skeleton (`net10.0`).

Current backend status:

- Swagger/OpenAPI is enabled
- `/api/health` and `/api/version` are available
- EF Core persistence is configured for local PostgreSQL development
- migrations are applied explicitly with `dotnet ef database update`
- Orders/enquiries API now persists data in PostgreSQL
- ASP.NET Core Identity + JWT Bearer protects Orders administration routes
- `/api/auth/login` and `/api/auth/me` provide the backend authentication foundation
- the public Order form calls `POST /api/orders`
- the public Contact form calls `POST /api/contact-messages` and persists messages in PostgreSQL
- the public Order form accepts JPG, PNG, WebP and PDF attachments up to 5 MB each
- attachment metadata, including upload scan status, is stored in PostgreSQL; development files are stored under `backend/storage/uploads`
- public upload, order creation and Contact form endpoints use configurable per-IP rate limits
- administrators can manually remove expired orphan uploads through a protected cleanup endpoint
- public contact, social and footer settings load from `GET /api/site-settings/public`
- the Admin **Settings** section edits public contact and notification settings
- the Admin **Contact Messages** section lists Contact form messages and manages their workflow status
- public services/prices load from `GET /api/services`
- the Admin **Services** section creates, edits, hides, features, deletes or archives services and price options
- public portfolio/gallery data loads from `GET /api/portfolio`
- the Admin **Portfolio** section manages categories, work items, publication state, order and featured items
- portfolio images are uploaded to local development storage and served publicly only while linked to an active portfolio item
- page headings, body text, CTAs and key page images load from the Website Content CMS
- repeatable public blocks such as process steps, studio values, testimonials and privacy subsections load from the Repeatable Content CMS
- logo, favicon, default SEO metadata, header CTA and navigation labels/visibility are managed in Admin **Brand / SEO**
- the public Order form submits a dynamic `serviceOfferingId` while preserving legacy enum compatibility
- admin login, Orders list/detail/status/notes and Contact Messages list/detail/status use protected backend endpoints
- the admin sidebar exposes only backend-backed Orders, Contact Messages, Services, Portfolio, Content, Repeatable Content, Brand/SEO and Settings modules

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
Invoke-WebRequest http://localhost:5099/swagger/index.html -UseBasicParsing
```

If PostgreSQL is not needed for the current session, the API can still start
and serve these system endpoints because they do not execute database queries.

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
`http://127.0.0.1:5173/admin/login`. The frontend stores only the JWT access
token in `sessionStorage`; signing out clears it. Passwords are never stored by
the frontend.

Start the frontend in a second PowerShell window:

```powershell
npm.cmd run dev -- --host 127.0.0.1
```

The backend must be available at the configured `VITE_API_BASE_URL` before an
Order form submission or admin sign-in. Select up to five files in the public
Order form; after submission, open the enquiry in `/admin` to download its
protected attachments. `backend/storage/` is ignored by Git.

Public `POST /api/uploads/order-attachments` requests are limited to 10 per 10
minutes per IP, public `POST /api/orders` requests to 5 per 10 minutes per IP,
and public `POST /api/contact-messages` requests to 5 per 10 minutes per IP by default. A rejected request returns `429` and a `Retry-After` header; the
Order form displays the API message without exposing server details.

An upload that is not linked to an order and is older than the configured
`UploadStorage:OrphanCleanupAgeMinutes` TTL (120 minutes by default) can be
removed by an administrator through `POST /api/uploads/cleanup-orphans` using
an Admin JWT. Cleanup is manual at this stage. Local uploads are written to a
quarantine folder first, validated by file signature, optionally scanned through
configured ClamAV/command-line malware scanning, and moved to final storage only
when accepted. The default development scanner provider is `Disabled`; production
must configure ClamAV before accepting uploads. Production object storage is not
implemented.

## Services and prices

Sign in at `http://127.0.0.1:5173/admin`, then select **Services**. The owner can
create and edit services, add text-based price options, control display order,
mark services Featured for the Home preview, and hide inactive services. Public
Home/Services pages and the Order form read the active list from PostgreSQL via
`GET /api/services`; typed fallback services keep the public UI available if the
API is offline.

Deleting an unused service removes it. A service referenced by an existing
order is archived and hidden from new enquiries instead, while the order keeps
its stored service-name snapshot. Service image upload and drag-and-drop order
editing are not implemented.

## Site settings

Sign in at `http://127.0.0.1:5173/admin`, then select **Settings** in the
sidebar. The administrator edits one email and one contact phone, plus
contact/footer text, service area, social URLs, and the email notification
toggle. The email is shown on the public site and is also the owner notification
destination. The phone is public contact information only.

Enable owner new-request notifications with **Notify me about new requests** in
Admin Settings. The default development provider writes email content to the
backend log. The **Email delivery** block in Admin Settings can either keep
developer-managed configuration (`Configuration`) or use owner-managed `Gmail
SMTP`. In Gmail SMTP mode the owner enters a Gmail address and Google App
Password; the password is protected on the backend, never returned by the API,
and can be replaced or cleared from the admin UI.

Customer confirmation emails are separate from owner notifications. The
**Customer confirmations** block in Admin Settings has its own toggle and
plain-text subject/body templates for Order and Contact confirmations. Supported
placeholders include `{{studioName}}`, `{{customerName}}`, `{{customerEmail}}`,
`{{customerPhone}}`, plus Order-only `{{serviceName}}`, `{{preferredDate}}`,
and Contact-only `{{messageSubject}}`. WhatsApp and SMS notifications are not implemented or
planned for the current product scope. Public pages keep their typed fallback
content if the API cannot be reached.


## Contact messages

The public Contact page sends real enquiries to `POST /api/contact-messages`.
The backend validates name, email, optional phone, optional subject, message and
consent, stores the message in PostgreSQL, and returns `201 Created`. The form
shows loading, success and validation/API error states and clears after a
successful submission.

Sign in at `http://127.0.0.1:5173/admin`, then select **Contact Messages** to
view Contact form messages, filter by status and update the workflow status:
`New`, `Read`, `Replied` or `Archived`. New contact messages use the same owner
email notification foundation as Orders when email notifications are enabled in
Site Settings. In development the default logging provider writes the email
content to the backend log; SMTP can be configured through user-secrets or
environment variables.



## Production email / SMTP checklist

Owner notifications for Orders and Contact Messages are already implemented, but
local development uses `Provider=Logging` by default. Real email delivery is a
mandatory production setup item and must be configured outside source control.

Before production release:

- choose one delivery mode:
  - developer-managed `Notifications:Email:Provider=Smtp` through user-secrets,
    environment variables or a secret store
  - owner-managed **Admin > Settings > Email delivery > Gmail SMTP**
- keep raw SMTP credentials out of Git, committed `appsettings*.json`, docs and
  screenshots
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
- before production, configure deliverability records and operations:
  SPF, DKIM, DMARC, bounce/rejection monitoring, background retry/queueing and
  credential rotation

Example local Gmail SMTP setup uses user-secrets only:

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

## Brand and SEO settings

Sign in to Admin and select **Brand / SEO** to upload a JPG, PNG or WebP logo,
favicon or default Open Graph image and edit the brand name, logo alt text,
header CTA, default title/description and navigation labels/visibility. The
public header, footer and document metadata load these settings from the
backend. If the backend is unavailable, the bundled logo and typed defaults
keep the public site usable. SVG upload is intentionally disabled.

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
  
