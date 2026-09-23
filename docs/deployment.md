# NexusCRM Deployment

Three supported local/production paths:

| Path | When to use |
|------|-------------|
| `.\run.ps1` | Classic: host Postgres + Docker Redis/Rabbit/OTel + API + Angular |
| `.\run.ps1 -Aspire` | Aspire AppHost + dashboard + Angular (`.\stop.ps1` stops both modes) |
| Docker Compose app stack | Containerized API + SPA + infra demo |

## Prerequisites

- .NET 10 SDK
- Node.js 20+ (Angular)
- Docker Desktop
- For Aspire: `dotnet workload` / Aspire templates (`Aspire.ProjectTemplates`) — AppHost SDK `13.6.1`

## 1. Scripts

```powershell
.\run.ps1                 # classic stack
.\run.ps1 -Aspire         # Aspire AppHost + dashboard + Angular
.\run.ps1 -Aspire -SkipWeb
.\stop.ps1                # stops classic or Aspire (from .run/pids.json)
.\stop.ps1 -KeepDocker
```

### Classic (`.\run.ps1`)

Host PostgreSQL `postgres`/`postgres` on `localhost:5432` (database `nexuscrm`). Docker Compose brings up Redis, RabbitMQ, and the OTel collector only.

### Aspire (`.\run.ps1 -Aspire`)

Starts `NexusCRM.AppHost` (tracked in `.run/pids.json`) and optionally Angular.

- **Dashboard:** https://localhost:17117
- AppHost resources: Postgres (**5433**), Redis, RabbitMQ, Api, Worker
- Connection strings / OTLP injected by Aspire (`OpenTelemetry__Enabled=false` under AppHost)
- SPA: http://localhost:4200 — set `environment.ts` to the `api` HTTPS URL from the dashboard if it is not `https://localhost:7148`

OpenAPI (Development): `/openapi/v1.json` on the API resource.

Manual equivalent:

```powershell
dotnet run --project src/NexusCRM.AppHost
```

## 3. Docker Compose (API + SPA)

From repo root:

```powershell
$env:JWT_SIGNING_KEY = "replace-with-a-long-random-secret-key-32+"
docker compose `
  -f deploy/docker-compose.yml `
  -f deploy/docker-compose.app.yml `
  --profile docker-db `
  up -d --build
```

| Service | URL |
|---------|-----|
| SPA | http://localhost:8088 |
| API | http://localhost:8080 |
| OpenAPI | only when `ASPNETCORE_ENVIRONMENT=Development` |
| RabbitMQ UI | http://localhost:15672 (`nexus`/`nexus`) |
| OTLP | `localhost:4317` |

Health: `GET http://localhost:8080/health/live`, `GET /health/ready`.

Stop:

```powershell
docker compose -f deploy/docker-compose.yml -f deploy/docker-compose.app.yml --profile docker-db down
```

## Configuration checklist (production)

- [ ] Strong `Jwt:SigningKey` (≥32 chars) via secrets/env — never commit real keys
- [ ] `Cors:Origins` limited to real SPA origins
- [ ] `Security:ExposeMetricsEndpoint` only if scrapers are network-isolated
- [ ] TLS terminated at reverse proxy; keep `ForwardedHeaders` enabled
- [ ] Postgres credentials rotated; do not use compose defaults
- [ ] Migrations: API applies EF migrations on Development startup; for Production run `dotnet ef database update` (or an init job) explicitly

## CI

GitHub Actions (`.github/workflows/ci.yml`) restores/builds/tests the .NET solution (Testcontainers for Api.Tests) and builds the Angular app. Aspire AppHost is built as part of `dotnet build NexusCRM.slnx`.
