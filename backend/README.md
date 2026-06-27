# Bespoke Sewing Studio backend

ASP.NET Core Web API skeleton for the Bespoke Sewing Studio project.

Current status:

- backend is a skeleton only
- domain model and application contract drafts are in place
- EF Core persistence is configured for PostgreSQL
- the `InitialCreate` migration is included but has not been applied
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

The repository currently contains the `InitialCreate` migration. It was
generated without connecting to PostgreSQL and has not been applied by this
task. Installing the matching CLI tool, if it is missing locally:

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

Orders, clients, portfolio, services, and upload CRUD endpoints are not
implemented yet. The frontend remains in mock/prototype mode and does not call
this backend.
