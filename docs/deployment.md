# Deployment

The API is an ASP.NET Core service and should be deployed to a .NET-capable host such as Render, Railway, Fly.io, Azure App Service, or a Docker VPS.

## Render blueprint

This repo includes `render.yaml` and `Dockerfile`.

1. Create a Render Blueprint from the GitHub repository.
2. Select branch `feature/phase2-order-workflow`.
3. Render creates:
   - Web service: `dms-backend-api`
   - PostgreSQL database: `dms-postgres`
4. After deployment, save the public API URL below.

```text
API_BASE_URL=
```

Health check:

```text
{API_BASE_URL}/api/health
```

Swagger:

```text
{API_BASE_URL}/swagger
```
