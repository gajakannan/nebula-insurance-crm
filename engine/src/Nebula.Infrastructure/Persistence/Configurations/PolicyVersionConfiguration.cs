using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class PolicyVersionConfiguration : IEntityTypeConfiguration<PolicyVersion>
{
    public void Configure(EntityTypeBuilder<PolicyVersion> builder)
    {
        builder.ToTable("PolicyVersions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.VersionReason).IsRequired().HasMaxLength(40);
        builder.Property(e => e.EffectiveDate).IsRequired().HasColumnType("date");
        builder.Property(e => e.ExpirationDate).IsRequired().HasColumnType("date");
        builder.Property(e => e.TotalPremium).HasColumnType("decimal(18,2)");
        builder.Property(e => e.PremiumCurrency).IsRequired().HasMaxLength(3).HasDefaultValue("USD");
        builder.Property(e => e.ProfileSnapshotJson).IsRequired().HasColumnType("jsonb");
        builder.Property(e => e.CoverageSnapshotJson).IsRequired().HasColumnType("jsonb");
        builder.Property(e => e.PremiumSnapshotJson).IsRequired().HasColumnType("jsonb");
        builder.Property(e => e.CreatedByUserId).IsRequired();
        builder.Property(e => e.UpdatedByUserId).IsRequired();
        builder.Property(e => e.DeletedByUserId);
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);

        builder.HasOne(e => e.Policy)
            .WithMany(e => e.Versions)
            .HasForeignKey(e => e.PolicyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasIndex(e => new { e.PolicyId, e.VersionNumber })
            .IsUnique()
            .HasDatabaseName("UX_PolicyVersions_PolicyId_VersionNumber");

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
