# NexusCRM Roadmap

## Phase 1 — Architecture + Engineering Foundation ✅
## Phase 2 — Identity + Multi-tenancy ✅
## Phase 3 — CRM Core ✅

## Phase 4 — Angular Enterprise UI ✅

- [x] Design tokens + distinctive typography
- [x] Shared UI primitives (empty/loading/toast)
- [x] Shell polish + active nav + a11y basics
- [x] Optimistic lead/deal board moves with rollback

## Phase 5 — Workflow + Events ✅

- [x] Workflow definitions (trigger/conditions/actions)
- [x] Dispatch on lead/deal stage (+ score) transitions
- [x] Durable workflow runs + background execution
- [x] Workflows admin UI + recent runs
- [x] Seeded Qualified-lead and Won-deal automations

## Phase 6 — Real-time + Notifications ✅

- [x] SignalR board hub with JWT + tenant groups
- [x] Publish lead/deal create & move events
- [x] Angular live board sync + connection indicator
- [x] In-app notification center (persist + SignalR + shell panel)

## Phase 7 — Search + Reporting ✅

- [x] Global search API (customers / leads / deals, permission-filtered)
- [x] CRM summary + pipeline funnel reports (`reports.read`)
- [x] Angular Search + Reports pages
- [x] Demo CRM seed for empty tenants

## Phase 8 — Observability + Performance ✅

- [x] OpenTelemetry traces/metrics (OTLP + Prometheus `/metrics`)
- [x] Live/ready health JSON probes
- [x] Request timing, compression, Serilog enrichment
- [x] Report response caching + EF retry + search indexes

## Phase 9 — Security Hardening ✅

- [x] Security headers, HSTS, forwarded headers, request size limit
- [x] JWT startup validation + shorter access tokens
- [x] Configurable lockout, refresh session cap, timing-safe login
- [x] Auth rate limits, CORS allow-list, metrics exposure guard
- [x] Password policy helper + domain tests

## Phase 10 — Testing + Load Testing ✅

- [x] Expanded application validator tests
- [x] API integration tests (Testcontainers PostgreSQL)
- [x] k6 smoke + load scripts
- [x] CI runs full `dotnet test` suite

## Phase 11 — Plugin / Extension System ✅

- [x] Plugin abstractions + sample enrichment/notification channel plugins
- [x] Tenant enable/disable settings + catalog/runtime host
- [x] Optional external assembly probe directory
- [x] API + Angular Plugins admin page (`plugins.read` / `plugins.manage`)

## Phase 12 — Deployment + Documentation ✅

- [x] .NET Aspire AppHost (Postgres/Redis/RabbitMQ + Api + Worker)
- [x] ServiceDefaults (discovery, resilience, Aspire OTLP)
- [x] Dockerfiles + compose app stack for API/SPA
- [x] Deployment guide + ADR 0013

## Later

Operational hardening (K8s manifests, signed plugin packages, Playwright UI gates) as needed.
