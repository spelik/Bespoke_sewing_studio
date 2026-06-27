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
and `AddIdentityAuth`. All three migrations were applied to the local
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

Physical attachments and email notifications are not implemented. The public
frontend Order form calls anonymous `POST /api/orders`; admin frontend
integration is still a prototype.

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
dotnet user-secrets set "SeedAdmin:Email" "admin@example.test" --project backend/src/BespokeStudio.Api
dotnet user-secrets set "SeedAdmin:Password" "replace-with-a-strong-local-password" --project backend/src/BespokeStudio.Api
dotnet user-secrets set "Jwt:SigningKey" "replace-with-at-least-32-random-characters" --project backend/src/BespokeStudio.Api
```

Environment-variable equivalents for deployment or temporary local use are:

```powershell
$env:SeedAdmin__Email = "admin@example.test"
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
  -Body '{"email":"admin@example.test","password":"replace-with-a-strong-local-password"}'

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

Portfolio, services, and upload CRUD endpoints are not implemented yet. The
public Order form uses this backend, while site content and the admin frontend
remain in mock/prototype mode. Refresh tokens, password reset, email
confirmation, MFA, and production secret rotation are not implemented.
