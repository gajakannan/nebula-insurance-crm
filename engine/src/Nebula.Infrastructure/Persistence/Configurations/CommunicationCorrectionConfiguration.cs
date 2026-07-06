using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class CommunicationCorrectionConfiguration : IEntityTypeConfiguration<CommunicationCorrection>
{
    public void Configure(EntityTypeBuilder<CommunicationCorrection> builder)
    {
        builder.ToTable("CommunicationCorrections");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Action).IsRequired().HasMaxLength(40);
        builder.Property(e => e.Reason).IsRequired().HasMaxLength(500);
        builder.Property(e => e.PreviousSummary).HasMaxLength(500);
        builder.Property(e => e.PreviousBody).HasMaxLength(8000);
        builder.Property(e => e.NewSummary).HasMaxLength(500);
        builder.Property(e => e.NewBody).HasMaxLength(8000);
        builder.Property(e => e.CreatedByUserId).IsRequired();
        builder.Property(e => e.UpdatedByUserId).IsRequired();
        builder.Property(e => e.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasOne(e => e.CommunicationEvent)
            .WithMany(e => e.Corrections)
            .HasForeignKey(e => e.CommunicationEventId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
