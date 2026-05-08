using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class LobProductConfiguration : IEntityTypeConfiguration<LobProduct>
{
    public void Configure(EntityTypeBuilder<LobProduct> builder)
    {
        builder.ToTable("LobProducts");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.ProductKey).IsRequired().HasMaxLength(80);
        builder.Property(e => e.LineOfBusiness).HasMaxLength(50);
        builder.Property(e => e.Name).IsRequired().HasMaxLength(160);
        builder.Property(e => e.Status).IsRequired().HasMaxLength(30).HasDefaultValue("Active");
        builder.Property(e => e.Description).HasMaxLength(1000);
        builder.Property(e => e.CreatedByUserId).IsRequired();
        builder.Property(e => e.UpdatedByUserId).IsRequired();
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);

        builder.Property(e => e.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasIndex(e => e.ProductKey)
            .IsUnique()
            .HasDatabaseName("UX_LobProducts_ProductKey");

        builder.HasIndex(e => new { e.LineOfBusiness, e.Status })
            .HasDatabaseName("IX_LobProducts_LineOfBusiness_Status");

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
