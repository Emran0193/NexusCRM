using NexusCRM.Domain.Common;

namespace NexusCRM.Domain.Identity;

public sealed class ApiKey : AggregateRoot, ITenantScoped
{
    private ApiKey(
        Guid id,
        Guid tenantId,
        Guid createdByUserId,
        string name,
        string prefix,
        string keyHash,
        IEnumerable<string> scopes,
        DateTimeOffset? expiresAtUtc)
        : base(id)
    {
        TenantId = tenantId;
        CreatedByUserId = createdByUserId;
        Name = name;
        Prefix = prefix;
        KeyHash = keyHash;
        Scopes = scopes.Select(s => s.Trim().ToLowerInvariant()).Distinct().ToList();
        ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        IsActive = true;
    }

    private ApiKey()
    {
    }

    public Guid TenantId { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Prefix { get; private set; } = string.Empty;

    public string KeyHash { get; private set; } = string.Empty;

    public IReadOnlyList<string> Scopes { get; private set; } = [];

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? ExpiresAtUtc { get; private set; }

    public DateTimeOffset? LastUsedAtUtc { get; private set; }

    public bool IsActive { get; private set; }

    public bool IsExpired => ExpiresAtUtc.HasValue && ExpiresAtUtc.Value <= DateTimeOffset.UtcNow;

    public bool IsUsable => IsActive && !IsExpired;

    public static ApiKey Create(
        Guid tenantId,
        Guid createdByUserId,
        string name,
        string prefix,
        string keyHash,
        IEnumerable<string> scopes,
        DateTimeOffset? expiresAtUtc = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);
        ArgumentException.ThrowIfNullOrWhiteSpace(keyHash);

        return new ApiKey(
            Guid.NewGuid(),
            tenantId,
            createdByUserId,
            name.Trim(),
            prefix,
            keyHash,
            scopes,
            expiresAtUtc);
    }

    public void MarkUsed() => LastUsedAtUtc = DateTimeOffset.UtcNow;

    public void Revoke() => IsActive = false;
}
