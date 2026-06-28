
# Bespoke Sewing Studio frontend

React, Vite and TypeScript frontend for the Bespoke Sewing Studio website. The current UI was exported from Figma Make and is being stabilised incrementally with an ASP.NET Core backend.

## Frontend data mode

The public site is backend-first and CMS-driven: contact settings, brand/navigation/SEO,
page content, services/prices and portfolio data load from the ASP.NET Core API.
Centralised typed frontend defaults are used only when the corresponding public API
cannot be reached. The public Order form sends real requests to
`POST /api/orders`, which persists enquiries in PostgreSQL through the ASP.NET
Core backend. The admin login and Orders screens also use the backend API; the
admin Services, Portfolio, Content, Settings and Brand/SEO sections use protected backend APIs. Optional order
attachments are uploaded first and linked to the created enquiry by ID.

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
- the public Order form accepts JPG, PNG, WebP and PDF attachments up to 5 MB each
- attachment metadata is stored in PostgreSQL; development files are stored under `backend/storage/uploads`
- public upload and order creation endpoints use configurable per-IP rate limits
- administrators can manually remove expired orphan uploads through a protected cleanup endpoint
- public contact, social and footer settings load from `GET /api/site-settings/public`
- the Admin **Settings** section edits public contact and notification settings
- public services/prices load from `GET /api/services`
- the Admin **Services** section creates, edits, hides, features, deletes or archives services and price options
- public portfolio/gallery data loads from `GET /api/portfolio`
- the Admin **Portfolio** section manages categories, work items, publication state, order and featured items
- portfolio images are uploaded to local development storage and served publicly only while linked to an active portfolio item
- page headings, body text, CTAs and key page images load from the Website Content CMS
- logo, favicon, default SEO metadata, header CTA and navigation labels/visibility are managed in Admin **Brand / SEO**
- the public Order form submits a dynamic `serviceOfferingId` while preserving legacy enum compatibility
- admin login and Orders list/detail/status/notes use protected backend endpoints
- the admin sidebar exposes only backend-backed Orders, Services, Portfolio, Content, Brand/SEO and Settings modules

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
minutes per IP, and public `POST /api/orders` requests to 5 per 10 minutes per
IP by default. A rejected request returns `429` and a `Retry-After` header; the
Order form displays the API message without exposing server details.

An upload that is not linked to an order and is older than the configured
`UploadStorage:OrphanCleanupAgeMinutes` TTL (120 minutes by default) can be
removed by an administrator through `POST /api/uploads/cleanup-orphans` using
an Admin JWT. Cleanup is manual at this stage. Production object storage and
antivirus/deep-content scanning are not implemented.

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

Enable new-enquiry email notifications with the toggle in Admin Settings. The
default development provider writes email content to the backend log; SMTP can
be configured through user-secrets or environment variables. WhatsApp and SMS
notifications are not implemented or planned for the current product scope.
Public pages keep their typed fallback content if the API cannot be reached.

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
Typed frontend defaults remain available when the backend cannot be reached.
Content images are stored locally under `backend/storage/uploads/content-images`
in development. The existing logo remains a bundled frontend fallback; logo
upload is not part of this module.

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
  
