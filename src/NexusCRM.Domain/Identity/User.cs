using NexusCRM.Domain.Common;

namespace NexusCRM.Domain.Identity;

public sealed class User : AggregateRoot
{
    private readonly List<UserTenantMembership> _memberships = [];
    private readonly List<RefreshToken> _refreshTokens = [];

    private User(
        Guid id,
        string email,
        string displayName,
        string passwordHash)
        : base(id)
    {
        Email = email;
        DisplayName = displayName;
        PasswordHash = passwordHash;
        Status = UserStatus.Active;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    private User()
    {
    }

    public string Email { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public UserStatus Status { get; private set; }

    public bool EmailConfirmed { get; private set; }

    public bool MfaEnabled { get; private set; }

    public string? MfaSecret { get; private set; }

    public int AccessFailedCount { get; private set; }

    public DateTimeOffset? LockoutEndUtc { get; private set; }

    public DateTimeOffset? LastLoginAtUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<UserTenantMembership> Memberships => _memberships.AsReadOnly();

    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    public bool IsLockedOut =>
        LockoutEndUtc.HasValue && LockoutEndUtc.Value > DateTimeOffset.UtcNow;

    public static User Create(string email, string displayName, string passwordHash, Guid? id = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        var user = new User(
            id ?? Guid.NewGuid(),
            email.Trim().ToLowerInvariant(),
            displayName.Trim(),
            passwordHash);

        user.Raise(new UserRegisteredDomainEvent(user.Id, user.Email));
        return user;
    }

    public void JoinTenant(Guid tenantId, Guid roleId, bool isDefault = false)
    {
        if (_memberships.Any(m => m.TenantId == tenantId))
        {
            return;
        }

        if (isDefault)
        {
            foreach (var membership in _memberships)
            {
                membership.ClearDefault();
            }
        }

        _memberships.Add(UserTenantMembership.Create(tenantId, roleId, isDefault || _memberships.Count == 0));
    }

    public UserTenantMembership? GetMembership(Guid tenantId) =>
        _memberships.FirstOrDefault(m => m.TenantId == tenantId);

    public void RecordSuccessfulLogin()
    {
        AccessFailedCount = 0;
        LockoutEndUtc = null;
        LastLoginAtUtc = DateTimeOffset.UtcNow;
    }

    public void RecordFailedLogin(int maxFailedAttempts = 5, int lockoutMinutes = 15)
    {
        AccessFailedCount++;
        if (AccessFailedCount >= maxFailedAttempts)
        {
            LockoutEndUtc = DateTimeOffset.UtcNow.AddMinutes(lockoutMinutes);
            AccessFailedCount = 0;
            Raise(new UserLockedOutDomainEvent(Id, LockoutEndUtc.Value));
        }
    }

    public void EnableMfa(string secret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);
        MfaSecret = secret;
        MfaEnabled = true;
    }

    public void DisableMfa()
    {
        MfaEnabled = false;
        MfaSecret = null;
    }

    public void ConfirmEmail() => EmailConfirmed = true;

    public void SetPasswordHash(string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        PasswordHash = passwordHash;
    }

    public RefreshToken IssueRefreshToken(string tokenHash, string? deviceInfo, string? ipAddress, TimeSpan lifetime)
    {
        var token = RefreshToken.Create(Id, tokenHash, deviceInfo, ipAddress, lifetime);
        _refreshTokens.Add(token);
        return token;
    }

    /// <summary>
    /// Keeps only the newest active refresh tokens (session cap).
    /// </summary>
    public void EnforceRefreshTokenLimits(int maxActive)
    {
        if (maxActive < 1)
        {
            maxActive = 1;
        }

        var surplusActive = _refreshTokens
            .Where(t => t.IsActive)
            .OrderByDescending(t => t.CreatedAtUtc)
            .Skip(maxActive)
            .ToList();

        foreach (var token in surplusActive)
        {
            token.Revoke("Exceeded max active sessions");
        }
    }

    public RefreshToken? FindActiveRefreshToken(string tokenHash) =>
        _refreshTokens.FirstOrDefault(t => t.TokenHash == tokenHash && t.IsActive);

    public void RevokeRefreshToken(string tokenHash, string reason, string? replacedByHash = null)
    {
        var token = _refreshTokens.FirstOrDefault(t => t.TokenHash == tokenHash);
        token?.Revoke(reason, replacedByHash);
    }

    public void RevokeAllRefreshTokens(string reason)
    {
        foreach (var token in _refreshTokens.Where(t => t.IsActive))
        {
            token.Revoke(reason);
        }
    }
}

public enum UserStatus
{
    Active = 0,
    Suspended = 1,
    Disabled = 2
}

public sealed class UserTenantMembership
{
    private UserTenantMembership(Guid tenantId, Guid roleId, bool isDefault)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        RoleId = roleId;
        IsDefault = isDefault;
        JoinedAtUtc = DateTimeOffset.UtcNow;
    }

    private UserTenantMembership()
    {
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid RoleId { get; private set; }

    public bool IsDefault { get; private set; }

    public DateTimeOffset JoinedAtUtc { get; private set; }

    public static UserTenantMembership Create(Guid tenantId, Guid roleId, bool isDefault) =>
        new(tenantId, roleId, isDefault);

    internal void ClearDefault() => IsDefault = false;
}

public sealed record UserRegisteredDomainEvent(Guid UserId, string Email) : DomainEvent;

public sealed record UserLockedOutDomainEvent(Guid UserId, DateTimeOffset LockoutEndUtc) : DomainEvent;
