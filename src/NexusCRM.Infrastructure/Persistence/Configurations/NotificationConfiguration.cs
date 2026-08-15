using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusCRM.Domain.Notifications;

namespace NexusCRM.Infrastructure.Persistence.Configurations;

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<AppNotification>
{
    public void Configure(EntityTypeBuilder<AppNotification> builder)
    {
        builder.ToTable("notifications");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Body).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Category).HasMaxLength(64).IsRequired();
        builder.Property(x => x.EntityType).HasMaxLength(64);
        builder.Property(x => x.Href).HasMaxLength(400);
        builder.Property(x => x.RowVersion).HasColumnName("row_version").IsConcurrencyToken();
        builder.HasIndex(x => new { x.TenantId, x.CreatedAtUtc });
        builder.HasIndex(x => new { x.TenantId, x.IsRead, x.RecipientUserId });
    }
}
