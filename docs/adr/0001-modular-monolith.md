# ADR 0001: Modular Monolith First

## Status

Accepted

## Context

NexusCRM aims to demonstrate enterprise architecture without premature microservice complexity. Splitting every module into a service early often increases operational cost without proving domain boundaries.

## Decision

Start as a **modular monolith** with clear module boundaries (Identity, Customers, Leads, Deals, Workflows, Notifications). Selected modules may later be extracted into services when scaling or team ownership requires it.

## Consequences

- Faster local development and simpler transactions initially
- Stronger pressure to keep module boundaries clean
- Extraction path remains open via messaging and contracts
