using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class CommunicationFollowUpTaskLinkConfiguration : IEntityTypeConfiguration<CommunicationFollowUpTaskLink>
{
    public void Configure(EntityTypeBuilder<CommunicationFollowUpTaskLink> builder)
    {
        builder.ToTable("CommunicationFollowUpTaskLinks");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.CreatedByUserId).IsRequired();
        builder.Property(e => e.UpdatedByUserId).IsRequired();
        builder.Property(e => e.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasOne(e => e.CommunicationEvent)
            .WithMany(e => e.FollowUpTaskLinks)
            .HasForeignKey(e => e.CommunicationEventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.CommunicationEventId, e.TaskId })
            .IsUnique()
            .HasDatabaseName("UX_CommunicationFollowUpTaskLinks_Event_Task");
    }
}
