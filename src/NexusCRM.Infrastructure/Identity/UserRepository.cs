using Microsoft.EntityFrameworkCore;
using NexusCRM.Application.Abstractions.Identity;
using NexusCRM.Domain.Identity;
using NexusCRM.Infrastructure.Persistence;

namespace NexusCRM.Infrastructure.Identity;

internal sealed class UserRepository : IUserRepository
{
    private readonly NexusDbContext _db;

    public UserRepository(NexusDbContext db)
    {
        _db = db;
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        _db.Users
            .Include(u => u.Memberships)
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Email == email.Trim().ToLowerInvariant(), cancellationToken);

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Users
            .Include(u => u.Memberships)
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<User?> GetByRefreshTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        _db.Users
            .Include(u => u.Memberships)
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.RefreshTokens.Any(t => t.TokenHash == tokenHash), cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken = default) =>
        await _db.Users.AddAsync(user, cancellationToken);
}

internal sealed class RoleRepository : IRoleRepository
{
    private readonly NexusDbContext _db;

    public RoleRepository(NexusDbContext db)
    {
        _db = db;
    }

    public Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Roles.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task<Role?> GetByNameAsync(Guid tenantId, string name, CancellationToken cancellationToken = default) =>
        _db.Roles.FirstOrDefaultAsync(
            r => r.TenantId == tenantId && r.Name == name,
            cancellationToken);

    public async Task AddAsync(Role role, CancellationToken cancellationToken = default) =>
        await _db.Roles.AddAsync(role, cancellationToken);
}
