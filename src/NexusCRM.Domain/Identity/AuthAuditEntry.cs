using NexusCRM.Domain.Common;

namespace NexusCRM.Domain.Identity;

public sealed class AuthAuditEntry : Entity
{
    private AuthAuditEntry(
        Guid id,
        Guid? tenantId,
        Guid? userId,
        string action,
        bool succeeded,
        string? ipAddress,
        string? userAgent,
        string? details,
        string? correlationId)
        : base(id)
    {
        TenantId = tenantId;
        UserId = userId;
        Action = action;
        Succeeded = succeeded;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        Details = details;
        CorrelationId = correlationId;
        OccurredAtUtc = DateTimeOffset.UtcNow;
    }

    private AuthAuditEntry()
    {
    }

    public Guid? TenantId { get; private set; }

    public Guid? UserId { get; private set; }

    public string Action { get; private set; } = string.Empty;

    public bool Succeeded { get; private set; }

    public string? IpAddress { get; private set; }

    public string? UserAgent { get; private set; }

    public string? Details { get; private set; }

    public string? CorrelationId { get; private set; }

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public static AuthAuditEntry Create(
        string action,
        bool succeeded,
        Guid? tenantId = null,
        Guid? userId = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? details = null,
        string? correlationId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);

        return new AuthAuditEntry(
            Guid.NewGuid(),
            tenantId,
            userId,
            action,
            succeeded,
            ipAddress,
            userAgent,
            details,
            correlationId);
    }
}

public static class AuthAuditActions
{
    public const string LoginSucceeded = "auth.login.succeeded";
    public const string LoginFailed = "auth.login.failed";
    public const string LoginLockedOut = "auth.login.locked_out";
    public const string RefreshSucceeded = "auth.refresh.succeeded";
    public const string RefreshFailed = "auth.refresh.failed";
    public const string RefreshReuseDetected = "auth.refresh.reuse_detected";
    public const string Logout = "auth.logout";
    public const string MfaChallenge = "auth.mfa.challenge";
    public const string ApiKeyUsed = "auth.apikey.used";
}
