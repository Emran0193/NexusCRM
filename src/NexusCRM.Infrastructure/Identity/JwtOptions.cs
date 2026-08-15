namespace NexusCRM.Infrastructure.Identity;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "NexusCRM";

    public string Audience { get; set; } = "NexusCRM.Web";

    public string SigningKey { get; set; } = "CHANGE_ME_TO_A_LONG_RANDOM_SECRET_KEY_AT_LEAST_32_CHARS";

    public int AccessTokenMinutes { get; set; } = 30;

    public int RefreshTokenDays { get; set; } = 14;
}

public static class NexusClaimTypes
{
    public const string TenantId = "tenant_id";
    public const string Permission = "permission";
    public const string DisplayName = "display_name";
}
