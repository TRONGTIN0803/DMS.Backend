# DMS.Backend

Phase 1 foundation for a Distribution Management System built with ASP.NET Core, EF Core, PostgreSQL, and Clean Architecture.

## Structure

- `src/DMS.Domain`: entities, enums, domain primitives.
- `src/DMS.Application`: use-case contracts, DTOs, validators, repository/unit-of-work abstractions.
- `src/DMS.Infrastructure`: EF Core PostgreSQL DbContext, Fluent API mappings, seed data, repository implementations.
- `src/DMS.Api`: ASP.NET Core API, Swagger, middleware, DI wiring.
- `src/DMS.Shared`: reusable result and paging primitives.
- `tests`: unit and integration test projects.
- `docs/phase1_erd.dbml`: Phase 1 ERD source for dbdiagram.io.

## Local database

```powershell
docker compose up -d postgres
```

Default connection string format:

```text
Host=localhost;Port=5432;Database=<database-name>;Username=<database-user>;Password=<database-password>
```

Store the local connection string in user-secrets:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=dms;Username=dms_app;Password=dms_password" --project src\DMS.Api
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

PostgreSQL local dev connection values:

```text
Host=localhost
Port=5432
Database=<database-name>
Username=<database-user>
Password=<database-password>
```

The API expects `ConnectionStrings:DefaultConnection` from user-secrets or environment variables. The committed `appsettings.json` intentionally does not contain a database password.

Run the API:

```powershell
.\scripts\run-api.ps1
```

Then open:

- Swagger: http://localhost:5080/swagger
- Health: http://localhost:5080/api/health
- Items API: http://localhost:5080/api/v1/items

Development login:

```http
POST /api/v1/auth/login
Content-Type: application/json

{
  "userName": "admin",
  "password": "Admin@12345"
}
```

Copy the returned `accessToken` into Swagger's `Authorize` dialog as a Bearer token before calling protected CRUD APIs.

Refresh an expired access token:

```http
POST /api/v1/auth/refresh
Content-Type: application/json

{
  "refreshToken": "<refreshToken from login response>"
}
```

Open a database shell:

```powershell
.\scripts\open-db.ps1
```

Quick DB check:

```powershell
.\scripts\check-db.ps1
```

## Tests

Run unit tests:

```powershell
dotnet test tests\DMS.UnitTests\DMS.UnitTests.csproj
```

Integration tests use a real PostgreSQL database. By default they connect to:

```text
Host=localhost;Port=5432;Database=dms_test;Username=dms_app;Password=dms_password
```

Create the test database once:

```powershell
$env:PGPASSWORD='postgres'
& 'C:\Program Files\PostgreSQL\16\bin\createdb.exe' -h localhost -p 5432 -U postgres -O dms_app dms_test
```

Or override the connection string:

```powershell
$env:DMS_TEST_CONNECTION='Host=localhost;Port=5432;Database=dms_test;Username=dms_app;Password=dms_password'
dotnet test tests\DMS.IntegrationTests\DMS.IntegrationTests.csproj
```
