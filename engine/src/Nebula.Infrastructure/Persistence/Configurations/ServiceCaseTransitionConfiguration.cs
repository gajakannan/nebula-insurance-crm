using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class ServiceCaseTransitionConfiguration : IEntityTypeConfiguration<ServiceCaseTransition>
{
    public void Configure(EntityTypeBuilder<ServiceCaseTransition> builder)
    {
        builder.ToTable("ServiceCaseTransitions");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.FromStatus).HasMaxLength(40);
        builder.Property(e => e.ToStatus).IsRequired().HasMaxLength(40);
        builder.Property(e => e.ReasonCode).HasMaxLength(80);
        builder.Property(e => e.Note).HasMaxLength(1000);
        builder.Property(e => e.CreatedByUserId).IsRequired();
        builder.Property(e => e.UpdatedByUserId).IsRequired();
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);
        builder.Property(e => e.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasOne(e => e.ServiceCase)
            .WithMany(e => e.Transitions)
            .HasForeignKey(e => e.ServiceCaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.ServiceCaseId, e.OccurredAt })
            .HasDatabaseName("IX_ServiceCaseTransitions_Case_OccurredAt");
    }
}
