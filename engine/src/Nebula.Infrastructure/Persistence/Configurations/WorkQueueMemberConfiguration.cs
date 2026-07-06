using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class WorkQueueMemberConfiguration : IEntityTypeConfiguration<WorkQueueMember>
{
    public void Configure(EntityTypeBuilder<WorkQueueMember> builder)
    {
        builder.ToTable("WorkQueueMembers");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Role).IsRequired().HasMaxLength(32);
        builder.Property(m => m.RowVersion).IsRowVersion();
        builder.HasQueryFilter(m => !m.IsDeleted);
        builder.HasOne(m => m.WorkQueue).WithMany(q => q.Members).HasForeignKey(m => m.WorkQueueId);
        builder.HasOne(m => m.UserProfile).WithMany().HasForeignKey(m => m.UserProfileId);
        builder.HasIndex(m => new { m.WorkQueueId, m.UserProfileId, m.EffectiveFrom });
        builder.HasIndex(m => new { m.UserProfileId, m.EffectiveTo });
    }
}
