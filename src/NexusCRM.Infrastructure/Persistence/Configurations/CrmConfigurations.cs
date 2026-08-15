using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusCRM.Domain.Deals;
using NexusCRM.Domain.Leads;
using NexusCRM.Domain.Pipelines;

namespace NexusCRM.Infrastructure.Persistence.Configurations;

internal sealed class PipelineConfiguration : IEntityTypeConfiguration<Pipeline>
{
    public void Configure(EntityTypeBuilder<Pipeline> builder)
    {
        builder.ToTable("pipelines");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.RowVersion).HasColumnName("row_version").IsConcurrencyToken();
        builder.HasIndex(x => new { x.TenantId, x.Type, x.IsDefault });

        builder.Navigation(x => x.Stages).HasField("_stages").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.OwnsMany(x => x.Stages, stages =>
        {
            stages.ToTable("pipeline_stages");
            stages.WithOwner().HasForeignKey("PipelineId");
            stages.HasKey(s => s.Id);
            stages.Property(s => s.Name).HasMaxLength(100).IsRequired();
            stages.HasIndex(s => new { s.SortOrder });
        });
    }
}

internal sealed class LeadConfiguration : IEntityTypeConfiguration<Lead>
{
    public void Configure(EntityTypeBuilder<Lead> builder)
    {
        builder.ToTable("leads");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Source).HasMaxLength(100);
        builder.Property(x => x.Email).HasMaxLength(320);
        builder.Property(x => x.Phone).HasMaxLength(50);
        builder.Property(x => x.CompanyName).HasMaxLength(200);
        builder.Property(x => x.Notes).HasMaxLength(4000);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.RowVersion).HasColumnName("row_version").IsConcurrencyToken();

        var tags = builder.Property<List<string>>("_tags").HasColumnName("tags").HasColumnType("text[]");
        tags.Metadata.SetValueComparer(
            new ValueComparer<List<string>>(
                (a, b) => a!.SequenceEqual(b!),
                v => v.Aggregate(0, (h, i) => HashCode.Combine(h, i.GetHashCode())),
                v => v.ToList()));

        builder.HasIndex(x => new { x.TenantId, x.StageId });
        builder.HasIndex(x => new { x.TenantId, x.Title });
        builder.HasIndex(x => new { x.TenantId, x.Email });
        builder.HasIndex(x => new { x.TenantId, x.CompanyName });
        builder.HasIndex(x => new { x.TenantId, x.Status, x.CreatedAtUtc });
        builder.HasIndex(x => x.PipelineId);
    }
}

internal sealed class DealConfiguration : IEntityTypeConfiguration<Deal>
{
    public void Configure(EntityTypeBuilder<Deal> builder)
    {
        builder.ToTable("deals");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.RowVersion).HasColumnName("row_version").IsConcurrencyToken();

        builder.HasIndex(x => new { x.TenantId, x.StageId });
        builder.HasIndex(x => new { x.TenantId, x.Title });
        builder.HasIndex(x => new { x.TenantId, x.Status, x.CreatedAtUtc });
        builder.HasIndex(x => x.PipelineId);
    }
}
