# ADR 0011: Testing and Load Testing Strategy

## Status

Accepted

## Context

Phases 1–9 shipped features with domain/application/architecture unit tests. Phase 10 needs API-level confidence and a repeatable load story without a heavy commercial APM suite.

## Decision

- Keep fast unit tests (Domain, Application validators, Architecture rules)
- Add `NexusCRM.Api.Tests` using `WebApplicationFactory` for integration/smoke flows (health, auth, search, reports, customers)
- Database resolution order: `NEXUSCRM_TEST_CONNECTION` → local compose Postgres (`nexuscrm_it`) → Testcontainers when `NEXUSCRM_USE_TESTCONTAINERS=1`
- Skip Api.Tests cleanly when no Postgres is available (SkippableFact)
- Disable OpenTelemetry and background workflow processor in the test host
- Provide k6 scripts under `tests/load` for local performance checks
- CI sets `NEXUSCRM_USE_TESTCONTAINERS=1`

## Consequences

- Local unit tests always run; integration tests run when Postgres/Docker is available
- Load tests are opt-in locally (API must already be running)
- Later: Playwright UI tests, coverage gates, dedicated perf environments
