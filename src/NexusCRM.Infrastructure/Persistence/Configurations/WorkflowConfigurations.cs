using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusCRM.Domain.Workflows;

namespace NexusCRM.Infrastructure.Persistence.Configurations;

internal sealed class WorkflowDefinitionConfiguration : IEntityTypeConfiguration<WorkflowDefinition>
{
    public void Configure(EntityTypeBuilder<WorkflowDefinition> builder)
    {
        builder.ToTable("workflow_definitions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.TriggerType).HasConversion<string>().HasMaxLength(64);
        builder.Property(x => x.RowVersion).HasColumnName("row_version").IsConcurrencyToken();
        builder.HasIndex(x => new { x.TenantId, x.TriggerType, x.IsEnabled });

        builder.Navigation(x => x.Conditions).HasField("_conditions").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.OwnsMany(x => x.Conditions, c =>
        {
            c.ToTable("workflow_conditions");
            c.WithOwner().HasForeignKey("WorkflowDefinitionId");
            c.HasKey(x => x.Id);
            c.Property(x => x.Field).HasMaxLength(100).IsRequired();
            c.Property(x => x.Operator).HasConversion<string>().HasMaxLength(32);
            c.Property(x => x.Value).HasMaxLength(200).IsRequired();
        });

        builder.Navigation(x => x.Actions).HasField("_actions").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.OwnsMany(x => x.Actions, a =>
        {
            a.ToTable("workflow_actions");
            a.WithOwner().HasForeignKey("WorkflowDefinitionId");
            a.HasKey(x => x.Id);
            a.Property(x => x.Type).HasConversion<string>().HasMaxLength(64);
            a.Property(x => x.Target).HasMaxLength(200);
            a.Property(x => x.Value).HasMaxLength(1000);
        });
    }
}

internal sealed class WorkflowRunConfiguration : IEntityTypeConfiguration<WorkflowRun>
{
    public void Configure(EntityTypeBuilder<WorkflowRun> builder)
    {
        builder.ToTable("workflow_runs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TriggerType).HasConversion<string>().HasMaxLength(64);
        builder.Property(x => x.EntityType).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.ContextJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.ResultSummary).HasMaxLength(2000);
        builder.Property(x => x.Error).HasMaxLength(2000);
        builder.Property(x => x.RowVersion).HasColumnName("row_version").IsConcurrencyToken();
        builder.HasIndex(x => new { x.TenantId, x.Status, x.CreatedAtUtc });
    }
}
