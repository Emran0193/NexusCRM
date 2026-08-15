using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusCRM.Domain.Customers;

namespace NexusCRM.Infrastructure.Persistence.Configurations;

internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(320);
        builder.Property(x => x.Phone).HasMaxLength(50);
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.RowVersion).HasColumnName("row_version").IsConcurrencyToken();

        var tagsProperty = builder.Property<List<string>>("_tags")
            .HasColumnName("tags")
            .HasColumnType("text[]");

        tagsProperty.Metadata.SetValueComparer(
            new ValueComparer<List<string>>(
                (a, b) => a!.SequenceEqual(b!),
                v => v.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
                v => v.ToList()));

        builder.Navigation(x => x.Contacts).HasField("_contacts").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.OwnsMany(x => x.Contacts, contacts =>
        {
            contacts.ToTable("customer_contacts");
            contacts.WithOwner().HasForeignKey("CustomerId");
            contacts.HasKey(c => c.Id);
            contacts.Property(c => c.Name).HasMaxLength(200).IsRequired();
            contacts.Property(c => c.Email).HasMaxLength(320);
            contacts.Property(c => c.Phone).HasMaxLength(50);
        });

        builder.Navigation(x => x.Notes).HasField("_notes").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.OwnsMany(x => x.Notes, notes =>
        {
            notes.ToTable("customer_notes");
            notes.WithOwner().HasForeignKey("CustomerId");
            notes.HasKey(n => n.Id);
            notes.Property(n => n.Body).HasMaxLength(4000).IsRequired();
        });

        builder.Navigation(x => x.Timeline).HasField("_timeline").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.OwnsMany(x => x.Timeline, timeline =>
        {
            timeline.ToTable("customer_timeline");
            timeline.WithOwner().HasForeignKey("CustomerId");
            timeline.HasKey(t => t.Id);
            timeline.Property(t => t.EventType).HasMaxLength(100).IsRequired();
            timeline.Property(t => t.Summary).HasMaxLength(500).IsRequired();
            timeline.HasIndex(t => t.OccurredAtUtc);
        });

        builder.HasIndex(x => new { x.TenantId, x.Email });
        builder.HasIndex(x => new { x.TenantId, x.DisplayName });
        builder.HasIndex(x => x.TenantId);
    }
}
