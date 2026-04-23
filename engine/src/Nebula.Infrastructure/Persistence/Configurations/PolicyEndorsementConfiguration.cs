using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class PolicyEndorsementConfiguration : IEntityTypeConfiguration<PolicyEndorsement>
{
    public void Configure(EntityTypeBuilder<PolicyEndorsement> builder)
    {
        builder.ToTable("PolicyEndorsements");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.EndorsementReasonCode).IsRequired().HasMaxLength(80);
        builder.Property(e => e.EndorsementReasonDetail).HasMaxLength(1000);
        builder.Property(e => e.EffectiveDate).IsRequired().HasColumnType("date");
        builder.Property(e => e.PremiumDelta).HasColumnType("decimal(18,2)");
        builder.Property(e => e.PremiumCurrency).IsRequired().HasMaxLength(3).HasDefaultValue("USD");
        builder.Property(e => e.CreatedByUserId).IsRequired();
        builder.Property(e => e.UpdatedByUserId).IsRequired();
        builder.Property(e => e.DeletedByUserId);
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);

        builder.HasOne(e => e.Policy)
            .WithMany(e => e.Endorsements)
            .HasForeignKey(e => e.PolicyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.PolicyVersion)
            .WithMany()
            .HasForeignKey(e => e.PolicyVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasIndex(e => new { e.PolicyId, e.EndorsementNumber })
            .IsUnique()
            .HasDatabaseName("UX_PolicyEndorsements_PolicyId_EndorsementNumber");

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
