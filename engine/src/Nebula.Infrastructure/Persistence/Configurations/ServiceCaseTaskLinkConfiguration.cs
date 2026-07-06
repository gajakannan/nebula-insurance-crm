using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class ServiceCaseTaskLinkConfiguration : IEntityTypeConfiguration<ServiceCaseTaskLink>
{
    public void Configure(EntityTypeBuilder<ServiceCaseTaskLink> builder)
    {
        builder.ToTable("ServiceCaseTaskLinks");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Relationship).IsRequired().HasMaxLength(40).HasDefaultValue("FollowUp");
        builder.Property(e => e.CreatedByUserId).IsRequired();
        builder.Property(e => e.UpdatedByUserId).IsRequired();
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);
        builder.Property(e => e.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasOne(e => e.ServiceCase)
            .WithMany(e => e.TaskLinks)
            .HasForeignKey(e => e.ServiceCaseId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Task)
            .WithMany()
            .HasForeignKey(e => e.TaskId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.ServiceCaseId, e.TaskId })
            .IsUnique()
            .HasDatabaseName("UX_ServiceCaseTaskLinks_Case_Task");
    }
}
