namespace NexusCRM.Application.Abstractions.Tenancy;

public interface ITenantContext
{
    Guid? TenantId { get; }

    string? TenantSlug { get; }

    bool IsResolved { get; }
}

public interface ICurrentUser
{
    Guid? UserId { get; }

    string? Email { get; }

    Guid? TenantId { get; }

    IReadOnlyCollection<string> Roles { get; }

    IReadOnlyCollection<string> Permissions { get; }

    bool IsAuthenticated { get; }

    bool HasPermission(string permission);
}
