using NexusCRM.Domain.Identity;

namespace NexusCRM.Application.Abstractions.Identity;

public sealed class AuthSecurityOptions
{
    public const string SectionName = "Security";

    public int MaxFailedLoginAttempts { get; set; } = 5;

    public int LockoutMinutes { get; set; } = 15;

    public int MaxActiveRefreshTokens { get; set; } = 10;

    public int RefreshTokenDays { get; set; } = 14;

    public bool ExposeMetricsEndpoint { get; set; }

    public PasswordPolicyOptions Password { get; set; } = new();
}
