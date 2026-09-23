# ADR 0013: Aspire Orchestration + Container Deployment

## Status

Accepted

## Context

Phases 1–11 delivered a modular CRM with Docker Compose for infra only and `run.ps1` for local processes. Phase 12 needs a first-class orchestration story, OpenAPI-aware API hosting under Aspire, and repeatable container packaging without abandoning the existing compose/scripts path.

## Decision

- Add `NexusCRM.AppHost` (.NET Aspire 13) to compose Postgres/Redis/RabbitMQ + Api + Worker with connection-string injection and dashboard OTLP
- Add `NexusCRM.ServiceDefaults` for service discovery, HttpClient resilience, and Aspire OTLP exporters
- Keep existing Nexus OpenTelemetry/Prometheus path when `OpenTelemetry:Enabled=true` (scripts/compose); disable it under AppHost to avoid double exporters
- Keep Microsoft OpenAPI (`MapOpenApi` + API versioning) on the API — Aspire does not replace OpenAPI
- Ship `deploy/Dockerfile.api`, `deploy/Dockerfile.web`, `deploy/nginx.conf`, and `deploy/docker-compose.app.yml` for a full container demo
- Document all three paths in `docs/deployment.md`

## Consequences

- Developers can use Aspire dashboard or continue with `run.ps1`
- Angular is not hosted inside AppHost (Node/Aspire JS hosting is optional later); SPA still runs via npm or the web container
- Production should supply secrets via environment/secret store; compose defaults are demo-only
