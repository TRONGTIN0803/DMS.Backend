# DMS

Phase 1 foundation for a Distribution Management System built with ASP.NET Core, EF Core, PostgreSQL, and Clean Architecture.

## Structure

- `src/DMS.Domain`: entities, enums, domain primitives.
- `src/DMS.Application`: use-case contracts, DTOs, validators, repository/unit-of-work abstractions.
- `src/DMS.Infrastructure`: EF Core PostgreSQL DbContext, Fluent API mappings, seed data, repository implementations.
- `src/DMS.Api`: ASP.NET Core API, Swagger, middleware, DI wiring.
- `src/DMS.Shared`: reusable result and paging primitives.
- `tests`: unit and integration test projects.

## Local database

```powershell
docker compose up -d postgres
```

Default connection string:

```text
Host=localhost;Port=5432;Database=dms;Username=dms_app;Password=dms_password
```

## Expected next commands

```powershell
dotnet restore
dotnet build
dotnet ef migrations add InitialCreate --project src/DMS.Infrastructure --startup-project src/DMS.Api
dotnet ef database update --project src/DMS.Infrastructure --startup-project src/DMS.Api
dotnet run --project src/DMS.Api
```

## Run locally on Windows

PostgreSQL local dev credentials:

```text
Host=localhost
Port=5432
Database=dms
Username=dms_app
Password=dms_password
```

Run the API:

```powershell
.\scripts\run-api.ps1
```

Then open:

- Swagger: http://localhost:5080/swagger
- Health: http://localhost:5080/api/health
- Items API: http://localhost:5080/api/v1/items

Open a database shell:

```powershell
.\scripts\open-db.ps1
```

Quick DB check:

```powershell
.\scripts\check-db.ps1
```
