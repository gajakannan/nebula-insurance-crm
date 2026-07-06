using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class QueueWorkItemConfiguration : IEntityTypeConfiguration<QueueWorkItem>
{
    public void Configure(EntityTypeBuilder<QueueWorkItem> builder)
    {
        builder.ToTable("QueueWorkItems");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.SourceType).IsRequired().HasMaxLength(32);
        builder.Property(i => i.QueueStatus).IsRequired().HasMaxLength(32);
        builder.Property(i => i.RuleVersion).HasMaxLength(80);
        builder.Property(i => i.MatchReason).HasMaxLength(160);
        builder.Property(i => i.IdempotencyKey).IsRequired().HasMaxLength(240);
        builder.Property(i => i.RowVersion).IsRowVersion();
        builder.HasQueryFilter(i => !i.IsDeleted);
        builder.HasOne(i => i.WorkQueue).WithMany(q => q.QueueWorkItems).HasForeignKey(i => i.WorkQueueId);
        builder.HasOne(i => i.AssignedToUser).WithMany().HasForeignKey(i => i.AssignedToUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(i => new { i.SourceType, i.SourceId, i.IdempotencyKey }).IsUnique();
        builder.HasIndex(i => new { i.WorkQueueId, i.QueueStatus, i.RoutedAt });
    }
}
