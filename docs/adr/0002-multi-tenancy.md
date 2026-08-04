# ADR 0002: Shared Database Multi-Tenancy with Row Isolation

## Status

Accepted

## Context

The platform must serve many businesses from one deployment while guaranteeing Tenant A cannot access Tenant B data.

## Decision

Use a **shared database, shared schema** model with:

1. Tenant resolution at the request edge
2. `ITenantScoped` on tenant-owned aggregates
3. EF Core global query filters
4. Explicit application-layer tenant checks for sensitive operations

Dedicated databases per tenant remain a future option for enterprise isolation tiers.

## Consequences

- Lower operational complexity for demos and early tenants
- Query filters must never be bypassed carelessly
- Strong test coverage required for isolation
