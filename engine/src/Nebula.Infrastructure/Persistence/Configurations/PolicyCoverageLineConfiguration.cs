using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class PolicyCoverageLineConfiguration : IEntityTypeConfiguration<PolicyCoverageLine>
{
    public void Configure(EntityTypeBuilder<PolicyCoverageLine> builder)
    {
        builder.ToTable("PolicyCoverageLines");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.CoverageCode).IsRequired().HasMaxLength(40);
        builder.Property(e => e.CoverageName).HasMaxLength(200);
        builder.Property(e => e.Limit).HasColumnType("decimal(18,2)");
        builder.Property(e => e.Deductible).HasColumnType("decimal(18,2)");
        builder.Property(e => e.Premium).HasColumnType("decimal(18,2)");
        builder.Property(e => e.PremiumCurrency).IsRequired().HasMaxLength(3).HasDefaultValue("USD");
        builder.Property(e => e.ExposureBasis).HasMaxLength(40);
        builder.Property(e => e.ExposureQuantity).HasColumnType("decimal(18,2)");
        builder.Property(e => e.IsCurrent).HasDefaultValue(true);
        builder.Property(e => e.CreatedByUserId).IsRequired();
        builder.Property(e => e.UpdatedByUserId).IsRequired();
        builder.Property(e => e.DeletedByUserId);
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);

        builder.HasOne(e => e.Policy)
            .WithMany(e => e.CoverageLines)
            .HasForeignKey(e => e.PolicyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.PolicyVersion)
            .WithMany(e => e.CoverageLines)
            .HasForeignKey(e => e.PolicyVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasIndex(e => new { e.PolicyId, e.IsCurrent })
            .HasDatabaseName("IX_PolicyCoverageLines_PolicyId_IsCurrent");

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
