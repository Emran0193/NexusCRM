using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusCRM.Domain.Plugins;

namespace NexusCRM.Infrastructure.Persistence.Configurations;

internal sealed class TenantPluginSettingConfiguration : IEntityTypeConfiguration<TenantPluginSetting>
{
    public void Configure(EntityTypeBuilder<TenantPluginSetting> builder)
    {
        builder.ToTable("tenant_plugin_settings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PluginId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.SettingsJson).HasColumnType("jsonb");
        builder.Property(x => x.RowVersion).HasColumnName("row_version").IsConcurrencyToken();
        builder.HasIndex(x => new { x.TenantId, x.PluginId }).IsUnique();
    }
}
