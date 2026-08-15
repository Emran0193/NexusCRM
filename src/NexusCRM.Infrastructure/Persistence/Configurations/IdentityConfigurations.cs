using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusCRM.Domain.Identity;

namespace NexusCRM.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Email).HasMaxLength(320).IsRequired();
        builder.HasIndex(x => x.Email).IsUnique();
        builder.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.MfaSecret).HasMaxLength(128);
        // Stored for future optimistic concurrency; not enforced here — empty/bytea tokens
        // caused DbUpdateConcurrencyException on login under Npgsql.
        builder.Property(x => x.RowVersion).HasColumnName("row_version");

        builder.OwnsMany(x => x.Memberships, m =>
        {
            m.ToTable("user_tenant_memberships");
            m.WithOwner().HasForeignKey("UserId");
            m.HasKey(x => x.Id);
            m.Property(x => x.TenantId).IsRequired();
            m.Property(x => x.RoleId).IsRequired();
            m.HasIndex(x => new { x.TenantId, x.RoleId });
            m.HasIndex("UserId", nameof(UserTenantMembership.TenantId)).IsUnique();
        });

        builder.Navigation(x => x.Memberships)
            .HasField("_memberships")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(x => x.RefreshTokens, t =>
        {
            t.ToTable("refresh_tokens");
            t.WithOwner().HasForeignKey(x => x.UserId);
            t.HasKey(x => x.Id);
            t.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
            t.HasIndex(x => x.TokenHash);
            t.Property(x => x.DeviceInfo).HasMaxLength(256);
            t.Property(x => x.IpAddress).HasMaxLength(64);
            t.Property(x => x.RevokedReason).HasMaxLength(200);
            t.Property(x => x.ReplacedByTokenHash).HasMaxLength(128);
        });

        builder.Navigation(x => x.RefreshTokens)
            .HasField("_refreshTokens")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.RowVersion).HasColumnName("row_version");
        builder.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();

        var permissions = builder.Property<List<string>>("_permissions")
            .HasColumnName("permissions")
            .HasColumnType("text[]");

        permissions.Metadata.SetValueComparer(
            new ValueComparer<List<string>>(
                (a, b) => a!.SequenceEqual(b!, StringComparer.OrdinalIgnoreCase),
                v => v.Aggregate(0, (h, i) => HashCode.Combine(h, i.GetHashCode(StringComparison.OrdinalIgnoreCase))),
                v => v.ToList()));
    }
}

internal sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Module).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
    }
}

internal sealed class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.ToTable("api_keys");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Prefix).HasMaxLength(16).IsRequired();
        builder.Property(x => x.KeyHash).HasMaxLength(128).IsRequired();
        builder.Property(x => x.RowVersion).HasColumnName("row_version").IsConcurrencyToken();
        builder.HasIndex(x => x.Prefix);
        builder.HasIndex(x => new { x.TenantId, x.Name });

        builder.Property(x => x.Scopes)
            .HasColumnType("text[]");
    }
}

internal sealed class AuthAuditEntryConfiguration : IEntityTypeConfiguration<AuthAuditEntry>
{
    public void Configure(EntityTypeBuilder<AuthAuditEntry> builder)
    {
        builder.ToTable("auth_audit_entries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Action).HasMaxLength(100).IsRequired();
        builder.Property(x => x.IpAddress).HasMaxLength(64);
        builder.Property(x => x.UserAgent).HasMaxLength(512);
        builder.Property(x => x.Details).HasMaxLength(1000);
        builder.Property(x => x.CorrelationId).HasMaxLength(64);
        builder.HasIndex(x => x.OccurredAtUtc);
        builder.HasIndex(x => new { x.UserId, x.OccurredAtUtc });
    }
}
