using NexusCRM.Domain.Common;

namespace NexusCRM.Domain.Identity;

public sealed class RefreshToken : Entity
{
    private RefreshToken(
        Guid id,
        Guid userId,
        string tokenHash,
        string? deviceInfo,
        string? ipAddress,
        DateTimeOffset expiresAtUtc)
        : base(id)
    {
        UserId = userId;
        TokenHash = tokenHash;
        DeviceInfo = deviceInfo;
        IpAddress = ipAddress;
        ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    private RefreshToken()
    {
    }

    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    public string? DeviceInfo { get; private set; }

    public string? IpAddress { get; private set; }

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? RevokedAtUtc { get; private set; }

    public string? RevokedReason { get; private set; }

    public string? ReplacedByTokenHash { get; private set; }

    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAtUtc;

    public bool IsRevoked => RevokedAtUtc.HasValue;

    public bool IsActive => !IsRevoked && !IsExpired;

    public static RefreshToken Create(
        Guid userId,
        string tokenHash,
        string? deviceInfo,
        string? ipAddress,
        TimeSpan lifetime)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);
        return new RefreshToken(
            Guid.NewGuid(),
            userId,
            tokenHash,
            deviceInfo,
            ipAddress,
            DateTimeOffset.UtcNow.Add(lifetime));
    }

    public void Revoke(string reason, string? replacedByHash = null)
    {
        if (IsRevoked)
        {
            return;
        }

        RevokedAtUtc = DateTimeOffset.UtcNow;
        RevokedReason = reason;
        ReplacedByTokenHash = replacedByHash;
    }
}
