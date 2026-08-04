# NexusCRM Architecture Overview

## Style

Clean Architecture + selective DDD + selective CQRS inside a modular monolith.

```text
Angular SPA
    │
API / BFF (ASP.NET Core)
    │
Application (commands/queries, validation, behaviors)
    │
Domain (aggregates, domain events, policies)
    │
Infrastructure (EF Core, Redis, RabbitMQ, storage, telemetry)
    │
Worker (async jobs / workflows)
```

## Projects

| Project | Responsibility |
|---------|----------------|
| `NexusCRM.Domain` | Aggregates, value objects, domain events |
| `NexusCRM.Application` | Use cases, ports, MediatR handlers |
| `NexusCRM.Infrastructure` | Adapters: DB, cache, bus, files |
| `NexusCRM.Api` | HTTP edge, versioning, OpenAPI, middleware |
| `NexusCRM.Worker` | Background processing host |
| `NexusCRM.AppHost` | .NET Aspire orchestration (local/dev) |
| `NexusCRM.ServiceDefaults` | Aspire shared defaults (discovery, resilience, OTLP) |
| `NexusCRM.Contracts` | DTOs shared across API boundaries |
| `NexusCRM.Shared` | Result/Error primitives |
| `NexusCRM.Plugins.Abstractions` | Plugin host contracts |
| `NexusCRM.Plugins.Samples` | Demo lead enrichment + notification channel plugins |

## Cross-cutting

- Correlation IDs (`X-Correlation-Id`) + request timing (`X-Response-Time-Ms`)
- Tenant resolution + observability enrichment (tenant/user on spans/logs)
- Problem Details errors
- Rate limiting + response compression
- Health checks (`/health/live`, `/health/ready`)
- Structured logging (Serilog)
- OpenTelemetry traces/metrics → OTLP collector; Prometheus scrape `/metrics`
- Distributed cache for hot report queries
- Aspire AppHost for local orchestration; see [deployment.md](../deployment.md)
