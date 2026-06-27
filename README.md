
# Bespoke Sewing Studio frontend

React, Vite and TypeScript frontend for the Bespoke Sewing Studio website. The current UI was exported from Figma Make and is being stabilised incrementally with an ASP.NET Core backend.

## Frontend data mode

Site content and the remaining prototype features still use typed mock data from
`src/api/`. The public Order form now sends real requests to
`POST /api/orders`, which persists enquiries in PostgreSQL through the ASP.NET
Core backend.

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
- site content and the admin prototype still use mock/prototype data

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

Start the frontend in a second PowerShell window:

```powershell
npm.cmd run dev -- --host 127.0.0.1
```

The backend must be available at the configured `VITE_API_BASE_URL` before an
Order form submission. No frontend upload or admin API integration is enabled.

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
  
