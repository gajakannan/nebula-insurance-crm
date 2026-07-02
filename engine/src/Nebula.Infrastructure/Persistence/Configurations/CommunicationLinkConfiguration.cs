using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class CommunicationLinkConfiguration : IEntityTypeConfiguration<CommunicationLink>
{
    public void Configure(EntityTypeBuilder<CommunicationLink> builder)
    {
        builder.ToTable("CommunicationLinks");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.EntityType).IsRequired().HasMaxLength(50);
        builder.Property(e => e.CreatedByUserId).IsRequired();
        builder.Property(e => e.UpdatedByUserId).IsRequired();
        builder.Property(e => e.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasOne(e => e.CommunicationEvent)
            .WithMany(e => e.Links)
            .HasForeignKey(e => e.CommunicationEventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.CommunicationEventId, e.EntityType, e.EntityId })
            .IsUnique()
            .HasDatabaseName("UX_CommunicationLinks_Event_Entity");
        builder.HasIndex(e => new { e.EntityType, e.EntityId, e.IsPrimary })
            .HasDatabaseName("IX_CommunicationLinks_Entity_Primary");
    }
}
