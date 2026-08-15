using NexusCRM.Application.Abstractions.Tenancy;

namespace NexusCRM.Infrastructure.Tenancy;

public sealed class TenantContext : ITenantContext
{
    public Guid? TenantId { get; private set; }

    public string? TenantSlug { get; private set; }

    public bool IsResolved => TenantId.HasValue;

    public void Set(Guid tenantId, string? slug = null)
    {
        TenantId = tenantId;
        TenantSlug = slug;
    }
}

public sealed class CurrentUser : ICurrentUser
{
    public Guid? UserId { get; private set; }

    public string? Email { get; private set; }

    public Guid? TenantId { get; private set; }

    public IReadOnlyCollection<string> Roles { get; private set; } = [];

    public IReadOnlyCollection<string> Permissions { get; private set; } = [];

    public bool IsAuthenticated => UserId.HasValue;

    public void Set(
        Guid userId,
        string? email,
        Guid? tenantId,
        IReadOnlyCollection<string>? roles = null,
        IReadOnlyCollection<string>? permissions = null)
    {
        UserId = userId;
        Email = email;
        TenantId = tenantId;
        Roles = roles ?? [];
        Permissions = permissions ?? [];
    }

    public bool HasPermission(string permission) =>
        Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
}
