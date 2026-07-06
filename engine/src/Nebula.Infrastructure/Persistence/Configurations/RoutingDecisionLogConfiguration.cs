using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class RoutingDecisionLogConfiguration : IEntityTypeConfiguration<RoutingDecisionLog>
{
    public void Configure(EntityTypeBuilder<RoutingDecisionLog> builder)
    {
        builder.ToTable("RoutingDecisionLogs");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.SourceType).IsRequired().HasMaxLength(32);
        builder.Property(l => l.Outcome).IsRequired().HasMaxLength(64);
        builder.Property(l => l.ReasonCode).IsRequired().HasMaxLength(120);
        builder.Property(l => l.DecisionPayloadJson).IsRequired().HasColumnType("jsonb");
        builder.Property(l => l.RowVersion).IsRowVersion();
        builder.HasQueryFilter(l => !l.IsDeleted);
        builder.HasOne(l => l.QueueWorkItem).WithMany().HasForeignKey(l => l.QueueWorkItemId);
        builder.HasIndex(l => new { l.SourceType, l.SourceId, l.OccurredAt });
        builder.HasIndex(l => new { l.QueueWorkItemId, l.OccurredAt });
    }
}
