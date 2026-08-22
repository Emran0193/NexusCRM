# ADR 0004: Configuration-Driven Lead/Deal Pipelines

## Status

Accepted

## Context

Hardcoded lead/deal stages prevent the CRM from adapting across industries.

## Decision

Model `Pipeline` + `PipelineStage` per tenant and pipeline type (`Lead` | `Deal`). Leads and deals store `StageId` and transition through domain methods that interpret stage flags (`IsWon` / `IsLost`).

Default pipelines are seeded for development; tenants can later customize stages without code changes.

## Consequences

- Boards are driven by configuration, not enums
- Workflow automation (Phase 5) can key off stage transitions
- UI must load stages dynamically
