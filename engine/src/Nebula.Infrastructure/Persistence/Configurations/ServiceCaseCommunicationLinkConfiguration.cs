using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class ServiceCaseCommunicationLinkConfiguration : IEntityTypeConfiguration<ServiceCaseCommunicationLink>
{
    public void Configure(EntityTypeBuilder<ServiceCaseCommunicationLink> builder)
    {
        builder.ToTable("ServiceCaseCommunicationLinks");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.LinkType).IsRequired().HasMaxLength(40).HasDefaultValue("Context");
        builder.Property(e => e.CreatedByUserId).IsRequired();
        builder.Property(e => e.UpdatedByUserId).IsRequired();
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);
        builder.Property(e => e.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasOne(e => e.ServiceCase)
            .WithMany(e => e.CommunicationLinks)
            .HasForeignKey(e => e.ServiceCaseId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.CommunicationEvent)
            .WithMany()
            .HasForeignKey(e => e.CommunicationEventId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.ServiceCaseId, e.CommunicationEventId })
            .IsUnique()
            .HasDatabaseName("UX_ServiceCaseCommunicationLinks_Case_Event");
    }
}
