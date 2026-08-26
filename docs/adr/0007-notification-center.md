# ADR 0007: In-App Notification Center

## Status

Accepted

## Context

Workflow `LogNotification` actions only wrote logs. Users need durable, in-app alerts with unread state and live delivery alongside board sync.

## Decision

- Persist `AppNotification` rows (tenant-scoped; optional `RecipientUserId`)
- REST: list, mark one read, mark all read
- Publish `NotificationCreated` on the existing `/hubs/board` tenant group
- `INotificationService` used by workflow `LogNotification`; API host uses SignalR publisher, Worker uses no-op (rows still persist)
- Angular shell hosts a notification panel; toast on live create

## Consequences

- Workflow demos surface in the UI within seconds (API inline processor)
- Shared `IsRead` on tenant-wide rows is acceptable for the demo tenancy model; per-user receipts can replace it later
