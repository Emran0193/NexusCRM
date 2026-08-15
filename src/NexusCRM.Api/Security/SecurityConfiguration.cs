using NexusCRM.Infrastructure.Identity;

namespace NexusCRM.Api.Security;

public static class SecurityConfiguration
{
    public static void Validate(IConfiguration configuration, IHostEnvironment environment)
    {
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

        if (string.IsNullOrWhiteSpace(jwt.SigningKey) || jwt.SigningKey.Length < 32)
        {
            throw new InvalidOperationException(
                "Jwt:SigningKey must be at least 32 characters. Prefer user-secrets or environment variables.");
        }

        if (!environment.IsDevelopment() &&
            (jwt.SigningKey.Contains("Change", StringComparison.OrdinalIgnoreCase) ||
             jwt.SigningKey.Contains("Dev", StringComparison.OrdinalIgnoreCase) ||
             jwt.SigningKey.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                "Jwt:SigningKey looks like a development placeholder. Set a production secret before starting.");
        }

        if (jwt.AccessTokenMinutes is < 5 or > 120)
        {
            throw new InvalidOperationException("Jwt:AccessTokenMinutes must be between 5 and 120.");
        }
    }
}
