using NexusCRM.Application.Abstractions.Identity;
using NexusCRM.Domain.Identity;
using NexusCRM.Infrastructure.Persistence;

namespace NexusCRM.Infrastructure.Identity;

internal sealed class AuthAuditService : IAuthAuditService
{
    private readonly NexusDbContext _db;

    public AuthAuditService(NexusDbContext db)
    {
        _db = db;
    }

    public async Task WriteAsync(
        string action,
        bool succeeded,
        Guid? tenantId = null,
        Guid? userId = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? details = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        var entry = AuthAuditEntry.Create(
            action,
            succeeded,
            tenantId,
            userId,
            ipAddress,
            userAgent,
            details,
            correlationId);

        await _db.AuthAuditEntries.AddAsync(entry, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
