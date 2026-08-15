using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusCRM.Domain.Tenancy;

namespace NexusCRM.Infrastructure.Persistence.Configurations;

internal sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("tenants");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Slug)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Plan)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(x => x.RowVersion)
            .HasColumnName("row_version")
            .IsConcurrencyToken()
            .HasDefaultValue(Array.Empty<byte>());

        builder.HasIndex(x => x.Slug).IsUnique();
    }
}
