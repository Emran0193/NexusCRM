# ADR 0005: Configurable Workflow Engine on Domain Events

## Status

Accepted

## Context

Stage transitions are the natural automation points in a CRM. Hardcoded `if stage == X` logic does not scale across tenants or industries.

## Decision

- Model `WorkflowDefinition` (trigger + conditions + actions) and `WorkflowRun` (durable execution queue)
- Dispatch from application handlers after successful stage/score changes
- Evaluate conditions against a string context dictionary
- Execute actions asynchronously via a background processor (API-hosted for local demos; Worker host for production-like separation)

Seeded examples:

1. Lead → Qualified → create follow-up task + notify
2. Deal → Won → notify onboarding

## Consequences

- Extensible without code changes for new tenant rules
- Action surface starts small (notify/task/score/assign) and grows
- RabbitMQ can later replace DB polling without changing the domain model
