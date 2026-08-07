namespace NexusCRM.Domain.Common;

/// <summary>
/// Marks an entity that must be isolated per tenant.
/// Enforced via EF global query filters and application-layer checks.
/// </summary>
public interface ITenantScoped
{
    Guid TenantId { get; }
}
