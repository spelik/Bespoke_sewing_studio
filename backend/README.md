# Bespoke Sewing Studio backend

ASP.NET Core Web API skeleton for the Bespoke Sewing Studio project.

Current status:

- backend is a skeleton only
- no PostgreSQL connection yet
- no EF Core migrations yet
- no authentication or admin login yet
- no real orders/enquiries API yet

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
