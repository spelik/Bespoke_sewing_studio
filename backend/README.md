# Bespoke Sewing Studio backend

ASP.NET Core Web API skeleton for the Bespoke Sewing Studio project.

Current status:

- backend is a skeleton only
- domain model and application contract drafts are in place
- no PostgreSQL connection yet
- no EF Core migrations yet
- no authentication or admin login yet
- no real orders/enquiries API yet

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
