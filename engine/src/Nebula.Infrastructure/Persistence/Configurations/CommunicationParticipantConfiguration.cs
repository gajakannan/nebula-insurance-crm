using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class CommunicationParticipantConfiguration : IEntityTypeConfiguration<CommunicationParticipant>
{
    public void Configure(EntityTypeBuilder<CommunicationParticipant> builder)
    {
        builder.ToTable("CommunicationParticipants");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.DisplayName).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Email).HasMaxLength(320);
        builder.Property(e => e.ParticipantType).IsRequired().HasMaxLength(50);
        builder.Property(e => e.Role).HasMaxLength(100);
        builder.Property(e => e.LinkedEntityType).HasMaxLength(50);
        builder.Property(e => e.CreatedByUserId).IsRequired();
        builder.Property(e => e.UpdatedByUserId).IsRequired();
        builder.Property(e => e.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasOne(e => e.CommunicationEvent)
            .WithMany(e => e.Participants)
            .HasForeignKey(e => e.CommunicationEventId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
