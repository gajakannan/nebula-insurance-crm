using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class WorkflowTransitionConfiguration : IEntityTypeConfiguration<WorkflowTransition>
{
    public void Configure(EntityTypeBuilder<WorkflowTransition> builder)
    {
        builder.ToTable("WorkflowTransitions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.WorkflowType).IsRequired().HasMaxLength(50);
        builder.Property(e => e.FromState).HasMaxLength(30);
        builder.Property(e => e.ToState).IsRequired().HasMaxLength(30);
        builder.Property(e => e.Reason).HasMaxLength(500);
        builder.Property(e => e.ActorUserId).IsRequired();
        builder.Property(e => e.OccurredAt).IsRequired();

        builder.HasIndex(e => new { e.EntityId, e.OccurredAt })
            .HasDatabaseName("IX_WT_EntityId_OccurredAt")
            .IsDescending(false, true);
    }
}
