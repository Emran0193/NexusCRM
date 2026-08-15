using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using NexusCRM.Infrastructure.Tenancy;

namespace NexusCRM.Infrastructure.Persistence;

public sealed class NexusDbContextFactory : IDesignTimeDbContextFactory<NexusDbContext>
{
    public NexusDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<NexusDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=nexuscrm;Username=postgres;Password=postgres")
            .Options;

        return new NexusDbContext(options, new TenantContext());
    }
}
