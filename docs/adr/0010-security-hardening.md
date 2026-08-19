# ADR 0010: Security Hardening

## Status

Accepted

## Context

Identity (ADR 0003) already provides JWT + refresh rotation, lockout fields, MFA, and permission policies. Phase 9 hardens the edge and closes operational gaps for portfolio/production readiness.

## Decision

- Security headers middleware (nosniff, frame deny, referrer, COOP/CORP, API CSP)
- HSTS outside Development; forwarded headers for correct client IP
- Startup validation of JWT signing key (length + reject obvious placeholders outside Development)
- Shorter access tokens (15m); configurable lockout, session cap, refresh lifetime via `Security:*`
- Auth endpoints rate-limited (10/min/IP); health/metrics excluded from global limiter
- Timing-equalized failed login for unknown emails; refresh reuse detection retained
- Fallback authorization policy: authenticated by default; anonymous only on health/OpenAPI/metrics (dev)
- Metrics scrape disabled outside Development unless `Security:ExposeMetricsEndpoint=true`
- Password policy helper for future set-password flows; CORS allow-list tightened
- 1 MB max request body

## Consequences

- Safer defaults without an external IdP
- Production must supply a real `Jwt:SigningKey` (user-secrets / env / vault)
- SPA remains on a separate origin; browser CSP for the Angular host is separate from the API CSP
