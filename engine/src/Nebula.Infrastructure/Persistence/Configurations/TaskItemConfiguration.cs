using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("Tasks");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Title).IsRequired().HasMaxLength(255);
        builder.Property(e => e.Description).HasMaxLength(2000);
        builder.Property(e => e.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Open");
        builder.Property(e => e.Priority).IsRequired().HasMaxLength(20).HasDefaultValue("Normal");
        builder.Property(e => e.AssignedToUserId).IsRequired();
        builder.Property(e => e.LinkedEntityType).HasMaxLength(50);
        builder.Property(e => e.CreatedByUserId).IsRequired();
        builder.Property(e => e.UpdatedByUserId).IsRequired();
        builder.Property(e => e.DeletedByUserId);
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);

        builder.Property(e => e.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.HasIndex(e => new { e.AssignedToUserId, e.Status, e.DueDate })
            .HasDatabaseName("IX_Tasks_AssignedToUserId_Status_DueDate");

        builder.HasIndex(e => new { e.DueDate, e.Status })
            .HasDatabaseName("IX_Tasks_DueDate_Status")
            .HasFilter("\"IsDeleted\" = false AND \"Status\" != 'Done'");

        builder.HasIndex(e => new { e.LinkedEntityType, e.LinkedEntityId })
            .HasDatabaseName("IX_Tasks_LinkedEntity");

        // F0004: Composite index for "assignedByMe" view (CreatedByUserId) and "myWork" view overlap
        builder.HasIndex(e => new { e.CreatedByUserId, e.AssignedToUserId })
            .HasDatabaseName("IX_Tasks_CreatedByUserId_AssignedToUserId");
    }
}
