# ADR 0006: SignalR Tenant Board Sync

## Status

Accepted

## Context

Sales teams need concurrent Kanban updates without page refresh. Polling is chatty and laggy.

## Decision

- Expose `/hubs/board` SignalR hub, authorized via JWT (`access_token` query for WebSockets)
- Connections join `tenant:{tenantId}:boards` from JWT tenant claim
- Application handlers publish through `IBoardRealtimePublisher`
- API host uses `IHubContext<BoardHub>`; Worker uses no-op publisher
- Angular boards subscribe and upsert cards in Signals; show live connection state

## Consequences

- Near real-time collaboration on leads/deals boards
- Tenant isolation enforced by hub groups
- Same hub later carries `NotificationCreated` (see ADR 0007)
- Later: Redis backplane for multi-instance API scale-out
