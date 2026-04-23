using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class CarrierRefConfiguration : IEntityTypeConfiguration<CarrierRef>
{
    public void Configure(EntityTypeBuilder<CarrierRef> builder)
    {
        builder.ToTable("CarrierRefs");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name).IsRequired().HasMaxLength(160);
        builder.Property(e => e.NaicCode).HasMaxLength(20);
        builder.Property(e => e.IsActive).HasDefaultValue(true);
        builder.Property(e => e.CreatedByUserId).IsRequired();
        builder.Property(e => e.UpdatedByUserId).IsRequired();
        builder.Property(e => e.DeletedByUserId);
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);

        builder.Property(e => e.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasIndex(e => e.Name).IsUnique().HasDatabaseName("UX_CarrierRefs_Name");

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
