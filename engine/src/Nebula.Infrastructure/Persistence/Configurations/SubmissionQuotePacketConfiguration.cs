using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class SubmissionQuotePacketConfiguration : IEntityTypeConfiguration<SubmissionQuotePacket>
{
    public void Configure(EntityTypeBuilder<SubmissionQuotePacket> builder)
    {
        builder.ToTable("SubmissionQuotePackets");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Status).IsRequired().HasMaxLength(30).HasDefaultValue("Draft");
        builder.Property(e => e.LinkedDocumentRefsJson).IsRequired().HasColumnType("jsonb").HasDefaultValueSql("'[]'::jsonb");
        builder.Property(e => e.RecordedPremiumAmount).HasPrecision(18, 2);
        builder.Property(e => e.RecordedLimits).HasMaxLength(500);
        builder.Property(e => e.RecordedDeductibles).HasMaxLength(500);
        builder.Property(e => e.EffectiveDate).HasColumnType("date");
        builder.Property(e => e.CarrierMarket).HasMaxLength(200);
        builder.Property(e => e.ReadinessState).IsRequired().HasMaxLength(40).HasDefaultValue("Draft");
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);

        builder.HasOne(e => e.Submission)
            .WithMany(e => e.QuotePackets)
            .HasForeignKey(e => e.SubmissionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.HasIndex(e => e.SubmissionId)
            .IsUnique()
            .HasDatabaseName("IX_SubmissionQuotePackets_SubmissionId");
    }
}
