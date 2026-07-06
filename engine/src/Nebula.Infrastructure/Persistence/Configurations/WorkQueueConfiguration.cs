using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class WorkQueueConfiguration : IEntityTypeConfiguration<WorkQueue>
{
    public void Configure(EntityTypeBuilder<WorkQueue> builder)
    {
        builder.ToTable("WorkQueues");
        builder.HasKey(q => q.Id);

        builder.Property(q => q.Name).IsRequired().HasMaxLength(160);
        builder.Property(q => q.WorkType).IsRequired().HasMaxLength(32);
        builder.Property(q => q.Status).IsRequired().HasMaxLength(32);
        builder.Property(q => q.Description).HasMaxLength(500);
        builder.Property(q => q.RowVersion).IsRowVersion();
        builder.HasQueryFilter(q => !q.IsDeleted);

        builder.HasIndex(q => new { q.Name, q.WorkType })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");
        builder.HasIndex(q => new { q.WorkType, q.Status });
        builder.HasIndex(q => new { q.WorkType, q.IsFallback })
            .HasFilter("\"IsFallback\" = true AND \"Status\" = 'Active' AND \"IsDeleted\" = false");
    }
}
