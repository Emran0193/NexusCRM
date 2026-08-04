# NexusCRM

Multi-tenant, modular CRM **platform** — a portfolio-grade reference implementation in modern .NET and Angular.

> Build the core once; configure the business instead of rewriting the application.

## Goals

- Modular monolith with extractable boundaries
- Multi-tenant isolation
- Clean Architecture + selective DDD/CQRS
- Configurable workflows, dynamic fields, plugins (phased)
- Observability, security, testing, and performance as first-class concerns

## Solution layout

```text
src/
  NexusCRM.Api              HTTP edge / BFF
  NexusCRM.Application      Use cases, validation, behaviors
  NexusCRM.Domain           Aggregates & domain events
  NexusCRM.Infrastructure   EF Core, Redis, storage, messaging
  NexusCRM.Contracts        API DTOs
  NexusCRM.Shared           Result / Error primitives
  NexusCRM.Plugins.Abstractions  Plugin contracts
  NexusCRM.Plugins.Samples  Demo enrichment / notification plugins
  NexusCRM.ServiceDefaults  Aspire shared defaults
  NexusCRM.AppHost          Aspire orchestration
  NexusCRM.Worker           Background jobs host
  NexusCRM.Web              Angular SPA (feature-based)
tests/
  NexusCRM.Domain.Tests
  NexusCRM.Application.Tests
  NexusCRM.Architecture.Tests
docs/
  adr/
  architecture/
  deployment.md
deploy/
  docker-compose.yml
  docker-compose.app.yml
  Dockerfile.api
  Dockerfile.web
```

## Prerequisites

- .NET 10 SDK
- Node.js 20+ / npm
- Docker Desktop (PostgreSQL, Redis, RabbitMQ, OTel collector)

## Quick start

```powershell
# Classic: Docker infra + build + API + Angular
.\run.ps1

# Aspire: AppHost dashboard + wired Postgres/Redis/Rabbit/Api/Worker + Angular
.\run.ps1 -Aspire
# Dashboard: https://localhost:17117

# Stop whichever mode is running
.\stop.ps1
# .\stop.ps1 -KeepDocker
```

See [docs/deployment.md](docs/deployment.md) for Aspire, Compose app stack, and production checklist.

Or manually:

```bash
# Infrastructure
docker compose -f deploy/docker-compose.yml up -d

# API
dotnet run --project src/NexusCRM.Api

# Angular SPA
cd src/NexusCRM.Web
npm start
```

Health: `GET /health` (ready), `GET /health/live`, `GET /health/ready`  
Metrics: `GET /metrics` (Prometheus)  
OpenAPI (dev): `/openapi/v1.json`  
OTel: collector on `localhost:4317` via `deploy/docker-compose.yml`

### Demo login (seeded in Development)

| Field | Value |
|-------|-------|
| Email | `admin@nexuscrm.local` |
| Password | `ChangeMe!12345` |
| Tenant | `00000000-0000-0000-0000-000000000001` |

Auth endpoints:

```http
POST /api/v1/auth/login
POST /api/v1/auth/refresh
POST /api/v1/auth/logout
GET  /api/v1/auth/me
```

Protected APIs require bearer tokens with module permissions (`customers.*`, `leads.*`, `deals.*`).

### Security notes

- Set a strong `Jwt:SigningKey` (≥32 chars) via user-secrets or environment variables before production
- Auth endpoints are rate-limited; accounts lock after repeated failures (`Security:*`)
- `/metrics` is Development-only unless `Security:ExposeMetricsEndpoint=true`
- API responses include standard security headers; SPA sets `referrer=no-referrer`

### CRM Core endpoints

```http
GET/POST /api/v1/customers
GET/PATCH /api/v1/customers/{id}
POST /api/v1/customers/{id}/contacts|notes|tags

GET  /api/v1/leads/board
POST /api/v1/leads
POST /api/v1/leads/{id}/move|score

GET  /api/v1/deals/board
POST /api/v1/deals
POST /api/v1/deals/{id}/move

GET  /api/v1/workflows
GET  /api/v1/workflows/runs
POST /api/v1/workflows/{id}/toggle

GET  /api/v1/notifications
POST /api/v1/notifications/{id}/read
POST /api/v1/notifications/read-all

GET  /api/v1/search?q=
GET  /api/v1/reports/summary
GET  /api/v1/reports/pipelines

GET  /api/v1/plugins
POST /api/v1/plugins/{pluginId}/toggle
```

### Plugins

1. Sign in (re-login if upgrading — needs `plugins.read` / `plugins.manage`)
2. Open **Plugins** — sample enrichers and the console notification channel are listed
3. Create a lead with a corporate email (or move one to **Qualified**) to see enrichment tags/notes
4. Trigger a workflow notification — the console channel logs delivery when enabled
5. Optional: drop plugin DLLs into `Plugins:ExternalDirectory`

### Search + reports

1. Sign in (re-login if upgrading — needs `reports.read`)
2. Open **Reports** for KPIs and lead/deal funnels (demo seed populates sample data on first run)
3. Open **Search** and type `Acme` or `Northwind`

### Workflow demo

1. Sign in and open **Leads**
2. Create a lead, drag it to **Qualified**
3. Open **Workflows** — a run should appear for “Qualified lead follow-up”
4. Watch the shell **notification** bell — unread badge + toast when the workflow notifies
5. Drag a deal to **Won** to fire the won-deal notification workflow

Workflow runs are processed by a background service inside the API (and optionally `NexusCRM.Worker`).

### Real-time boards (SignalR)

Hub: `/hubs/board` (JWT via `accessTokenFactory` / `access_token` query)

Events: `LeadBoardChanged`, `DealBoardChanged`, `NotificationCreated`

Open **Leads** or **Deals** in two browsers — create or drag a card in one; the other updates live (`● Live` indicator).

## Development phases

| Phase | Focus |
|------:|-------|
| 1 | Architecture + engineering foundation |
| 2 | Identity + multi-tenancy |
| 3 | CRM core (customers, leads, deals) |
| 4 | Angular enterprise UI |
| 5 | Workflow + events |
| 6 | Real-time + notifications |
| 7 | Search + reporting |
| 8 | Observability + performance |
| 9 | Security hardening |
| 10 | Testing + load testing |
| 11 | Plugin / extension system ✅ |
| 12 | Deployment + documentation ✅ |

### Tests

```bash
dotnet test NexusCRM.slnx
# Unit tests always run. Api.Tests need Postgres:
#   docker compose -f deploy/docker-compose.yml up -d postgres
#   or: NEXUSCRM_USE_TESTCONTAINERS=1 (Docker required)
#   or: NEXUSCRM_TEST_CONNECTION="Host=..."

# Load tests (API must be running) — see tests/load/README.md
k6 run -e BASE_URL=https://localhost:7148 -e INSECURE=1 tests/load/smoke.js
```

## Docs

- [Architecture overview](docs/architecture/overview.md)
- [ADR 0001: Modular monolith](docs/adr/0001-modular-monolith.md)
- [ADR 0002: Multi-tenancy](docs/adr/0002-multi-tenancy.md)
- [ADR 0011: Testing strategy](docs/adr/0011-testing-strategy.md)
- [ADR 0012: Plugin extension system](docs/adr/0012-plugin-extension-system.md)
- [ADR 0013: Aspire and deployment](docs/adr/0013-aspire-and-deployment.md)
- [Deployment guide](docs/deployment.md)

## License

Private portfolio project — all rights reserved unless otherwise stated.
