using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class CoverageWindowConfiguration : IEntityTypeConfiguration<CoverageWindow>
{
    public void Configure(EntityTypeBuilder<CoverageWindow> builder)
    {
        builder.ToTable("CoverageWindows");
        builder.HasKey(w => w.Id);

        builder.Property(w => w.Status).IsRequired().HasMaxLength(32);
        builder.Property(w => w.Reason).HasMaxLength(500);
        builder.Property(w => w.RowVersion).IsRowVersion();
        builder.HasQueryFilter(w => !w.IsDeleted);
        builder.HasOne(w => w.CoveredUser).WithMany().HasForeignKey(w => w.CoveredUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(w => w.BackupUser).WithMany().HasForeignKey(w => w.BackupUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(w => w.WorkQueue).WithMany(q => q.CoverageWindows).HasForeignKey(w => w.WorkQueueId);
        builder.HasIndex(w => new { w.CoveredUserId, w.StartsAt, w.EndsAt, w.Status });
        builder.HasIndex(w => new { w.WorkQueueId, w.StartsAt, w.EndsAt });
    }
}
