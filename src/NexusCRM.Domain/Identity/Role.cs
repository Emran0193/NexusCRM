using NexusCRM.Domain.Common;

namespace NexusCRM.Domain.Identity;

public sealed class Role : AggregateRoot, ITenantScoped
{
    // List (not HashSet): Npgsql maps text[] to IList/array types only.
    private readonly List<string> _permissions = [];

    private Role(Guid id, Guid tenantId, string name, string? description, bool isSystem)
        : base(id)
    {
        TenantId = tenantId;
        Name = name;
        Description = description;
        IsSystem = isSystem;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    private Role()
    {
    }

    public Guid TenantId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public bool IsSystem { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<string> Permissions => _permissions.AsReadOnly();

    public static Role Create(Guid tenantId, string name, string? description = null, bool isSystem = false)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Role(Guid.NewGuid(), tenantId, name.Trim(), description?.Trim(), isSystem);
    }

    public void Grant(string permissionCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionCode);
        var normalized = permissionCode.Trim().ToLowerInvariant();
        if (_permissions.Contains(normalized, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        _permissions.Add(normalized);
    }

    public void Revoke(string permissionCode)
    {
        _permissions.RemoveAll(p => p.Equals(permissionCode.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public bool HasPermission(string permissionCode) =>
        _permissions.Contains(permissionCode, StringComparer.OrdinalIgnoreCase);
}
