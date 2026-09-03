# ADR 0008: Search and Reporting

## Status

Accepted

## Context

Users need cross-entity discovery and lightweight pipeline KPIs without a separate BI stack.

## Decision

- Global search via `GET /api/v1/search?q=` over customers, leads, and deals (ILIKE-style contains), filtered by the caller’s module read permissions
- Reporting via `ICrmAnalyticsReader` aggregating EF queries:
  - `GET /api/v1/reports/summary` — tenant KPIs
  - `GET /api/v1/reports/pipelines` — stage funnels for default lead/deal pipelines
- Gate reports with `reports.read`; no new OLAP store in Phase 7
- Angular Search (debounced) and Reports pages; demo CRM seed for empty tenants

## Consequences

- Fast to ship and demo; acceptable at portfolio scale
- Later: PostgreSQL full-text / OpenSearch, materialized report views, export
