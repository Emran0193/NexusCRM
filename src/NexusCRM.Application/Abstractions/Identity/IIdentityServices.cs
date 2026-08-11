using NexusCRM.Domain.Identity;

namespace NexusCRM.Application.Abstractions.Identity;

public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string passwordHash);

    /// <summary>
    /// Runs a password verification against a dummy hash to reduce timing variance on unknown users.
    /// </summary>
    void PerformDummyVerification(string password);
}

public interface ITokenService
{
    AccessTokenResult CreateAccessToken(
        User user,
        Guid tenantId,
        string roleName,
        IReadOnlyCollection<string> permissions);

    string CreateRefreshToken();

    string HashToken(string token);
}

public sealed record AccessTokenResult(string Token, DateTimeOffset ExpiresAtUtc);

public interface ITotpService
{
    string GenerateSecret();

    bool VerifyCode(string secret, string code);
}

public interface IAuthAuditService
{
    Task WriteAsync(
        string action,
        bool succeeded,
        Guid? tenantId = null,
        Guid? userId = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? details = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default);
}

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<User?> GetByRefreshTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task AddAsync(User user, CancellationToken cancellationToken = default);
}

public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Role?> GetByNameAsync(Guid tenantId, string name, CancellationToken cancellationToken = default);

    Task AddAsync(Role role, CancellationToken cancellationToken = default);
}

public interface IPermissionChecker
{
    bool HasPermission(string permission);

    bool HasAnyPermission(params string[] permissions);
}

/// <summary>
/// ABAC-style resource authorization hook. Implementations can evaluate ownership, status, etc.
/// </summary>
public interface IResourceAuthorizationService
{
    Task<bool> CanAccessAsync(
        string resourceType,
        Guid resourceId,
        string action,
        CancellationToken cancellationToken = default);
}
