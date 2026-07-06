using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class AssignmentRuleConfiguration : IEntityTypeConfiguration<AssignmentRule>
{
    public void Configure(EntityTypeBuilder<AssignmentRule> builder)
    {
        builder.ToTable("AssignmentRules");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.RuleType).IsRequired().HasMaxLength(64);
        builder.Property(r => r.Status).IsRequired().HasMaxLength(32);
        builder.Property(r => r.ConditionsJson).IsRequired().HasColumnType("jsonb");
        builder.Property(r => r.OutcomeJson).IsRequired().HasColumnType("jsonb");
        builder.Property(r => r.RowVersion).IsRowVersion();
        builder.HasQueryFilter(r => !r.IsDeleted);
        builder.HasOne(r => r.WorkQueue).WithMany(q => q.AssignmentRules).HasForeignKey(r => r.WorkQueueId);
        builder.HasIndex(r => new { r.WorkQueueId, r.Status, r.Precedence });
        builder.HasIndex(r => new { r.RuleType, r.Status, r.Precedence });
    }
}
