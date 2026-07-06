using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class ServiceCaseClaimReferenceConfiguration : IEntityTypeConfiguration<ServiceCaseClaimReference>
{
    public void Configure(EntityTypeBuilder<ServiceCaseClaimReference> builder)
    {
        builder.ToTable("ServiceCaseClaimReferences");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.CarrierClaimNumber).HasMaxLength(100);
        builder.Property(e => e.ClaimantDisplayName).HasMaxLength(200);
        builder.Property(e => e.LossSummary).HasMaxLength(2000);
        builder.Property(e => e.CarrierContactReference).HasMaxLength(500);
        builder.Property(e => e.CreatedByUserId).IsRequired();
        builder.Property(e => e.UpdatedByUserId).IsRequired();
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);
        builder.Property(e => e.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasOne(e => e.ServiceCase)
            .WithOne(e => e.ClaimReference)
            .HasForeignKey<ServiceCaseClaimReference>(e => e.ServiceCaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.ServiceCaseId).IsUnique().HasDatabaseName("UX_ServiceCaseClaimReferences_ServiceCase");
        builder.HasIndex(e => e.CarrierClaimNumber).HasDatabaseName("IX_ServiceCaseClaimReferences_CarrierClaimNumber");
    }
}
