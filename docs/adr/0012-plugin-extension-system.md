# ADR 0012: Plugin / Extension System

## Status

Accepted

## Context

NexusCRM needs optional tenant-scoped extensions (enrichment, outbound channels) without forking the core product. Earlier phases already use Clean Architecture ports; Phase 11 adds a first-class plugin boundary.

## Decision

- Ship `NexusCRM.Plugins.Abstractions` with `INexusPlugin` plus capability interfaces (`ILeadEnrichmentPlugin`, `INotificationChannelPlugin`)
- Provide demo implementations in `NexusCRM.Plugins.Samples` (DI-registered when `Plugins:LoadSamples=true`)
- Optionally probe `Plugins:ExternalDirectory` for assemblies implementing `INexusPlugin` (isolated load context)
- Persist per-tenant enable/disable in `tenant_plugin_settings` (`plugins.read` / `plugins.manage`)
- Host invokes enrichment on lead create/move and notification-channel plugins after in-app notification create
- Plugin failures are logged and isolated; they must not fail the primary CRM operation

## Consequences

- New capabilities require a new interface in Abstractions + a host hook
- External DLLs are trusted code — operators must control the probe directory
- Later: signed packages, marketplace metadata, richer settings JSON UI
