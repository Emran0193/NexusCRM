using Microsoft.EntityFrameworkCore;
using NexusCRM.Application.Abstractions.Identity;
using NexusCRM.Application.Abstractions.Tenancy;
using NexusCRM.Domain.Identity;
using NexusCRM.Infrastructure.Persistence;

namespace NexusCRM.Infrastructure.Identity;

/// <summary>
/// Baseline ABAC: tenant ownership + permission action mapping.
/// Extend per aggregate as CRM modules grow.
/// </summary>
internal sealed class ResourceAuthorizationService : IResourceAuthorizationService
{
    private readonly NexusDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ResourceAuthorizationService(NexusDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<bool> CanAccessAsync(
        string resourceType,
        Guid resourceId,
        string action,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.TenantId is null)
        {
            return false;
        }

        return resourceType.ToLowerInvariant() switch
        {
            "customer" => await CanAccessCustomerAsync(resourceId, action, cancellationToken),
            _ => false
        };
    }

    private async Task<bool> CanAccessCustomerAsync(
        Guid resourceId,
        string action,
        CancellationToken cancellationToken)
    {
        var requiredPermission = action.ToLowerInvariant() switch
        {
            "read" => SystemPermissions.CustomersRead,
            "write" or "update" => SystemPermissions.CustomersWrite,
            "delete" => SystemPermissions.CustomersDelete,
            _ => null
        };

        if (requiredPermission is null || !_currentUser.HasPermission(requiredPermission))
        {
            return false;
        }

        return await _db.Customers.AnyAsync(
            c => c.Id == resourceId && c.TenantId == _currentUser.TenantId,
            cancellationToken);
    }
}
