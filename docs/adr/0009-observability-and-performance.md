# ADR 0009: Observability and Performance

## Status

Accepted

## Context

Phase 1 reserved OTel collector in compose and Serilog at the edge, but traces/metrics were not wired. Search and reports need indexes and short-lived caching under load.

## Decision

- OpenTelemetry traces + metrics (ASP.NET, HttpClient, EF Core, runtime, MediatR `ActivitySource`) exported via OTLP to the local collector; Prometheus scrape at `/metrics` on the API
- Health: `/health/live` (process), `/health/ready` (database, optional Redis), JSON payloads
- Request timing header `X-Response-Time-Ms` + slow-request warnings; response compression
- Serilog enrichers: environment, machine, correlation, tenant/user scopes
- Report queries cached 30s in `IDistributedCache` (memory or Redis)
- EF: retry-on-failure, command timeout; additional lead/deal indexes for search/status

## Consequences

- Local demos: `docker compose` OTel collector logs spans/metrics; scrape `/metrics` without extra UI
- Later: Grafana/Tempo/Prometheus stack, cache invalidation on writes, OpenSearch for search
