using Microsoft.EntityFrameworkCore;
using NexusCRM.Application.Abstractions.Persistence;
using NexusCRM.Application.Abstractions.Tenancy;
using NexusCRM.Domain.Common;
using NexusCRM.Domain.Customers;
using NexusCRM.Domain.Deals;
using NexusCRM.Domain.Identity;
using NexusCRM.Domain.Leads;
using NexusCRM.Domain.Notifications;
using NexusCRM.Domain.Pipelines;
using NexusCRM.Domain.Plugins;
using NexusCRM.Domain.Tenancy;
using NexusCRM.Domain.Workflows;

namespace NexusCRM.Infrastructure.Persistence;

public sealed class NexusDbContext : DbContext, IUnitOfWork
{
    private readonly ITenantContext _tenantContext;

    public NexusDbContext(DbContextOptions<NexusDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();

    public DbSet<AuthAuditEntry> AuthAuditEntries => Set<AuthAuditEntry>();

    public DbSet<Pipeline> Pipelines => Set<Pipeline>();

    public DbSet<Lead> Leads => Set<Lead>();

    public DbSet<Deal> Deals => Set<Deal>();

    public DbSet<WorkflowDefinition> WorkflowDefinitions => Set<WorkflowDefinition>();

    public DbSet<WorkflowRun> WorkflowRuns => Set<WorkflowRun>();

    public DbSet<AppNotification> Notifications => Set<AppNotification>();

    public DbSet<TenantPluginSetting> TenantPluginSettings => Set<TenantPluginSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NexusDbContext).Assembly);

        modelBuilder.Entity<Customer>().HasQueryFilter(c =>
            !c.IsDeleted &&
            (!_tenantContext.IsResolved ||
             _tenantContext.TenantId == null ||
             c.TenantId == _tenantContext.TenantId));

        modelBuilder.Entity<Role>().HasQueryFilter(r =>
            !_tenantContext.IsResolved ||
            _tenantContext.TenantId == null ||
            r.TenantId == _tenantContext.TenantId);

        modelBuilder.Entity<ApiKey>().HasQueryFilter(k =>
            !_tenantContext.IsResolved ||
            _tenantContext.TenantId == null ||
            k.TenantId == _tenantContext.TenantId);

        modelBuilder.Entity<Pipeline>().HasQueryFilter(p =>
            !_tenantContext.IsResolved ||
            _tenantContext.TenantId == null ||
            p.TenantId == _tenantContext.TenantId);

        modelBuilder.Entity<Lead>().HasQueryFilter(l =>
            !_tenantContext.IsResolved ||
            _tenantContext.TenantId == null ||
            l.TenantId == _tenantContext.TenantId);

        modelBuilder.Entity<Deal>().HasQueryFilter(d =>
            !_tenantContext.IsResolved ||
            _tenantContext.TenantId == null ||
            d.TenantId == _tenantContext.TenantId);

        modelBuilder.Entity<WorkflowDefinition>().HasQueryFilter(w =>
            !_tenantContext.IsResolved ||
            _tenantContext.TenantId == null ||
            w.TenantId == _tenantContext.TenantId);

        modelBuilder.Entity<WorkflowRun>().HasQueryFilter(r =>
            !_tenantContext.IsResolved ||
            _tenantContext.TenantId == null ||
            r.TenantId == _tenantContext.TenantId);

        modelBuilder.Entity<AppNotification>().HasQueryFilter(n =>
            !_tenantContext.IsResolved ||
            _tenantContext.TenantId == null ||
            n.TenantId == _tenantContext.TenantId);

        modelBuilder.Entity<TenantPluginSetting>().HasQueryFilter(s =>
            !_tenantContext.IsResolved ||
            _tenantContext.TenantId == null ||
            s.TenantId == _tenantContext.TenantId);

        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Property(nameof(IAuditable.CreatedAtUtc)).CurrentValue ??= DateTimeOffset.UtcNow;
            }
        }

        foreach (var entry in ChangeTracker.Entries<AggregateRoot>())
        {
            if (entry.State != EntityState.Added)
            {
                continue;
            }

            var rowVersion = entry.Property(nameof(AggregateRoot.RowVersion));
            if (rowVersion.CurrentValue is not byte[] { Length: > 0 })
            {
                rowVersion.CurrentValue = Guid.NewGuid().ToByteArray();
            }
        }

        // OwnsMany(RefreshToken) can mark brand-new tokens Modified (UPDATE 0 rows) instead of Added.
        foreach (var entry in ChangeTracker.Entries<RefreshToken>().Where(e => e.State == EntityState.Modified))
        {
            if (await entry.GetDatabaseValuesAsync(cancellationToken) is null)
            {
                entry.State = EntityState.Added;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
